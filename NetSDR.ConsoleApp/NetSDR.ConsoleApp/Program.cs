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

        var serviceProvider = new ServiceCollection()
            .AddTransient<INetSdrClient, NetSdrClient>()
            .BuildServiceProvider();
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
            var command = GetCommandIdentifier(inputValue, availableCommands);
            switch (command)
            {
                case 1:
                    netSdrClient.Connect(environmentSettings.Host, environmentSettings.TcpPort);
                    break;
                case 2:
                    netSdrClient.Disconnect();
                    break;
                case 3:
                    Console.WriteLine("Starting I/Q transmitting...");
                    netSdrClient.StartStopReceiver(true, environmentSettings.StartStopReceiver);
                    break;
                case 4:
                    Console.WriteLine("Stopping I/Q transmitting...");
                    netSdrClient.StartStopReceiver(false, environmentSettings.StartStopReceiver);
                    break;
                case 5:
                    Console.WriteLine("Enter frequency value in Hz");
                    var frequencyInputValue = Console.ReadLine();
                    var frequency = GetFrequency(frequencyInputValue);
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
                case 6:
                    Console.WriteLine("Exiting application...");
                    return;
                default:
                    Console.WriteLine("Invalid command. Please try again.");
                    break;
            }

            Console.WriteLine("\nEnter the command");
        }
    }

    private static IReadOnlyCollection<SdrCommand> GetSdrCommands()
    {
        return new List<SdrCommand>()
        {
            new(1, "Connect"),
            new(2, "Disconnect"),
            new(3, "Start I/Q transmitting"),
            new(4, "Stop I/Q transmitting"),
            new(5, "Change Frequency")
        };
    }

    private static int GetCommandIdentifier(string? inputValue, IReadOnlyCollection<SdrCommand> availableCommands)
    {
        if (!string.IsNullOrEmpty(inputValue) && int.TryParse(inputValue, out var input) && availableCommands.Any(c => c.Identifier == input))
        {
            return input;
        }

        return ErrorCode;
    }

    private static int GetFrequency(string? inputValue)
    {
        if (!string.IsNullOrEmpty(inputValue) && int.TryParse(inputValue, out var input))
        {
            return input;
        }

        return ErrorCode;
    }

    private static EnvironmentSettings ReadEnvironmentSettings()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<EnvironmentSettings>(File.ReadAllText("environments.json"), options);
        return result ?? throw new Exception("No settings found");
    }
}
