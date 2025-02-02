using FluentAssertions;
using Moq;
using NetSDR.Library.Models;
using NetSDR.Library.Services;
using Xunit;

namespace NetSDR.Library.Test;

public class NetSdrServerTests
{
    private readonly Mock<ITcpClientWrapper> _mockTcpClient;
    private readonly NetSdrServer _server;
    private readonly StringWriter _stringWriter;

    public NetSdrServerTests()
    {
        _mockTcpClient = new Mock<ITcpClientWrapper>();
        var udpMock = new Mock<IUdpClientWrapper>();

        _mockTcpClient.Setup(c => c.GetStream()).Returns(new MemoryStream());

        _stringWriter = new StringWriter();
        Console.SetOut(_stringWriter);

        _server = new NetSdrServer(_mockTcpClient.Object, udpMock.Object);
    }

    [Fact]
    public void Connect_ShouldEstablishConnection()
    {
        _mockTcpClient.Setup(c => c.Connect(It.IsAny<string>(), It.IsAny<int>()));
        _server.Connect("127.0.0.1", 50000, 60000);
        _mockTcpClient.Verify(c => c.Connect(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void Connect_WhenAlreadyConnected_ShouldNotReconnect()
    {
        // GIVEN
        _server.Connect("127.0.0.1", 50000, 60000);

        // WHEN
        _server.Connect("127.0.0.1", 50000, 60000);

        // ASSERT
        var consoleContent = _stringWriter.ToString();
        consoleContent.Should().Contain("Already connected.");
    }

    [Fact]
    public void Disconnect_ShouldCloseTcpAndUdpConnections()
    {
        // GIVEN
        _server.Connect("127.0.0.1", 50000, 60000);

        // WHEN
        _server.Disconnect();

        // ASSERT
        _mockTcpClient.Verify(c => c.Disconnect(), Times.Once);
    }

    [Fact]
    public void Disconnect_WhenNotConnected_ShouldNotThrow()
    {
        // WHEN
        _server.Disconnect();

        // ASSERT
        var consoleContent = _stringWriter.ToString();
        consoleContent.Should().Contain("Not connected.");
    }

    [Fact]
    public void StartReceiver_ShouldSendValidCommand()
    {
        // GIVEN
        _server.Connect("127.0.0.1", 50000, 60000);
        var config = new ReceiverConfig { CaptureMode = 0x80, IsComplex = true };

        // WHEN
        _server.ToggleReceiverState(true, config);
        _server.Disconnect();

        // ASSERT
        var consoleContent = _stringWriter.ToString();
        consoleContent.Should().Contain("0800180080028000");
    }

    [Fact]
    public void StopReceiver_ShouldSendValidCommand()
    {
        // GIVEN
        _server.Connect("127.0.0.1", 50000, 60000);
        var config = new ReceiverConfig { CaptureMode = 0x80, IsComplex = true };

        // WHEN
        _server.ToggleReceiverState(false, config);
        _server.Disconnect();

        // ASSERT
        var consoleContent = _stringWriter.ToString();
        consoleContent.Should().Contain("0800180080018000");
    }


    [Fact]
    public void StartStopReceiver_WithInvalidCaptureMode_ShouldNotSendCommand()
    {
        // GIVEN
        _server.Connect("127.0.0.1", 50000, 60000);
        var config = new ReceiverConfig { CaptureMode = 0x99, IsComplex = true };

        // WHEN
        _server.ToggleReceiverState(true, config);
        _server.Disconnect();

        // ASSERT
        var consoleContent = _stringWriter.ToString();
        consoleContent.Should().Contain("Invalid capture mode.");
    }

    [Fact]
    public void StartStopReceiver_WithFifoModeZeroSamples_ShouldNotSendCommand()
    {
        // GIVEN
        _server.Connect("127.0.0.1", 50000, 60000);
        var config = new ReceiverConfig { CaptureMode = 0x01, FifoSamples = 0 };

        // WHEN
        _server.ToggleReceiverState(true, config);
        _server.Disconnect();

        // ASSERT
        var consoleContent = _stringWriter.ToString();
        consoleContent.Should().Contain("FIFO mode requires a non-zero number of samples.");
    }

    [Fact]
    public void StartStopReceiver_WhenNotConnected_ShouldNotSendCommand()
    {
        // GIVEN
        var config = new ReceiverConfig { CaptureMode = 0x80, IsComplex = true };

        // WHEN
        _server.ToggleReceiverState(true, config);
        _server.Disconnect();

        // ASSERT
        var consoleContent = _stringWriter.ToString();
        consoleContent.Should().Contain("Not connected.");
    }

    [Fact]
    public void SetFrequency_ShouldSendValidCommand()
    {
        // GIVEN
        _server.Connect("127.0.0.1", 50000, 60000);
        var config = new FrequencyConfig { ChannelId = 0x00 };

        // WHEN
        _server.SetFrequency(14010000, config);
        _server.Disconnect();

        // ASSERT
        var consoleContent = _stringWriter.ToString();
        consoleContent.Should().Contain("0A0020000090C6D50000");
    }

    [Fact]
    public void SetFrequency_WhenNotConnected_ShouldNotSendCommand()
    {
        // GIVEN
        var config = new FrequencyConfig { ChannelId = 0x00 };

        // WHEN
        _server.SetFrequency(1000000000, config);
        _server.Disconnect();

        // ASSERT
        var consoleContent = _stringWriter.ToString();
        consoleContent.Should().Contain("Not connected.");
    }

    [Fact]
    public void SetFrequency_WithNegativeValue_ShouldNotSendCommand()
    {
        // GIVEN
        _server.Connect("127.0.0.1", 50000, 60000);
        var config = new FrequencyConfig { ChannelId = 0x00 };

        // WHEN
        _server.SetFrequency(-1, config);
        _server.Disconnect();

        // ASSERT
        var consoleContent = _stringWriter.ToString();
        consoleContent.Should().Contain("Frequency must be a non-negative value.");
    }
}