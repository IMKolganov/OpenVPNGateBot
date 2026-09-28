using System.Collections.Concurrent;

namespace DataGateVPNBot.Services.CryptoPay;

public sealed class CryptoPayPaidInvoiceStore : ICryptoPayPaidInvoiceStore
{
    private readonly ConcurrentDictionary<long, byte> _processed = new();

    public bool TryMarkProcessed(long invoiceId) => _processed.TryAdd(invoiceId, 0);
}
