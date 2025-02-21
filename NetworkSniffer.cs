using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using PacketDotNet;
using SharpPcap;
using System.Security.Principal;
using NLog;
using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;

namespace NetworkDiscovery
{
    public class Device
    {
        public string Brand { get; set; }
        public string IPAddress { get; set; }
        public string MacAddress { get; set; }
        public string Name { get; set; }
        public string DiscoveryMethod { get; set; }
        public string Model { get; set; }
        public DateTime LastSeen { get; set; }
        public int SignalStrength { get; set; }
    }

    public class PacketCaptureEventArgs : EventArgs
    {
        public string SourceIP { get; set; }
        public string MacAddress { get; set; }
        public int SignalStrength { get; set; }
    }

    public class VendorData
    {
        [JsonPropertyName("vendors")]
        public Dictionary<string, VendorInfo> Vendors { get; set; }
    }

    public class VendorInfo
    {
        [JsonPropertyName("prefixes")]
        public Dictionary<string, string> Prefixes { get; set; }
    }

    public class NetworkSniffer : IDisposable
    {
        private ICaptureDevice _device;
        private readonly Dictionary<string, Device> _discoveredDevices;
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _isRunning;
        private readonly object _lock = new object();
        private Task _processingTask;
        private bool _disposed;
        private Dictionary<string, (string Brand, string Type)> _vendorPrefixes;

        public event EventHandler<Device> OnDeviceDiscovered;
        public event EventHandler<PacketCaptureEventArgs> OnPacketCaptured;

        public NetworkSniffer()
        {
            _discoveredDevices = new Dictionary<string, Device>();
            _isRunning = false;
            _disposed = false;
            _vendorPrefixes = new Dictionary<string, (string Brand, string Type)>();
            LoadVendorPrefixes();
        }

        private void LoadVendorPrefixes()
        {
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vendor-macs.json");
                if (!File.Exists(jsonPath))
                {
                    jsonPath = "vendor-macs.json"; // Try current directory if not found in base directory
                }

                if (!File.Exists(jsonPath))
                {
                    throw new FileNotFoundException("vendor-macs.json not found");
                }

                string jsonContent = File.ReadAllText(jsonPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var vendorData = JsonSerializer.Deserialize<VendorData>(jsonContent, options);
                
                if (vendorData?.Vendors == null)
                {
                    throw new JsonException("Invalid JSON format: 'vendors' object not found");
                }

                _vendorPrefixes = new Dictionary<string, (string Brand, string Type)>();

                foreach (var vendor in vendorData.Vendors)
                {
                    if (vendor.Value?.Prefixes != null)
                    {
                        foreach (var prefix in vendor.Value.Prefixes)
                        {
                            _vendorPrefixes[prefix.Key] = (vendor.Key, prefix.Value);
                        }
                    }
                }

                Console.WriteLine($"Successfully loaded {_vendorPrefixes.Count} MAC address prefixes from vendor-macs.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading vendor MAC prefixes: {ex.Message}");
                if (ex is JsonException jsonEx)
                {
                    Console.WriteLine($"JSON parsing error: {jsonEx.Message}");
                }
                _vendorPrefixes = new Dictionary<string, (string Brand, string Type)>();
            }
        }

        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public void StartCapture(string deviceName)
        {
            ThrowIfDisposed();

            if (!IsAdministrator())
            {
                throw new UnauthorizedAccessException("Administrator privileges are required to capture network traffic.");
            }

            lock (_lock)
            {
                if (_isRunning)
                {
                    StopCapture();
                }

                try
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    var devices = CaptureDeviceList.Instance;

                    if (devices == null || devices.Count == 0)
                    {
                        throw new Exception("No network interfaces found. Make sure WinPcap/Npcap is installed.");
                    }

                    _device = devices.FirstOrDefault(d => d.Name == deviceName);

                    if (_device == null)
                    {
                        throw new Exception($"Network interface '{deviceName}' not found.");
                    }

                    try
                    {
                        _device.Open(DeviceModes.Promiscuous, 1000);
                        // Broader packet filter to capture more traffic
                        _device.Filter = "ether proto 0x0800 or arp or udp port 5246 or udp port 5247 or port 80 or port 443 or icmp";
                        _device.OnPacketArrival += Device_OnPacketArrival;
                        _device.StartCapture();
                        _isRunning = true;

                        Console.WriteLine($"Started capturing on interface: {deviceName}");
                        Console.WriteLine("Packet filter: " + _device.Filter);

                        _processingTask = Task.Run(ProcessPacketsAsync, _cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error opening network interface: {ex.Message}. Make sure you have administrator privileges and WinPcap/Npcap is installed.");
                    }
                }
                catch (Exception ex)
                {
                    CleanupCapture();
                    throw new Exception($"Error starting capture: {ex.Message}");
                }
            }
        }

