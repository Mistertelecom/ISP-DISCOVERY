using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using SharpPcap;

namespace NetworkDiscovery
{
    public class NetworkService
    {
        private readonly NetworkSniffer _sniffer;
        private readonly List<WebSocket> _connectedClients;
        private readonly object _lock = new object();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isScanning;

        public NetworkService()
        {
            _sniffer = new NetworkSniffer();
            _sniffer.OnDeviceDiscovered += Sniffer_OnDeviceDiscovered;
            _sniffer.OnPacketCaptured += Sniffer_OnPacketCaptured;
            _connectedClients = new List<WebSocket>();
            _cancellationTokenSource = new CancellationTokenSource();
            _isScanning = false;
        }

        public static List<NetworkInterfaceInfo> GetNetworkInterfaces()
        {
            try
            {
                var devices = CaptureDeviceList.Instance;
                return devices.Select(dev => new NetworkInterfaceInfo 
                { 
                    Name = dev.Name,
                    Description = dev.Description 
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting network interfaces: {ex.Message}");
                return new List<NetworkInterfaceInfo>();
            }
        }

        public async Task StartScanningAsync(string interfaceName)
        {
            if (_isScanning)
            {
                await StopScanningAsync();
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                await Task.Run(() => 
                {
                    _sniffer.StartCapture(interfaceName);
                    _isScanning = true;
                }, _cancellationTokenSource.Token);

                await BroadcastToClientsAsync(JsonSerializer.Serialize(new { 
                    type = "status", 
                    data = new { status = "scanning_started" } 
                }));
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Scanning operation was canceled.");
                _isScanning = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting scan: {ex.Message}");
                _isScanning = false;
                throw;
            }
        }

        public async Task StopScanningAsync()
        {
            if (!_isScanning) return;

            try
            {
                _cancellationTokenSource.Cancel();
                _sniffer.StopCapture();
                _isScanning = false;

                await BroadcastToClientsAsync(JsonSerializer.Serialize(new { 
                    type = "status", 
                    data = new { status = "scanning_stopped" } 
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping scan: {ex.Message}");
                throw;
            }
            finally
            {
                _isScanning = false;
            }
        }

        public async Task HandleWebSocketConnection(WebSocket webSocket)
        {
            try
            {
                lock (_lock)
                {
                    _connectedClients.Add(webSocket);
                }

                // Send initial status
                var statusMessage = JsonSerializer.Serialize(new { 
                    type = "status", 
                    data = new { 
                        status = "connected",
                        isScanning = _isScanning
                    } 
                });
                await SendToClientAsync(webSocket, statusMessage);

                var buffer = new byte[1024 * 4];
                while (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        var result = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                string.Empty,
                                CancellationToken.None);
                            break;
                        }
                    }
                    catch (WebSocketException)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    _connectedClients.Remove(webSocket);
                }
            }
        }

        private async Task SendToClientAsync(WebSocket client, string message)
        {
            if (client.State != WebSocketState.Open) return;

            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending to client: {ex.Message}");
            }
        }

        private async Task BroadcastToClientsAsync(string message)
        {
            List<WebSocket> deadClients = new List<WebSocket>();

            lock (_lock)
            {
                foreach (var client in _connectedClients.ToList())
                {
                    try
                    {
                        if (client.State == WebSocketState.Open)
                        {
                            SendToClientAsync(client, message).Wait();
                        }
                        else
                        {
                            deadClients.Add(client);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error broadcasting to client: {ex.Message}");
                        deadClients.Add(client);
                    }
                }

                foreach (var deadClient in deadClients)
                {
                    _connectedClients.Remove(deadClient);
                }
            }
        }

        private async void Sniffer_OnDeviceDiscovered(object sender, Device device)
        {
            try
            {
                var message = JsonSerializer.Serialize(new
                {
                    type = "device",
                    data = device
                });
                await BroadcastToClientsAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in device discovery handler: {ex.Message}");
            }
        }

        private async void Sniffer_OnPacketCaptured(object sender, PacketCaptureEventArgs e)
        {
            try
            {
                var message = JsonSerializer.Serialize(new
                {
                    type = "packet",
                    data = new { sourceIP = e.SourceIP }
                });
                await BroadcastToClientsAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in packet capture handler: {ex.Message}");
            }
        }
    }

    public class NetworkInterfaceInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
