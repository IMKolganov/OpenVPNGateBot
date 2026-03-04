using DataGateVPNBot.Services.BotServices;
using OpenVPNGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class TelegramSettingsServiceTests
{
    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Russian)]
    [InlineData(Language.Greek)]
    public void GetTelegramMenuByLanguage_Returns_Commands_For_Language(Language language)
    {
        var sut = new TelegramSettingsService();
        var commands = sut.GetTelegramMenuByLanguage(language);

        Assert.NotNull(commands);
        Assert.True(commands.Length > 0);
        foreach (var cmd in commands)
        {
            Assert.False(string.IsNullOrEmpty(cmd.Command));
            Assert.False(string.IsNullOrEmpty(cmd.Description));
        }
    }

    [Fact]
    public void GetTelegramMenuByLanguage_English_Contains_Expected_Commands()
    {
        var sut = new TelegramSettingsService();
        var commands = sut.GetTelegramMenuByLanguage(Language.English);

        var commandStrings = commands.Select(c => c.Command).ToArray();
        Assert.Contains("/get_my_files", commandStrings);
        Assert.Contains("/make_new_file", commandStrings);
        Assert.Contains("/how_to_use", commandStrings);
    }

    [Fact]
    public void GetTelegramMenuByLanguage_Invalid_Enum_Throws()
    {
        var sut = new TelegramSettingsService();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.GetTelegramMenuByLanguage((Language)999));
    }
}
