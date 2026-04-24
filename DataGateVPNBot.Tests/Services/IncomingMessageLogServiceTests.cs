using DataGateVPNBot.Services.BotServices;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Responses;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class IncomingMessageLogServiceTests
{
    [Fact]
    public async Task Log_Message_Calls_Sender_With_Request_Containing_User_And_Text()
    {
        AddMessageRequest? capturedRequest = null;
        var sender = new Mock<IIncomingMessageLogSenderService>();
        sender.Setup(s => s.TelegramBotIncomingMessageLogAddMessageAsync(It.IsAny<AddMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AddMessageRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new AddMessageResponse());

        var errorService = new Mock<IErrorService>();
        var sut = new IncomingMessageLogService(sender.Object, errorService.Object, Mock.Of<ILogger<IncomingMessageLogService>>());
        var botClient = Mock.Of<ITelegramBotClient>();
        var msg = new Message
        {
            From = new User { Id = 42, Username = "testuser", FirstName = "Test", LastName = "User", IsBot = false },
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 1, Type = ChatType.Private },
            Text = "Hello"
        };

        await sut.Log(botClient, msg, CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest!.Message);
        Assert.Equal(42, capturedRequest.Message.TelegramId);
        Assert.Equal("testuser", capturedRequest.Message.Username);
        Assert.Equal("Hello", capturedRequest.Message.MessageText);
        sender.Verify(s => s.TelegramBotIncomingMessageLogAddMessageAsync(It.IsAny<AddMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Log_Message_Creates_File_In_Logs_Directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "IncomingMessageLogServiceTests_" + Guid.NewGuid().ToString("N"));
        var sender = new Mock<IIncomingMessageLogSenderService>();
        sender.Setup(s => s.TelegramBotIncomingMessageLogAddMessageAsync(It.IsAny<AddMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddMessageResponse());
        var errorService = new Mock<IErrorService>();
        var sut = new IncomingMessageLogService(sender.Object, errorService.Object, Mock.Of<ILogger<IncomingMessageLogService>>());
        var botClient = Mock.Of<ITelegramBotClient>();
        var msg = new Message
        {
            From = new User { Id = 99, Username = "filetest", FirstName = "F", LastName = "T", IsBot = false },
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 1, Type = ChatType.Private },
            Text = "File test"
        };
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.CreateDirectory(tempDir);
            Directory.SetCurrentDirectory(tempDir);
            await sut.Log(botClient, msg, CancellationToken.None);

            var logsDir = Path.Combine(tempDir, "Logs");
            Assert.True(Directory.Exists(logsDir));
            var files = Directory.GetFiles(logsDir, "99_*.json");
            Assert.NotEmpty(files);
            var content = await File.ReadAllTextAsync(files[0]);
            Assert.Contains("99", content);
            Assert.Contains("File test", content);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
