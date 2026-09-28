using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.BotServices;
using DataGateMonitor.SharedModels.Enums;
using Microsoft.Extensions.Options;
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
        var sut = CreateSut(donationsEnabled: false);
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
    public void GetTelegramMenuByLanguage_Hides_Donate_When_Donations_Disabled_By_Default()
    {
        var sut = CreateSut(donationsEnabled: false);
        var commands = sut.GetTelegramMenuByLanguage(Language.English);

        var commandStrings = commands.Select(c => c.Command).ToArray();
        Assert.Contains("/get_my_files", commandStrings);
        Assert.Contains("/make_new_file", commandStrings);
        Assert.Contains("/how_to_use", commandStrings);
        Assert.DoesNotContain("/donate", commandStrings);
        Assert.DoesNotContain("/unsubscribed_vpn_users", commandStrings);
        Assert.DoesNotContain("/refresh_profile_photos", commandStrings);
        Assert.DoesNotContain("/remind_channel_subscribe", commandStrings);
        Assert.DoesNotContain("/remind_channel_email", commandStrings);
    }

    [Fact]
    public void GetTelegramMenuByLanguage_Includes_Donate_When_Stars_Enabled()
    {
        var sut = CreateSut(
            stars: new TelegramStarsConfiguration { Enabled = true },
            crypto: new CryptoPayConfiguration());

        var commandStrings = sut.GetTelegramMenuByLanguage(Language.English)
            .Select(c => c.Command)
            .ToArray();

        Assert.Contains("/donate", commandStrings);
    }

    [Fact]
    public void GetTelegramMenuByLanguage_Includes_Donate_When_CryptoPay_Configured()
    {
        var sut = CreateSut(
            stars: new TelegramStarsConfiguration(),
            crypto: new CryptoPayConfiguration { Enabled = true, ApiToken = "token" });

        var commandStrings = sut.GetTelegramMenuByLanguage(Language.English)
            .Select(c => c.Command)
            .ToArray();

        Assert.Contains("/donate", commandStrings);
    }

    [Fact]
    public void GetTelegramMenuByLanguage_WithAdminCommands_IncludesAdminOnlyEntries()
    {
        var sut = CreateSut(donationsEnabled: false);
        var commands = sut.GetTelegramMenuByLanguage(Language.English, includeAdminCommands: true);

        var commandStrings = commands.Select(c => c.Command).ToArray();
        Assert.Contains("/unsubscribed_vpn_users", commandStrings);
        Assert.Contains("/refresh_profile_photos", commandStrings);
        Assert.Contains("/remind_channel_subscribe", commandStrings);
        Assert.Contains("/remind_channel_email", commandStrings);
        Assert.Contains("/get_my_files", commandStrings);
        Assert.DoesNotContain("/donate", commandStrings);
    }

    [Fact]
    public void GetTelegramMenuByLanguage_Invalid_Enum_Throws()
    {
        var sut = CreateSut(donationsEnabled: false);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.GetTelegramMenuByLanguage((Language)999));
    }

    private static TelegramSettingsService CreateSut(
        bool donationsEnabled = false,
        TelegramStarsConfiguration? stars = null,
        CryptoPayConfiguration? crypto = null)
    {
        stars ??= new TelegramStarsConfiguration { Enabled = donationsEnabled };
        crypto ??= new CryptoPayConfiguration
        {
            Enabled = donationsEnabled,
            ApiToken = donationsEnabled ? "token" : string.Empty
        };
        return new TelegramSettingsService(Options.Create(stars), Options.Create(crypto));
    }
}
