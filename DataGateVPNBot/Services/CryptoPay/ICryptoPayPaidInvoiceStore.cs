namespace DataGateVPNBot.Services.CryptoPay;

public interface ICryptoPayPaidInvoiceStore
{
    /// <summary>Returns true if this invoice id was not seen before.</summary>
    bool TryMarkProcessed(long invoiceId);
}
