using NetSDR.Library.Consts;
using NetSDR.Library.Models;
using System.Text;

namespace NetSDR.Library.Services
{
    public class NetSdrServer(ITcpClientWrapper tcpClient, IUdpClientWrapper udpClient) : INetSdrClient, IDisposable
    {
        private static readonly byte[] CaptureModeBytes = [0x00, 0x80, 0x01, 0x83, 0x03];
        private static readonly byte[] AvailableChannelsBytes = [0x00, 0x02, 0xFF];

        private Stream _tcpStream = null!;
        private CancellationTokenSource _listenersCancellationTokenSource = new();

        private bool _isConnected;
        private bool _isReceiverTurnedOn;

        public void Connect(string host, int tcpPort, int udpPort)
        {
            Console.WriteLine("Connecting to receiver...");
            if (_isConnected)
            {
                Console.WriteLine("Already connected.");
                return;
            }
            try
            {
                _listenersCancellationTokenSource = new();

                tcpClient.Connect(host, tcpPort);
                udpClient.Connect(host, udpPort);

                _tcpStream = tcpClient.GetStream();
                StartListeningForUnsolicitedMessages(_listenersCancellationTokenSource.Token);
                StartListeningForUdpPackets($"{Directory.GetCurrentDirectory()}/TestMessages.txt", _listenersCancellationTokenSource.Token);

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
                StopListeners();

                tcpClient.Disconnect();
                udpClient.Disconnect();

                _tcpStream.Close();

                _isReceiverTurnedOn = false;
                _isConnected = false;
                Console.WriteLine("Disconnected!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to disconnect: {ex.Message}");
            }
        }

        public void ToggleReceiverState(bool start, ReceiverConfig receiverConfig)
        {
            if (!_isConnected)
            {
                Console.WriteLine("Not connected.");
                return;
            }
            if (!CaptureModeBytes.Contains(receiverConfig.CaptureMode))
            {
                Console.WriteLine("Invalid capture mode.");
                return;
            }
            if (receiverConfig is { CaptureMode: 0x01, FifoSamples: 0 })
            {
                Console.WriteLine("FIFO mode requires a non-zero number of samples.");
                return;
            }

            var dataType = receiverConfig.IsComplex ? CommandConstants.ComplexIqData : CommandConstants.RealAdSamples;
            byte runStopControlByte;
            if (start)
            {
                runStopControlByte = CommandConstants.StartReceiver;
                _isReceiverTurnedOn = true;
            }
            else
            {
                runStopControlByte = CommandConstants.StopReceiver;
                _isReceiverTurnedOn = false;
            }

            var nFifoSamples = receiverConfig.FifoSamples ?? 0;
            byte[] commandBytes = [
            
                0x08, 0x00, // Length (8 bytes total)
                0x18, 0x00, // Control Item Code (0x0018 for receiver state control)
                dataType,   // Data Type (0x00 = real A/D samples, 0x80 = complex I/Q baseband data)
                runStopControlByte, // Run/Stop Control (0x01 = Stop, 0x02 = Start)
                receiverConfig.CaptureMode,
                (byte)(nFifoSamples & 0xFF) // FIFO Samples (only used in FIFO mode)
            ];
            SendCommand(commandBytes);
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
            if (!AvailableChannelsBytes.Contains(frequencyConfig.ChannelId))
            {
                Console.WriteLine("Invalid channel ID. Must be 0x00 (Channel 1), 0x02 (Channel 2), or 0xFF (All channels).");
                return;
            }
            var frequencyBytes = BitConverter.GetBytes(frequencyHz);
            byte[] commandBytes = [
                0x0A, 0x00, // Length (10 bytes total)
                0x20, 0x00, // Control Item Code (0x0020 for frequency control)
                frequencyConfig.ChannelId,
                ..frequencyBytes[..5]
            ];
            SendCommand(commandBytes);
        }

        private void SendCommand(byte[] command)
        {
            try
            {
                Console.WriteLine($"Sending command: {BitConverter.ToString(command).Replace("-", "")}");
                _tcpStream.Write(command, 0, command.Length);
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
                var buffer = new byte[CommandConstants.ResponseBufferSize];
                var bytesRead = _tcpStream.Read(buffer, 0, buffer.Length);
                if (bytesRead <= 2)
                {
                    return "Invalid response";
                }

                var header = $"{buffer[1]:X2}{buffer[0]:X2}";
                return header switch
                {
                    CommandConstants.NackHeader => "NAK",
                    CommandConstants.AckHeader => "ACK",
                    _ => Encoding.UTF8.GetString(buffer).Trim()
                };
            }
            catch (Exception ex)
            {
                return $"Error reading response: {ex.Message}";
            }
        }

        private void StartListeningForUnsolicitedMessages(CancellationToken cancellationToken) => Task.Run(() => ListenForUnsolicitedMessages(cancellationToken), cancellationToken);

        private void StopListeners() => _listenersCancellationTokenSource.Cancel();

        private async Task ListenForUnsolicitedMessages(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    if (_tcpStream is null || !_tcpStream.CanRead)
                    {
                        continue;
                    }
                    // Here will be logic to handle unsolicited items
                    // TODO Ignore tcp packets that were sent by us
                    var buffer = new byte[CommandConstants.ResponseBufferSize];
                    var bytesRead = await _tcpStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
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

        private void ProcessUnsolicitedMessage(byte[] message) => Console.WriteLine($"Received unsolicited message: {BitConverter.ToString(message).Replace("-", " ")}");

        private void StartListeningForUdpPackets(string outputFilePath, CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                await using var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: CommandConstants.FileBufferSize, useAsync: true);
                await using var writer = new StreamWriter(fileStream, Encoding.UTF8, leaveOpen: true);

                try
                {
                    while (true)
                    {
                        if(_isConnected && _isReceiverTurnedOn && !cancellationToken.IsCancellationRequested)
                        {
                            var result = await udpClient.ReceiveAsync();
                            await ProcessUdpPacket(result.Buffer, writer, cancellationToken);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("UDP listener stopped.");
                    Console.WriteLine($"Error while listening for UDP packets: {ex.Message}");
                }
            }, cancellationToken);
        }

        private async Task ProcessUdpPacket(byte[]? packet, StreamWriter writer, CancellationToken cancellationToken)
        {
            // TODO add extra validation if needed
            if (packet is null || packet.Length < CommandConstants.MinimumUdpPacketLength) // Minimum length for header
            {
                return;
            }

            // Extract I/Q data (skip the first 4 bytes - header)
            var iqData = packet[4..];

            // Convert binary I/Q data to a hex string for logging or save raw bytes if needed
            var iqDataHex = BitConverter.ToString(iqData).Replace("-", " ");

            await writer.WriteLineAsync(iqDataHex.AsMemory(), cancellationToken);
            await writer.FlushAsync(cancellationToken);

            Console.WriteLine($"Received message from udp: {iqDataHex}");
        }

        public void Dispose() => Disconnect();
    }
}