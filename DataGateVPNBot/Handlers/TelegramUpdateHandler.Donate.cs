using DataGateVPNBot.Configurations;
using DataGateVPNBot.Localization;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.CryptoPay;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Donations;
using DataGateVPNBot.Services.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Requests;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    private async Task<Message> Donate(Message msg, CancellationToken cancellationToken)
    {
        return await SendDonateMenuAsync(msg.Chat.Id, msg.From!.Id, cancellationToken);
    }

    private async Task HandleDonateCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var message = callbackQuery.Message ?? throw new InvalidOperationException("Message is null.");
        var data = callbackQuery.Data?.Trim() ?? string.Empty;

        if (data.Equals(CryptoPayDonationService.CallbackMenu, StringComparison.OrdinalIgnoreCase))
        {
            await SendDonateMenuAsync(message.Chat.Id, callbackQuery.From.Id, cancellationToken);
            return;
        }

        if (StarsDonation.TryParseCallback(data, out var stars))
        {
            await SendStarsInvoiceAsync(message.Chat.Id, callbackQuery.From.Id, stars, cancellationToken);
            return;
        }

        if (CryptoPayDonationService.TryParseConfirmCallback(data, out var confirmedUsd))
        {
            await SendCryptoInvoiceAsync(message.Chat.Id, callbackQuery.From.Id, confirmedUsd, cancellationToken);
            return;
        }

        if (CryptoPayDonationService.TryParseAmountCallback(data, out var amountUsd))
        {
            await SendCryptoRiskBannerAsync(message.Chat.Id, callbackQuery.From.Id, amountUsd, cancellationToken);
            return;
        }

        await _botClient.SendMessage(
            message.Chat.Id,
            await GetDonateTextAsync("DonateInvoiceFailed", callbackQuery.From.Id, cancellationToken),
            cancellationToken: cancellationToken);
    }

    private async Task SendStarsInvoiceAsync(
        long chatId,
        long telegramId,
        int stars,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var starsConfig = scope.ServiceProvider.GetRequiredService<IOptions<TelegramStarsConfiguration>>().Value;
        var allowed = TelegramStarsConfigurationHelper.ParseAmounts(starsConfig.Amounts);
        if (!starsConfig.IsConfigured || !allowed.Contains(stars))
        {
            await _botClient.SendMessage(
                chatId,
                await GetDonateTextAsync("DonateInvoiceFailed", telegramId, cancellationToken),
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var title = await GetDonateTextAsync("DonateStarsTitle", telegramId, cancellationToken);
            var description = await GetDonateTextAsync("DonateStarsDescription", telegramId, cancellationToken);
            await _botClient.SendInvoice(
                chatId,
                title.Length <= 32 ? title : title[..32],
                description.Length <= 255 ? description : description[..255],
                StarsDonation.Payload(telegramId, stars),
                StarsDonation.Currency,
                [new LabeledPrice("Donate", stars)],
                providerToken: "",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Stars donation invoice for {TelegramId}", telegramId);
            await _botClient.SendMessage(
                chatId,
                await GetDonateTextAsync("DonateInvoiceFailed", telegramId, cancellationToken),
                cancellationToken: cancellationToken);
        }
    }

    private async Task SendCryptoRiskBannerAsync(
        long chatId,
        long telegramId,
        decimal amountUsd,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var donationService = scope.ServiceProvider.GetRequiredService<ICryptoPayDonationService>();
        if (!donationService.IsConfigured || !donationService.AmountsUsd.Contains(amountUsd))
        {
            await _botClient.SendMessage(
                chatId,
                await GetDonateTextAsync("DonateInvoiceFailed", telegramId, cancellationToken),
                cancellationToken: cancellationToken);
            return;
        }

        var amountLabel = CryptoPayWebhookSignature.FormatUsd(amountUsd);
        var text = await GetDonateTextAsync(
            "DonateCryptoRiskBanner",
            telegramId,
            new Dictionary<string, string> { ["amount"] = amountLabel },
            cancellationToken);
        var continueLabel = await GetDonateTextAsync("DonateCryptoRiskContinue", telegramId, cancellationToken);
        var ignoreLabel = await GetDonateTextAsync("DonateCryptoRiskIgnore", telegramId, cancellationToken);

        await _botClient.SendMessage(
            chatId,
            text,
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup([
                [
                    InlineKeyboardButton.WithCallbackData(
                        TruncateDonateButton(continueLabel),
                        CryptoPayDonationService.ConfirmCallback(amountUsd))
                ],
                [
                    InlineKeyboardButton.WithCallbackData(
                        TruncateDonateButton(ignoreLabel),
                        CryptoPayDonationService.CallbackMenu)
                ]
            ]),
            linkPreviewOptions: LinkPreviewOptions.Disabled,
            cancellationToken: cancellationToken);
    }

    private async Task SendCryptoInvoiceAsync(
        long chatId,
        long telegramId,
        decimal amountUsd,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var donationService = scope.ServiceProvider.GetRequiredService<ICryptoPayDonationService>();
        if (!donationService.IsConfigured || !donationService.AmountsUsd.Contains(amountUsd))
        {
            await _botClient.SendMessage(
                chatId,
                await GetDonateTextAsync("DonateInvoiceFailed", telegramId, cancellationToken),
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var me = await _botClient.GetMe(cancellationToken);
            var invoice = await donationService.CreateDonationInvoiceAsync(
                telegramId,
                amountUsd,
                me.Username,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(invoice.PayUrl))
                throw new InvalidOperationException("Crypto Pay invoice has no payment URL.");

            var amountLabel = CryptoPayWebhookSignature.FormatUsd(amountUsd);
            var text = await GetDonateTextAsync(
                "DonateInvoiceCreated",
                telegramId,
                new Dictionary<string, string> { ["amount"] = amountLabel },
                cancellationToken);
            var payButton = await GetDonateTextAsync(
                "DonatePayButton",
                telegramId,
                new Dictionary<string, string> { ["amount"] = amountLabel },
                cancellationToken);

            await _botClient.SendMessage(
                chatId,
                text,
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithUrl(TruncateDonateButton(payButton), invoice.PayUrl)),
                linkPreviewOptions: LinkPreviewOptions.Disabled,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Crypto Pay donation invoice for {TelegramId}", telegramId);
            await _botClient.SendMessage(
                chatId,
                await GetDonateTextAsync("DonateInvoiceFailed", telegramId, cancellationToken),
                cancellationToken: cancellationToken);
        }
    }

    private async Task<Message> SendDonateMenuAsync(
        long chatId,
        long telegramId,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var starsConfig = scope.ServiceProvider.GetRequiredService<IOptions<TelegramStarsConfiguration>>().Value;
        var crypto = scope.ServiceProvider.GetRequiredService<ICryptoPayDonationService>();
        var starAmounts = TelegramStarsConfigurationHelper.ParseAmounts(starsConfig.Amounts);

        var starsOn = starsConfig.IsConfigured && starAmounts.Count > 0;
        var cryptoOn = crypto.IsConfigured && crypto.AmountsUsd.Count > 0;
        if (!starsOn && !cryptoOn)
        {
            return await _botClient.SendMessage(
                chatId,
                await GetDonateTextAsync("DonateDisabled", telegramId, cancellationToken),
                cancellationToken: cancellationToken);
        }

        var rows = new List<InlineKeyboardButton[]>();
        if (starsOn)
        {
            var row = new List<InlineKeyboardButton>();
            foreach (var stars in starAmounts)
            {
                row.Add(InlineKeyboardButton.WithCallbackData($"⭐ {stars}", StarsDonation.Callback(stars)));
                if (row.Count == 4)
                {
                    rows.Add(row.ToArray());
                    row = [];
                }
            }

            if (row.Count > 0)
                rows.Add(row.ToArray());
        }

        if (cryptoOn)
        {
            var row = new List<InlineKeyboardButton>();
            foreach (var amount in crypto.AmountsUsd)
            {
                var label = $"USDT ${CryptoPayWebhookSignature.FormatUsd(amount)}";
                row.Add(InlineKeyboardButton.WithCallbackData(
                    label,
                    $"{CryptoPayDonationService.CallbackAmountPrefix}{CryptoPayWebhookSignature.FormatUsd(amount)}"));
                if (row.Count == 3)
                {
                    rows.Add(row.ToArray());
                    row = [];
                }
            }

            if (row.Count > 0)
                rows.Add(row.ToArray());
        }

        return await _botClient.SendMessage(
            chatId,
            await GetDonateTextAsync("DonateIntro", telegramId, cancellationToken),
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(rows),
            cancellationToken: cancellationToken);
    }

    private async Task HandleSuccessfulDonationPaymentAsync(
        Message msg,
        SuccessfulPayment payment,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(payment.Currency, StarsDonation.Currency, StringComparison.OrdinalIgnoreCase) ||
            !StarsDonation.TryParsePayload(payment.InvoicePayload, out var telegramId, out var stars))
        {
            _logger.LogInformation(
                "Successful payment ignored. Currency={Currency}; Payload={Payload}",
                payment.Currency, payment.InvoicePayload);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var chargeStore = scope.ServiceProvider.GetRequiredService<StarsPaidChargeStore>();
        if (!string.IsNullOrWhiteSpace(payment.TelegramPaymentChargeId) &&
            !chargeStore.TryMarkProcessed(payment.TelegramPaymentChargeId))
        {
            _logger.LogInformation(
                "Duplicate Stars payment ignored. ChargeId={ChargeId}",
                payment.TelegramPaymentChargeId);
            return;
        }

        var donorId = msg.From?.Id ?? telegramId;
        _logger.LogInformation(
            "Stars donation received. TelegramId={TelegramId}; Stars={Stars}; ChargeId={ChargeId}",
            donorId, stars, payment.TelegramPaymentChargeId);

        var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
        await errorService.SendMessageToAdminsAsync(
            "💚 Stars donation received\n" +
            $"TelegramId: {donorId}\n" +
            $"Amount: {stars} XTR\n" +
            $"ChargeId: {payment.TelegramPaymentChargeId}",
            cancellationToken);

        await _botClient.SendMessage(
            msg.Chat.Id,
            await GetDonateTextAsync(
                "DonateThanks",
                donorId,
                new Dictionary<string, string>
                {
                    ["amount"] = stars.ToString(),
                    ["asset"] = "⭐"
                },
                cancellationToken),
            cancellationToken: cancellationToken);
    }

    private async Task AnswerDonatePreCheckoutAsync(PreCheckoutQuery update, CancellationToken cancellationToken)
    {
        var ok = StarsDonation.IsDonatePreCheckout(update.Currency, update.InvoicePayload);
        try
        {
            await _botClient.SendRequest(
                new AnswerPreCheckoutQueryRequest
                {
                    PreCheckoutQueryId = update.Id,
                    Ok = ok,
                    ErrorMessage = ok ? null : "This invoice is no longer valid."
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to answer pre-checkout {QueryId}", update.Id);
        }
    }

    private async Task<string> GetDonateTextAsync(string key, long telegramId, CancellationToken cancellationToken)
    {
        try
        {
            var text = await GetLocalizationTextAsync(key, telegramId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(text) &&
                !text.StartsWith("[Translation missing", StringComparison.Ordinal))
                return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Donate localization lookup failed for key {Key}", key);
        }

        var language = DataGateMonitor.SharedModels.Enums.Language.English;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
            language = (await localizationService.GetTelegramUserLanguageAsync(
                new GetTelegramUserLanguageRequest { TelegramId = telegramId },
                cancellationToken)).PreferredLanguage;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Donate language lookup failed for {TelegramId}", telegramId);
        }

        return DonateTextFallbacks.Get(key, language);
    }

    private async Task<string> GetDonateTextAsync(
        string key,
        long telegramId,
        IReadOnlyDictionary<string, string> placeholders,
        CancellationToken cancellationToken)
    {
        var template = await GetDonateTextAsync(key, telegramId, cancellationToken);
        return LocalizationPlaceholderFormatter.Apply(template, placeholders);
    }

    private static string TruncateDonateButton(string label)
    {
        const int telegramButtonLimit = 64;
        if (string.IsNullOrWhiteSpace(label))
            return "…";
        return label.Length <= telegramButtonLimit ? label : label[..(telegramButtonLimit - 1)] + "…";
    }
}
