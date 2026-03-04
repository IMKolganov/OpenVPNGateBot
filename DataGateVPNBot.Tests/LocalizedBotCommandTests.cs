using DataGateVPNBot.Models;
using Xunit;

namespace DataGateVPNBot.Tests;

public class LocalizedBotCommandTests
{
    [Fact]
    public void ToTelegramCommand_Returns_Description_For_Existing_LangCode()
    {
        var cmd = new LocalizedBotCommand
        {
            Command = "/test",
            Descriptions = new Dictionary<string, string> { ["en"] = "English", ["ru"] = "Russian" }
        };

        var en = cmd.ToTelegramCommand("en");
        Assert.Equal("/test", en.Command);
        Assert.Equal("English", en.Description);

        var ru = cmd.ToTelegramCommand("ru");
        Assert.Equal("Russian", ru.Description);
    }

    [Fact]
    public void ToTelegramCommand_Falls_Back_To_En_When_LangCode_Missing()
    {
        var cmd = new LocalizedBotCommand
        {
            Command = "/foo",
            Descriptions = new Dictionary<string, string> { ["en"] = "English text" }
        };

        var result = cmd.ToTelegramCommand("el");
        Assert.Equal("/foo", result.Command);
        Assert.Equal("English text", result.Description);
    }

    [Fact]
    public void ToTelegramCommand_Falls_Back_To_Command_When_En_Also_Missing()
    {
        var cmd = new LocalizedBotCommand
        {
            Command = "/bar",
            Descriptions = new Dictionary<string, string> { ["ru"] = "Russian" }
        };

        var result = cmd.ToTelegramCommand("el");
        Assert.Equal("/bar", result.Command);
        Assert.Equal("/bar", result.Description);
    }
}
