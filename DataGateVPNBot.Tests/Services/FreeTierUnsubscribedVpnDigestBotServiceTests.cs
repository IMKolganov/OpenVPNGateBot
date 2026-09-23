using System.Security.Authentication;
using DataGateVPNBot.Handlers;
using DataGateVPNBot.Services.BotServices;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Enums;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Responses;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class FreeTierUnsubscribedVpnDigestBotServiceTests
{
    [Fact]
    public async Task GetDigestAsync_ReturnsStructuredData_WhenApiSucceeds()
    {
        var http = new Mock<IHttpRequestService>();
        http.Setup(h => h.PostAsync<ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>
            {
                Success = true,
                Data = new DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse { Token = "t" },
            });
        http.Setup(h => h.GetAsync<ApiResponse<FreeTierUnsubscribedVpnDigestResponse>>(
                "api/free-tier-enforcement/unsubscribed-vpn-digest", "t", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<FreeTierUnsubscribedVpnDigestResponse>
            {
                Success = true,
                Data = new FreeTierUnsubscribedVpnDigestResponse
                {
                    Text = "digest-text",
                    Candidates =
                    [
                        new FreeTierEnforcementCandidateDto { UserId = 150, Email = "a@b.c" },
                        new FreeTierEnforcementCandidateDto { UserId = 7, Email = null },
                    ],
                },
            });

        var auth = new AuthService(http.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new FreeTierUnsubscribedVpnDigestBotService(
            auth, http.Object, Mock.Of<ILogger<FreeTierUnsubscribedVpnDigestBotService>>());

        var digest = await sut.GetDigestAsync(CancellationToken.None);

        Assert.NotNull(digest);
        Assert.Equal("digest-text", digest!.Text);
        Assert.Equal(2, digest.Candidates.Count);
        Assert.Equal("a@b.c", digest.Candidates[0].Email);
    }

    [Fact]
    public async Task GetDigestAsync_Throws_WhenNoToken()
    {
        var http = new Mock<IHttpRequestService>();
        http.Setup(h => h.PostAsync<ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>
            {
                Success = false,
            });

        var auth = new AuthService(http.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new FreeTierUnsubscribedVpnDigestBotService(
            auth, http.Object, Mock.Of<ILogger<FreeTierUnsubscribedVpnDigestBotService>>());

        await Assert.ThrowsAsync<AuthenticationException>(() => sut.GetDigestAsync(CancellationToken.None));
    }
}

public class FreeTierRemindKeyboardTests
{
    [Fact]
    public void BuildRemindKeyboard_IncludesTgAndEmailButtons()
    {
        var keyboard = TelegramUpdateHandler.BuildRemindKeyboard(
        [
            new FreeTierEnforcementCandidateDto
            {
                UserId = 22,
                DisplayName = "Irina",
                Email = "irina@x.com",
                TelegramId = 439938925,
            },
            new FreeTierEnforcementCandidateDto
            {
                UserId = 150,
                DisplayName = "Tatyana",
                Email = "t@x.com",
                TelegramId = null,
            },
            new FreeTierEnforcementCandidateDto
            {
                UserId = 7,
                DisplayName = "NoContact",
                Email = null,
                TelegramId = null,
            },
        ]);

        Assert.NotNull(keyboard);
        var buttons = keyboard!.InlineKeyboard.SelectMany(r => r).ToList();
        Assert.Contains(buttons, b => b.Text == "TG all" && b.CallbackData == "/remind_channel_subscribe all");
        Assert.Contains(buttons, b => b.Text == "Email all" && b.CallbackData == "/remind_channel_email all");
        Assert.Contains(buttons, b => b.Text == "TG #22" && b.CallbackData == "/remind_channel_subscribe 22");
        Assert.Contains(buttons, b => b.Text == "Email #22" && b.CallbackData == "/remind_channel_email 22");
        Assert.Contains(buttons, b => b.Text == "Email #150" && b.CallbackData == "/remind_channel_email 150");
        Assert.DoesNotContain(buttons, b => b.CallbackData!.Contains(" 7"));

        var firstRow = keyboard.InlineKeyboard.First().ToList();
        Assert.Equal(2, firstRow.Count);
        Assert.Equal("TG all", firstRow[0].Text);
        Assert.Equal("Email all", firstRow[1].Text);
    }

    [Fact]
    public void BuildRemindKeyboard_AllRow_OnlyTg_WhenNoEmails()
    {
        var keyboard = TelegramUpdateHandler.BuildRemindKeyboard(
        [
            new FreeTierEnforcementCandidateDto
            {
                UserId = 1,
                DisplayName = "A",
                TelegramId = 100,
                Email = null,
            },
        ]);

        Assert.NotNull(keyboard);
        var firstRow = keyboard!.InlineKeyboard.First().ToList();
        Assert.Single(firstRow);
        Assert.Equal("TG all", firstRow[0].Text);
        Assert.DoesNotContain(keyboard.InlineKeyboard.SelectMany(r => r), b => b.Text == "Email all");
    }

    [Fact]
    public void BuildRemindKeyboard_ReturnsNull_WhenNoContacts()
    {
        var keyboard = TelegramUpdateHandler.BuildRemindKeyboard(
        [
            new FreeTierEnforcementCandidateDto { UserId = 1, Email = null, TelegramId = null },
        ]);

        Assert.Null(keyboard);
    }

    [Fact]
    public void GetRemindAllTargets_FiltersByChannel()
    {
        FreeTierEnforcementCandidateDto[] candidates =
        [
            new() { UserId = 22, DisplayName = "Irina", TelegramId = 1, Email = "a@b.c" },
            new() { UserId = 150, DisplayName = "Tatyana", TelegramId = null, Email = "t@x.com" },
            new() { UserId = 7, DisplayName = "NoContact", TelegramId = null, Email = null },
        ];

        var tg = TelegramUpdateHandler.GetRemindAllTargets(
            candidates, FreeTierChannelSubscribeRemindChannel.Telegram);
        var email = TelegramUpdateHandler.GetRemindAllTargets(
            candidates, FreeTierChannelSubscribeRemindChannel.Email);

        Assert.Equal(["22"], tg);
        Assert.Equal(["22", "150"], email);
    }

    [Fact]
    public void FormatRemindAllSummary_AllOk()
    {
        var text = TelegramUpdateHandler.FormatRemindAllSummary("TG", 3, 3, []);
        Assert.Equal("✅ TG reminders: 3/3 sent.", text);
    }

    [Fact]
    public void FormatRemindAllSummary_IncludesFailures()
    {
        var text = TelegramUpdateHandler.FormatRemindAllSummary(
            "Email", 2, 1, ["#150: User has no email"]);
        Assert.StartsWith("⚠️ Email reminders: 1/2 sent.", text);
        Assert.Contains("• #150: User has no email", text);
    }
}