        public void StopCapture()
        {
            ThrowIfDisposed();

            lock (_lock)
            {
                if (!_isRunning) return;

                try
                {
                    _isRunning = false;
                    _cancellationTokenSource?.Cancel();

                    if (_device != null)
                    {
                        try
                        {
                            _device.StopCapture();
                            _device.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error stopping capture: {ex.Message}");
                        }
                        finally
                        {
                            _device = null;
                        }
                    }

                    _discoveredDevices.Clear();
                    
                    try
                    {
                        _processingTask?.Wait(TimeSpan.FromSeconds(5));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error waiting for processing task: {ex.Message}");
                    }
                    finally
                    {
                        _processingTask = null;
                    }
                }
                finally
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }

        private void CleanupCapture()
        {
            try
            {
                StopCapture();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        private async Task ProcessPacketsAsync()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    CleanupOldDevices();
                    await Task.Delay(30000, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in packet processing: {ex.Message}");
            }
        }

        private void CleanupOldDevices()
        {
            var now = DateTime.UtcNow;
            var oldDevices = _discoveredDevices.Values
                .Where(d => (now - d.LastSeen).TotalMinutes > 5)
                .ToList();

            foreach (var device in oldDevices)
            {
                _discoveredDevices.Remove(device.MacAddress);
            }
        }

        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            if (!_isRunning) return;

            try
            {
                var rawPacket = e.GetPacket();
                if (rawPacket?.Data == null) return;

                var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                var ethernetPacket = packet?.Extract<EthernetPacket>();
                
                if (ethernetPacket == null) return;

                var sourceMac = ethernetPacket.SourceHardwareAddress.ToString().Replace(":", "").ToUpper();
                Console.WriteLine($"Packet received from MAC: {sourceMac}"); // Debug logging

                var deviceInfo = GetVendorFromMac(sourceMac);
                var ipPacket = packet.Extract<IPPacket>();
                var sourceIP = ipPacket?.SourceAddress.ToString() ?? "Unknown";

                // Log all packets for debugging
                Console.WriteLine($"Packet: MAC={sourceMac}, IP={sourceIP}, IsVendorMatch={deviceInfo.HasValue}");

                if (!deviceInfo.HasValue)
                {
                    // Log unknown devices for debugging
                    Console.WriteLine($"Unknown vendor MAC prefix: {sourceMac.Substring(0, 6)}");
                    return;
                }

                var signalStrength = CalculateSignalStrength(rawPacket);

                lock (_lock)
                {
                    if (!_discoveredDevices.TryGetValue(sourceMac, out var device))
                    {
                        device = new Device
                        {
                            MacAddress = sourceMac,
                            Brand = deviceInfo.Value.Brand,
                            Model = deviceInfo.Value.Type,
                            DiscoveryMethod = "Packet Capture",
                            LastSeen = DateTime.UtcNow
                        };
                        _discoveredDevices.Add(sourceMac, device);
                        Console.WriteLine($"New device discovered: {device.Brand} {device.Model} ({sourceMac})");
                        OnDeviceDiscovered?.Invoke(this, device);
                    }

                    device.LastSeen = DateTime.UtcNow;
                    device.SignalStrength = signalStrength;
                    if (ipPacket != null)
                    {
                        device.IPAddress = sourceIP;
                        if (string.IsNullOrEmpty(device.Name))
                        {
                            device.Name = GetHostname(sourceIP);
                        }
                    }

                    OnPacketCaptured?.Invoke(this, new PacketCaptureEventArgs 
                    { 
                        SourceIP = sourceIP,
                        MacAddress = sourceMac,
                        SignalStrength = signalStrength
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing packet: {ex.Message}");
            }
        }

        private int CalculateSignalStrength(RawCapture rawPacket)
        {
            try
            {
                if (rawPacket.LinkLayerType == LinkLayers.Ieee80211)
                {
                    return BitConverter.ToInt32(rawPacket.Data, 22);
                }
            }
            catch
            {
                // Ignore errors in signal strength calculation
            }
            return 0;
        }

        private (string Brand, string Type)? GetVendorFromMac(string macAddress)
        {
            if (string.IsNullOrEmpty(macAddress) || macAddress.Length < 6) return null;

            var prefix = macAddress.Substring(0, 6);
            if (_vendorPrefixes.TryGetValue(prefix, out var vendorInfo))
            {
                // Debug logging for MAC prefix matching
                Console.WriteLine($"MAC prefix {prefix} matched to {vendorInfo.Brand} {vendorInfo.Type}");
                return vendorInfo;
            }

            return null;
        }

        private string GetHostname(string ip)
        {
            if (string.IsNullOrEmpty(ip) || ip == "Unknown") return "Unknown";

            try
            {
                var hostEntry = Dns.GetHostEntry(ip);
                return hostEntry.HostName;
            }
            catch
            {
                return "Unknown";
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NetworkSniffer));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                StopCapture();
                _cancellationTokenSource?.Dispose();
            }

            _disposed = true;
        }

        ~NetworkSniffer()
        {
            Dispose(false);
        }
    }
}
