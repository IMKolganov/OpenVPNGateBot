namespace DataGateVPNBot.Services.CryptoPay;

public interface ICryptoPayApiClient
{
    Task<CryptoPayInvoice> CreateInvoiceAsync(
        CryptoPayCreateInvoiceRequest request,
        CancellationToken cancellationToken);
}
