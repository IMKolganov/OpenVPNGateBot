namespace DataGateVPNBot.Services.CryptoPay;

public interface ICryptoPayDonationService
{
    bool IsConfigured { get; }

    IReadOnlyList<decimal> AmountsUsd { get; }

    Task<CryptoPayInvoice> CreateDonationInvoiceAsync(
        long telegramId,
        decimal amountUsd,
        string? botUsername,
        CancellationToken cancellationToken);

    Task<CryptoPayPaidDonation?> ProcessWebhookAsync(
        string rawBody,
        string? signature,
        CancellationToken cancellationToken);
}
