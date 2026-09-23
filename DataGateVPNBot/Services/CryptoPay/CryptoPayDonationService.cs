using DataGateVPNBot.Configurations;
using DataGateVPNBot.Models.Configurations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataGateVPNBot.Services.CryptoPay;

public sealed class CryptoPayDonationService(
    ICryptoPayApiClient apiClient,
    ICryptoPayPaidInvoiceStore paidInvoiceStore,
    IOptions<CryptoPayConfiguration> options,
    ILogger<CryptoPayDonationService> logger) : ICryptoPayDonationService
{
    public const string PayloadPrefix = "tg:";
    public const string CallbackMenu = "donate";
    public const string CallbackAmountPrefix = "donate:cb:";
    public const string CallbackConfirmPrefix = "donate:ok:";

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    public bool IsConfigured => options.Value.IsConfigured;

    public IReadOnlyList<decimal> AmountsUsd =>
        CryptoPayConfigurationHelper.ParseAmounts(options.Value.Amounts);

    public async Task<CryptoPayInvoice> CreateDonationInvoiceAsync(
        long telegramId,
        decimal amountUsd,
        string? botUsername,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Crypto Pay is not configured.");
        if (telegramId <= 0)
            throw new ArgumentOutOfRangeException(nameof(telegramId));
        if (amountUsd <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountUsd));

        var config = options.Value;
        var request = new CryptoPayCreateInvoiceRequest
        {
            CurrencyType = "fiat",
            Fiat = string.IsNullOrWhiteSpace(config.Fiat) ? "USD" : config.Fiat,
            Amount = CryptoPayWebhookSignature.FormatUsd(amountUsd),
            AcceptedAssets = string.IsNullOrWhiteSpace(config.AcceptedAssets) ? null : config.AcceptedAssets,
            Description = $"DataGate donation (${CryptoPayWebhookSignature.FormatUsd(amountUsd)})",
            HiddenMessage = "Thank you for supporting DataGate!",
            Payload = $"{PayloadPrefix}{telegramId}",
            AllowComments = true,
            AllowAnonymous = false,
            ExpiresIn = config.InvoiceExpiresInSeconds > 0 ? config.InvoiceExpiresInSeconds : 3600,
            SwapTo = string.IsNullOrWhiteSpace(config.SwapTo) ? null : config.SwapTo.Trim()
        };

        if (!string.IsNullOrWhiteSpace(botUsername))
        {
            request.PaidBtnName = "callback";
            request.PaidBtnUrl = $"https://t.me/{botUsername.Trim().TrimStart('@')}";
        }

        logger.LogInformation(
            "Creating Crypto Pay donation invoice. TelegramId={TelegramId}; Amount={Amount} {Fiat}",
            telegramId, request.Amount, request.Fiat);

        return await apiClient.CreateInvoiceAsync(request, cancellationToken);
    }

    public Task<CryptoPayPaidDonation?> ProcessWebhookAsync(
        string rawBody,
        string? signature,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsConfigured)
        {
            logger.LogWarning("Crypto Pay webhook received while donations are not configured.");
            throw new UnauthorizedAccessException("Crypto Pay is not configured.");
        }

        if (!CryptoPayWebhookSignature.IsValid(options.Value.ApiToken, rawBody, signature))
            throw new UnauthorizedAccessException("Invalid Crypto Pay webhook signature.");

        var update = JsonConvert.DeserializeObject<CryptoPayWebhookUpdate>(rawBody, JsonSettings);
        if (update?.Payload == null ||
            !string.Equals(update.UpdateType, "invoice_paid", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Ignoring Crypto Pay webhook. Type={Type}", update?.UpdateType);
            return Task.FromResult<CryptoPayPaidDonation?>(null);
        }

        var invoice = update.Payload;
        var isNew = paidInvoiceStore.TryMarkProcessed(invoice.InvoiceId);
        var telegramId = TryParseTelegramId(invoice.Payload);
        var asset = invoice.PaidAsset ?? invoice.Asset ?? invoice.Fiat ?? "USD";
        var amount = invoice.PaidAmount ?? invoice.Amount ?? "?";

        logger.LogInformation(
            "Crypto Pay invoice paid. InvoiceId={InvoiceId}; TelegramId={TelegramId}; Amount={Amount} {Asset}; Duplicate={Duplicate}",
            invoice.InvoiceId, telegramId, amount, asset, !isNew);

        return Task.FromResult<CryptoPayPaidDonation?>(new CryptoPayPaidDonation
        {
            InvoiceId = invoice.InvoiceId,
            TelegramId = telegramId,
            Amount = amount,
            Asset = asset,
            Comment = invoice.Comment,
            IsDuplicate = !isNew
        });
    }

    public static bool TryParseAmountCallback(string? data, out decimal amountUsd) =>
        TryParseUsdCallback(data, CallbackAmountPrefix, out amountUsd);

    public static bool TryParseConfirmCallback(string? data, out decimal amountUsd) =>
        TryParseUsdCallback(data, CallbackConfirmPrefix, out amountUsd);

    public static string ConfirmCallback(decimal amountUsd) =>
        $"{CallbackConfirmPrefix}{CryptoPayWebhookSignature.FormatUsd(amountUsd)}";

    private static bool TryParseUsdCallback(string? data, string prefix, out decimal amountUsd)
    {
        amountUsd = 0;
        if (string.IsNullOrWhiteSpace(data) ||
            !data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return decimal.TryParse(data[prefix.Length..], System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out amountUsd) && amountUsd > 0;
    }

    public static long? TryParseTelegramId(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload) ||
            !payload.StartsWith(PayloadPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        return long.TryParse(payload[PayloadPrefix.Length..], out var telegramId) && telegramId > 0
            ? telegramId
            : null;
    }
}
