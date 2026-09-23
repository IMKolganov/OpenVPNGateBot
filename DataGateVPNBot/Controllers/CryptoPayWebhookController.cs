using System.Text;
using DataGateVPNBot.Localization;
using DataGateVPNBot.Services.CryptoPay;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Requests;
using DataGateMonitor.SharedModels.Enums;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;

namespace DataGateVPNBot.Controllers;

[ApiController]
[Route("api/cryptopay")]
public class CryptoPayWebhookController(
    ICryptoPayDonationService donationService,
    ITelegramBotClient botClient,
    ILocalizationService localizationService,
    IErrorService errorService,
    ILogger<CryptoPayWebhookController> logger) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            rawBody = await reader.ReadToEndAsync(cancellationToken);

        CryptoPayWebhookSignature.TryGetHeader(Request.Headers, out var signature);

        CryptoPayPaidDonation? paid;
        try
        {
            paid = await donationService.ProcessWebhookAsync(rawBody, signature, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Rejected Crypto Pay webhook.");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Crypto Pay webhook failed.");
            await errorService.NotifyAdminsAboutExceptionAsync(ex, HttpContext, cancellationToken);
            return StatusCode(500);
        }

        if (paid is null || paid.IsDuplicate)
            return Ok();

        var adminText =
            "💚 Crypto donation received\n" +
            $"Invoice: {paid.InvoiceId}\n" +
            $"Amount: {paid.Amount} {paid.Asset}\n" +
            $"TelegramId: {paid.TelegramId?.ToString() ?? "—"}\n" +
            $"Comment: {(string.IsNullOrWhiteSpace(paid.Comment) ? "—" : paid.Comment)}";

        await errorService.SendMessageToAdminsAsync(adminText, cancellationToken);

        if (paid.TelegramId is > 0)
        {
            try
            {
                await botClient.SendMessage(
                    paid.TelegramId.Value,
                    await GetThanksTextAsync(paid, cancellationToken),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to thank donor TelegramId={TelegramId}", paid.TelegramId);
            }
        }

        return Ok();
    }

    private async Task<string> GetThanksTextAsync(CryptoPayPaidDonation paid, CancellationToken cancellationToken)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["amount"] = paid.Amount,
            ["asset"] = paid.Asset
        };

        try
        {
            if (paid.TelegramId is > 0)
            {
                var text = (await localizationService.GetTextForTelegramUser(
                    new GetTextForTelegramUserRequest
                    {
                        TelegramId = paid.TelegramId.Value,
                        Key = "DonateThanks"
                    },
                    cancellationToken)).Text;
                if (!string.IsNullOrWhiteSpace(text) &&
                    !text.StartsWith("[Translation missing", StringComparison.Ordinal))
                    return LocalizationPlaceholderFormatter.Apply(text, placeholders);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DonateThanks localization failed for {TelegramId}", paid.TelegramId);
        }

        var language = Language.English;
        try
        {
            if (paid.TelegramId is > 0)
            {
                language = (await localizationService.GetTelegramUserLanguageAsync(
                    new GetTelegramUserLanguageRequest { TelegramId = paid.TelegramId.Value },
                    cancellationToken)).PreferredLanguage;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DonateThanks language lookup failed for {TelegramId}", paid.TelegramId);
        }

        return LocalizationPlaceholderFormatter.Apply(DonateTextFallbacks.Get("DonateThanks", language), placeholders);
    }
}
