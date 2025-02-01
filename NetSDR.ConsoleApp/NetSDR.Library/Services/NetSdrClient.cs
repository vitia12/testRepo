using NetSDR.Library.Models;
using System.Net.Sockets;
using System.Text;

namespace NetSDR.Library.Services
{
    public class NetSdrClient : INetSdrClient, IDisposable
    {
        private TcpClient _tcpClient = new();
        private NetworkStream? _stream;
        private CancellationTokenSource? _listeningCancellationTokenSource;
        private bool _isConnected;

        // Added fields for UDP functionality
        private UdpClient? _udpClient;
        private Task? _udpListenerTask;

        public void Connect(string host = "127.0.0.1", int tcpPort = 50000, int udpPort = 60000)
        {
            Console.WriteLine("Connecting to receiver...");
            if (_isConnected)
            {
                Console.WriteLine("Already connected.");
                return;
            }
            try
            {
                _tcpClient = new TcpClient(host, tcpPort);
                _stream = _tcpClient.GetStream();
                StartListeningForUnsolicitedMessages();
                StartListeningForUdpPackets(udpPort);

                _isConnected = true;
                Console.WriteLine("Connected!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            Console.WriteLine("Disconnecting from receiver...");
            if (!_isConnected)
            {
                Console.WriteLine("Not connected.");
                return;
            }
            try
            {
                StopListeningForUnsolicitedMessages();
                StopListeningForUdpPackets();

                _stream?.Close();
                _tcpClient.Close();
                _isConnected = false;
                Console.WriteLine("Disconnected!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to disconnect: {ex.Message}");
            }
        }

        public void StartStopReceiver(bool start, StartStopReceiverConfig startStopReceiverConfig)
        {
            if (!_isConnected)
            {
                Console.WriteLine("Not connected.");
                return;
            }
            if (!new byte[] { 0x00, 0x80, 0x01, 0x83, 0x03 }.Contains(startStopReceiverConfig.CaptureMode))
            {
                Console.WriteLine("Invalid capture mode.");
                return;
            }
            if (startStopReceiverConfig is { CaptureMode: 0x01, FifoSamples: 0 })
            {
                Console.WriteLine("FIFO mode requires a non-zero number of samples.");
                return;
            }
            var dataType = (byte)(startStopReceiverConfig.IsComplex ? 0x80 : 0x00);
            var runStopControl = start ? (byte)0x02 : (byte)0x01;
            var nFifoSamples = startStopReceiverConfig.FifoSamples ?? 0;
            var commandBytes = new List<byte>
            {
                0x08, 0x00, // Length (8 bytes total)
                0x18, 0x00, // Control Item Code (0x0018 for receiver state control)
                dataType,   // Data Type (0x00 = real A/D samples, 0x80 = complex I/Q baseband data)
                runStopControl, // Run/Stop Control (0x01 = Stop, 0x02 = Start)
                startStopReceiverConfig.CaptureMode,
                (byte)(nFifoSamples & 0xFF) // FIFO Samples (only used in FIFO mode)
            };
            SendCommand(commandBytes.ToArray());
        }

        public void SetFrequency(long frequencyHz, FrequencyConfig frequencyConfig)
        {
            if (!_isConnected)
            {
                Console.WriteLine("Not connected.");
                return;
            }
            if (frequencyHz < 0)
            {
                Console.WriteLine("Frequency must be a non-negative value.");
                return;
            }
            if (!new byte[] { 0x00, 0x02, 0xFF }.Contains(frequencyConfig.ChannelId))
            {
                Console.WriteLine("Invalid channel ID. Must be 0x00 (Channel 1), 0x02 (Channel 2), or 0xFF (All channels).");
                return;
            }
            var frequencyBytes = BitConverter.GetBytes(frequencyHz);
            var commandBytes = new List<byte>
            {
                0x0A, 0x00, // Length (10 bytes total)
                0x20, 0x00, // Control Item Code (0x0020 for frequency control)
                frequencyConfig.ChannelId   // Channel ID
            };
            commandBytes.AddRange(frequencyBytes.Take(5));
            SendCommand(commandBytes.ToArray());
        }

        private void SendCommand(byte[] command)
        {
            try
            {
                Console.WriteLine($"Sending command: {BitConverter.ToString(command).Replace("-", "")}");
                _stream!.Write(command, 0, command.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending command: {ex.Message}");
            }
            var response = ReadResponse();
            if (response.StartsWith("NAK"))
            {
                Console.WriteLine($"Command is not supported: {response}");
            }
            else if (response.StartsWith("ACK"))
            {
                Console.WriteLine($"Command acknowledged: {response}");
            }
            else
            {
                Console.WriteLine($"Received response: {response}");
            }
        }

        private string ReadResponse()
        {
            try
            {
                var buffer = new byte[1024];
                using var memoryStream = new MemoryStream();
                if (_stream is not null)
                {
                    var bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    memoryStream.Write(buffer, 0, bytesRead);
                    var receivedBytes = memoryStream.ToArray();
                    var header = receivedBytes[0].ToString("X2") + receivedBytes[1].ToString("X2");
                    switch (header)
                    {
                        case "0200": return "NAK";
                        case "6003": return "ACK";
                        default: return Encoding.UTF8.GetString(receivedBytes).Trim();
                    }
                }
                return "No response";
            }
            catch (Exception ex)
            {
                return $"Error reading response: {ex.Message}";
            }
        }

        private void StartListeningForUnsolicitedMessages()
        {
            _listeningCancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ListenForUnsolicitedMessages(_listeningCancellationTokenSource.Token));
        }

        private void StopListeningForUnsolicitedMessages()
        {
            _listeningCancellationTokenSource?.Cancel();
            _listeningCancellationTokenSource = null;
        }

        private async Task ListenForUnsolicitedMessages(CancellationToken cancellationToken)
        {
            try
            {
                while (_isConnected && !cancellationToken.IsCancellationRequested)
                {
                    if (_stream is null || !_stream.DataAvailable)
                    {
                        await Task.Delay(100, cancellationToken); // Avoid busy-waiting
                        continue;
                    }
                    var buffer = new byte[1024];
                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead > 0)
                    {
                        var receivedBytes = buffer[..bytesRead];
                        ProcessUnsolicitedMessage(receivedBytes);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Stopped listening for unsolicited messages.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while listening for unsolicited messages: {ex.Message}");
            }
        }

        private void ProcessUnsolicitedMessage(byte[] message) => Console.WriteLine($"Received unsolicited message: {BitConverter.ToString(message).Replace("-", "")}");

        private void StartListeningForUdpPackets(int udpPort, string? outputFilePath = null)
        {
            if (_udpClient != null)
            {
                Console.WriteLine("UDP listener is already running.");
                return;
            }

            _udpClient = new UdpClient(udpPort);
            _udpListenerTask = Task.Run(async () =>
            {
                await using var fileStream = new FileStream(outputFilePath ?? $"{Directory.GetCurrentDirectory()}/TestMessages.txt", FileMode.Create, FileAccess.Write);
                try
                {
                    while (_isConnected && _udpClient != null)
                    {
                        var result = await _udpClient.ReceiveAsync();
                        ProcessUdpPacket(result.Buffer, fileStream);
                    }
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("UDP listener stopped.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while listening for UDP packets: {ex.Message}");
                }
            });
        }

        private void StopListeningForUdpPackets()
        {
            _udpClient?.Close();
            _udpClient = null;
            _udpListenerTask?.Wait(); // Ensure the task completes
            _udpListenerTask = null;
        }

        private void ProcessUdpPacket(byte[] packet, FileStream fileStream)
        {
            if (packet.Length < 4) // Minimum length for header
            {
                Console.WriteLine("Invalid UDP packet received.");
                return;
            }

            // Extract I/Q data (skip the first 4 bytes - header)
            var iqData = packet[4..];

            fileStream.Write(iqData, 0, iqData.Length);
            Console.WriteLine(string.Join(" ", packet));
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}