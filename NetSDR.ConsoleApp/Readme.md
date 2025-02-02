NetSDR Console Application

Overview
The NetSDR Console Application is a .NET 8.0 console tool for interacting with a NetSDR receiver via TCP and handling IQ data over UDP. 
The application provides various commands to connect, disconnect, start/stop IQ transmission, and change the frequency of the receiver.

Features
- Establish a TCP connection to the NetSDR receiver.
- Receive IQ data over UDP.
- Start and stop IQ data transmission.
- Adjust the receiver frequency.

Configurable environment settings via environments.json.

Prerequisites
- .NET 8.0 SDK
- A NetSDR receiver device
- A valid environments.json configuration file (see below)

Usage
Run the application using the following command:
dotnet run --project NetSDR.ConsoleApp

Upon execution, you will be prompted to enter a command number corresponding to an available option.

Available Commands

1 Connect to NetSDR receiver
2 Disconnect from receiver
3 Start I/Q transmitting
4 Stop I/Q transmitting
5 Change Frequency

After selecting command 5, you will be prompted to enter the desired frequency in Hz.

Configuration
Ensure the environments.json file is correctly set up before running the application.

Benchmarking
The application supports benchmarking using BenchmarkDotNet:

Testing
Run unit tests using:
dotnet test