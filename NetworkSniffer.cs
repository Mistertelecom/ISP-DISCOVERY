using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PacketDotNet;
using SharpPcap;

namespace NetworkDiscovery
{
    public class PacketCaptureEventArgs : EventArgs
    {
        public string SourceIP { get; set; }
        public string DestinationIP { get; set; }
        public string Protocol { get; set; }
        public int Length { get; set; }
    }

    public class Device
    {
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string Brand { get; set; }
        public string MacAddress { get; set; }
        public DateTime DiscoveryTime { get; set; }
        public string DiscoveryMethod { get; set; }
        public string DeviceType { get; set; }
        public string Version { get; set; }
        public string Model { get; set; }
    }

    public class NetworkSniffer
    {
        private ICaptureDevice captureDevice;
        private ConcurrentDictionary<string, Device> discoveredDevices;
        private ConcurrentDictionary<string, byte> processedAddresses;
        private BlockingCollection<RawCapture> packetQueue;
        private CancellationTokenSource cancellationTokenSource;
        private Task processingTask;
        private Task discoveryTask;
        private const int QUEUE_CAPACITY = 10000;
        private const int BATCH_SIZE = 200;
        private const int PROCESSING_THREADS = 8;

        public event EventHandler<Device> OnDeviceDiscovered;
        public event EventHandler<PacketCaptureEventArgs> OnPacketCaptured;

        private const string CAPTURE_FILTER = "ip or arp";

        private static readonly Dictionary<string, HashSet<string>> ManufacturerOUIs = new Dictionary<string, HashSet<string>>
        {
            ["Ubiquiti"] = new HashSet<string> { 
                "00156D", "002722", "0418D6", "18E829", "245A4C", "24A43C", "28704E", 
                "44D9E7", "687251", "7483C2", "788A20", "784558", "802AA8", "942A6F", 
                "AC8BA9", "B4FBE4", "D021F9", "DC9FDB", "E063DA", "F09FC2", "F492BF"
            },
            ["Mikrotik"] = new HashSet<string> { 
                "000C42", "085531", "18FD74", "2CC81B", "488F5A", "48A98A", "4C5E0C", 
                "64D154", "6C3B6B", "744D28", "789A18", "B869F4", "C4AD34", "CC2DE0", 
                "D401C3", "D4CA6D", "DC2C6E", "E48D8C"
            },
            ["Mimosa"] = new HashSet<string> { 
                "20B5C6", "F898B9", "586D8F", "7483C2", "DCFE07", "B0B1CD", "A0F3C1"
            }
        };

