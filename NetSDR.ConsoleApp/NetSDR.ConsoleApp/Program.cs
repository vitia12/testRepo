using Microsoft.Extensions.DependencyInjection;
using NetSDR.Library.Models;
using NetSDR.Library.Services;
using System.Text.Json;

namespace NetSDR.ConsoleApp;

public static class Program
{
    private const int ErrorCode = -1;

    public static void Main()
    {
        Console.WriteLine("".PadLeft(80, '='));
        Console.WriteLine("\nWelcome to NetSdr Sample Tool!");
        Console.WriteLine("\nAvailable options: Please enter number");

        var serviceProvider = RegisterServices();
        var netSdrClient = serviceProvider.GetRequiredService<INetSdrClient>();

        var availableCommands = GetSdrCommands();
        var environmentSettings = ReadEnvironmentSettings();
        foreach (var availableCommand in availableCommands)
        {
            Console.WriteLine($"{availableCommand.Identifier} - {availableCommand.Description}");
        }

        while (true)
        {
            var inputValue = Console.ReadLine();
            var command = GetParsedCommandId(inputValue, availableCommands);
            switch (command)
            {
                case 1:
                    netSdrClient.Connect(environmentSettings.Host, environmentSettings.TcpPort, environmentSettings.UdpPort);
                    break;
                case 2:
                    netSdrClient.Disconnect();
                    break;
                case 3:
                    Console.WriteLine("Starting I/Q transmitting...");
                    netSdrClient.ToggleReceiverState(true, environmentSettings.Receiver);
                    break;
                case 4:
                    Console.WriteLine("Stopping I/Q transmitting...");
                    netSdrClient.ToggleReceiverState(false, environmentSettings.Receiver);
                    break;
                case 5:
                    Console.WriteLine("Enter frequency value in Hz");
                    var frequencyInputValue = Console.ReadLine();
                    var frequency = GetParsedFrequency(frequencyInputValue);
                    if (frequency != ErrorCode)
                    {
                        Console.WriteLine("Changing frequency...");
                        netSdrClient.SetFrequency(frequency, environmentSettings.FrequencyConfig);
                    }
                    else
                    {
                        Console.WriteLine("Enter valid digital value");
                    }

                    break;
                default:
                    Console.WriteLine("Invalid command. Please try again.");
                    break;
            }

            Console.WriteLine("\nEnter the command");
        }
    }

    private static ServiceProvider RegisterServices()
    {
        var serviceProvider = new ServiceCollection()
            .AddTransient<INetSdrClient, NetSdrServer>()
            .AddTransient<ITcpClientWrapper, TcpClientWrapper>()
            .AddTransient<IUdpClientWrapper, UdpClientWrapper>()
            .BuildServiceProvider();
        return serviceProvider;
    }

    private static IReadOnlyCollection<SdrCommand> GetSdrCommands() =>
    [
        new(1, "Connect"),
        new(2, "Disconnect"),
        new(3, "Start I/Q transmitting"),
        new(4, "Stop I/Q transmitting"),
        new(5, "Change Frequency")
    ];

    private static int GetParsedCommandId(string? inputValue, IReadOnlyCollection<SdrCommand> availableCommands)
    {
        return !string.IsNullOrEmpty(inputValue) && int.TryParse(inputValue, out var input) && availableCommands.Any(c => c.Identifier == input)
            ? input
            : ErrorCode;
    }

    private static int GetParsedFrequency(string? inputValue) => !string.IsNullOrEmpty(inputValue) && int.TryParse(inputValue, out var input) ? input : ErrorCode;

    private static EnvironmentSettings ReadEnvironmentSettings()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<EnvironmentSettings>(File.ReadAllText("environments.json"), options);
        return result ?? throw new Exception("No settings found");
    }
}
