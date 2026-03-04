using DataGateVPNBot.Handlers;
using Xunit;

namespace DataGateVPNBot.Tests;

public class BotCommandsTests
{
    [Fact]
    public void All_Command_Constants_Start_With_Slash()
    {
        Assert.StartsWith("/", BotCommands.CommandStart);
        Assert.StartsWith("/", BotCommands.CommandGetMyFiles);
        Assert.StartsWith("/", BotCommands.CommandMakeNewFile);
        Assert.StartsWith("/", BotCommands.CommandAboutBot);
        Assert.StartsWith("/", BotCommands.CommandHowToUse);
        Assert.StartsWith("/", BotCommands.CommandChangeLanguage);
    }

    [Fact]
    public void Main_Commands_Are_Non_Empty()
    {
        Assert.False(string.IsNullOrEmpty(BotCommands.CommandStart));
        Assert.False(string.IsNullOrEmpty(BotCommands.CommandGetMyFiles));
        Assert.False(string.IsNullOrEmpty(BotCommands.CommandGetMyFilesWithToken));
        Assert.False(string.IsNullOrEmpty(BotCommands.CommandGetMyFilesWithoutToken));
        Assert.False(string.IsNullOrEmpty(BotCommands.CommandRegister));
    }
}