        public NetworkSniffer()
        {
            discoveredDevices = new ConcurrentDictionary<string, Device>();
            processedAddresses = new ConcurrentDictionary<string, byte>();
            packetQueue = new BlockingCollection<RawCapture>(QUEUE_CAPACITY);
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void StartCapture(string deviceName)
        {
            try
            {
                var devices = CaptureDeviceList.Instance;
                captureDevice = devices.FirstOrDefault(d => d.Name == deviceName);
                if (captureDevice == null)
                    throw new Exception($"Network interface '{deviceName}' not found.");

                discoveredDevices.Clear();
                processedAddresses.Clear();
                packetQueue = new BlockingCollection<RawCapture>(QUEUE_CAPACITY);

                processingTask = Task.WhenAll(Enumerable.Range(0, PROCESSING_THREADS)
                    .Select(_ => Task.Run(() => ProcessPacketsAsync(cancellationTokenSource.Token))));
                discoveryTask = Task.Run(() => SendDiscoveryPacketsAsync(cancellationTokenSource.Token));

                captureDevice.OnPacketArrival += PacketArrival;
                captureDevice.Open(DeviceModes.Promiscuous);
                captureDevice.Filter = CAPTURE_FILTER;
                captureDevice.StartCapture();
            }
            catch (Exception)
            {
                StopCapture();
                throw;
            }
        }

        private async Task SendDiscoveryPacketsAsync(CancellationToken token)
        {
            try
            {
                using (var udpClient = new UdpClient())
                {
                    udpClient.EnableBroadcast = true;
                    byte[] mndpPacket = new byte[] {
                        0x00, 0x00, 0x00, 0x00,
                        0x00, 0x01, 0x00, 0x04,
                        0x00, 0x00, 0x00, 0x00
                    };

                    while (!token.IsCancellationRequested)
                    {
                        await udpClient.SendAsync(mndpPacket, mndpPacket.Length, "255.255.255.255", 5678);
                        await Task.Delay(500, token);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in discovery sender: {ex.Message}");
            }
        }

        private void PacketArrival(object sender, PacketCapture e)
        {
            if (!packetQueue.IsAddingCompleted)
                packetQueue.TryAdd(e.GetPacket());
        }

        private async Task ProcessPacketsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var packets = new List<RawCapture>();
                    for (int i = 0; i < BATCH_SIZE && !cancellationToken.IsCancellationRequested; i++)
                    {
                        if (packetQueue.TryTake(out RawCapture packet, 50))
                            packets.Add(packet);
                        else break;
                    }

                    if (packets.Any())
                        await Task.WhenAll(packets.Select(packet => Task.Run(() => ProcessPacket(packet))));
                    else
                        await Task.Delay(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing packets: {ex.Message}");
            }
        }

        private void ProcessPacket(RawCapture rawPacket)
        {
            try
            {
                var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                var ethernetPacket = packet.Extract<EthernetPacket>();
                if (ethernetPacket == null) return;

                string sourceMac = ethernetPacket.SourceHardwareAddress.ToString()
                    .Replace(":", "").Replace("-", "").ToUpper();

                var ipPacket = packet.Extract<IPPacket>();
                if (ipPacket != null)
                {
                    string sourceIP = ipPacket.SourceAddress.ToString();

                    OnPacketCaptured?.Invoke(this, new PacketCaptureEventArgs
                    {
                        SourceIP = sourceIP,
                        DestinationIP = ipPacket.DestinationAddress.ToString(),
                        Protocol = ipPacket.Protocol.ToString(),
                        Length = rawPacket.Data.Length
                    });

                    if (processedAddresses.TryAdd(sourceMac, 1))
                    {
                        string brand = DetermineManufacturer(sourceMac);
                        if (brand != null)
                        {
                            var device = new Device
                            {
                                IPAddress = sourceIP,
                                MacAddress = FormatMacAddress(sourceMac),
                                Brand = brand,
                                Name = $"{brand} Device",
                                DiscoveryTime = DateTime.Now,
                                DiscoveryMethod = "OUI",
                                Model = DetermineModel(sourceMac, brand)
                            };

                            if (discoveredDevices.TryAdd(sourceMac, device))
                                OnDeviceDiscovered?.Invoke(this, device);
                        }
                    }
                }

                if (ethernetPacket.Type == (EthernetType)0x2000)
                {
                    ProcessCDPPacket(ethernetPacket, rawPacket.Data);
                }
                else
                {
                    var udpPacket = packet.Extract<UdpPacket>();
                    if (udpPacket != null && (udpPacket.DestinationPort == 5678 || udpPacket.SourcePort == 5678))
                        ProcessMNDPPacket(ethernetPacket, udpPacket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing packet: {ex.Message}");
            }
        }

        private string DetermineModel(string macAddress, string brand)
        {
            string oui = macAddress.Substring(0, 6);
            switch (brand)
            {
                case "Mimosa":
                    if (oui == "20B5C6") return "C5x/B5x Series";
                    else if (oui == "F898B9") return "A5c/A5x Series";
                    else return "Mimosa Device";
                case "Ubiquiti":
                    return "Unifi Series";
                case "Mikrotik":
                    return "RouterBoard Series";
                default:
                    return "Unknown";
            }
        }

        private void ProcessMNDPPacket(EthernetPacket ethernetPacket, UdpPacket udpPacket)
        {
            try
            {
                string sourceMac = ethernetPacket.SourceHardwareAddress.ToString()
                    .Replace(":", "").Replace("-", "").ToUpper();

                var payload = udpPacket.PayloadData;
                if (payload.Length < 4) return;

                var device = new Device
                {
                    MacAddress = FormatMacAddress(sourceMac),
                    Brand = "Mikrotik",
                    DiscoveryMethod = "MNDP",
                    DiscoveryTime = DateTime.Now,
                    Model = "RouterBoard Series"
                };

                int offset = 0;
                while (offset + 4 <= payload.Length)
                {
                    ushort type = BitConverter.ToUInt16(payload, offset);
                    ushort length = BitConverter.ToUInt16(payload, offset + 2);

                    if (offset + 4 + length > payload.Length) break;

                    string value = System.Text.Encoding.UTF8.GetString(payload, offset + 4, length).Trim('\0');
                    switch (type)
                    {
                        case 1: device.Name = value; break;
                        case 5: device.Version = value; break;
                        case 7: device.DeviceType = value; break;
                        case 8: device.IPAddress = value; break;
                    }
                    offset += 4 + length;
                }

                if (discoveredDevices.TryAdd(sourceMac, device))
                    OnDeviceDiscovered?.Invoke(this, device);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing MNDP packet: {ex.Message}");
            }
        }

        private void ProcessCDPPacket(EthernetPacket ethernetPacket, byte[] data)
        {
            try
            {
                string sourceMac = ethernetPacket.SourceHardwareAddress.ToString()
                    .Replace(":", "").Replace("-", "").ToUpper();

                var device = new Device
                {
                    MacAddress = FormatMacAddress(sourceMac),
                    DiscoveryMethod = "CDP",
                    DiscoveryTime = DateTime.Now,
                    Model = "Cisco Device"
                };

                int offset = 22;
                while (offset + 4 <= data.Length)
                {
                    int type = (data[offset] << 8) | data[offset + 1];
                    int length = (data[offset + 2] << 8) | data[offset + 3];

                    if (length < 4 || offset + length > data.Length) break;

                    switch (type)
                    {
                        case 1:
                            device.Name = System.Text.Encoding.ASCII.GetString(data, offset + 4, length - 4).Trim('\0');
                            break;
                        case 5:
                            device.DeviceType = System.Text.Encoding.ASCII.GetString(data, offset + 4, length - 4).Trim('\0');
                            break;
                        case 6:
                            device.Version = System.Text.Encoding.ASCII.GetString(data, offset + 4, length - 4).Trim('\0');
                            break;
                    }
                    offset += length;
                }

                if (device.Name?.Contains("UBNT", StringComparison.OrdinalIgnoreCase) == true)
                    device.Brand = "Ubiquiti";
                else if (device.Name?.Contains("Cisco", StringComparison.OrdinalIgnoreCase) == true)
                    device.Brand = "Cisco";
                else
                    device.Brand = "Unknown";

                if (discoveredDevices.TryAdd(sourceMac, device))
                    OnDeviceDiscovered?.Invoke(this, device);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing CDP packet: {ex.Message}");
            }
        }

        private string DetermineManufacturer(string macAddress)
        {
            string oui = macAddress.Substring(0, 6);
            return ManufacturerOUIs.FirstOrDefault(m => m.Value.Contains(oui)).Key;
        }

        private string FormatMacAddress(string mac)
        {
            return string.Join(":", Enumerable.Range(0, 6)
                .Select(i => mac.Substring(i * 2, 2)));
        }

        public void StopCapture()
        {
            try
            {
                cancellationTokenSource?.Cancel();
                packetQueue.CompleteAdding();
                if (captureDevice != null)
                {
                    captureDevice.StopCapture();
                    captureDevice.Close();
                }
                Task.WaitAll(new[] { processingTask, discoveryTask }, 1000);
            }
            catch (Exception)
            {
                // Ignorar erros durante finalização
            }
        }
    }
}