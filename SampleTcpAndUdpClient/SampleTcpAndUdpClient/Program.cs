using System.Net;
using System.Net.Sockets;

namespace SampleTcpAndUdpClient;

public static class Program
{
    static async Task Main()
    {
        var tcpTask = StartTcpTestClient(50000);
        var udpTask = StartUdpTestClient(60000);
        await Task.WhenAll(tcpTask, udpTask);
    }

    private static async Task StartTcpTestClient(int port)
    {
        var localEndPoint = new IPEndPoint(IPAddress.Any, port);
        using var listener = new TcpListener(localEndPoint);

        try
        {
            listener.Start();
            Console.WriteLine($"TCP Client started. Waiting for connections on port {port}...");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected!");
                await using var stream = client.GetStream();

                var data = "Hello, TCP!"u8.ToArray();
                try
                {
                    while (true)
                    {

                        await Task.Delay(3000);
                        //await stream.WriteAsync(data, 0, data.Length);
                        //Console.WriteLine("TCP Message Sent");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling TCP client: {ex.Message}");
                }
                finally
                {
                    client.Close();
                    Console.WriteLine("TCP Client disconnected.");
                }
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"TCP SocketException: {ex.Message}");
        }
        finally
        {
            listener.Stop();
        }
    }


    private static async Task StartUdpTestClient(int port)
    {
        var udpClient = new UdpClient();
        var remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
        Console.WriteLine($"UDP Client started. Waiting for connections on port {port}...");

        var data = new byte[] { 1, 2, 3, 4, 5, 6 };
        try
        {
            udpClient.Connect(remoteEndPoint);
            while (true)
            {

                await Task.Delay(3000);
                await udpClient.SendAsync(data, data.Length); // Using Send after Connect
                Console.WriteLine("UDP Message Sent");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"SocketException: {ex.Message}");
        }
        finally
        {
            udpClient.Close();
        }
    }
}