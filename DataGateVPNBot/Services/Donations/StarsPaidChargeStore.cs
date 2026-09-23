using System.Collections.Concurrent;

namespace DataGateVPNBot.Services.Donations;

public sealed class StarsPaidChargeStore
{
    private readonly ConcurrentDictionary<string, byte> _processed = new(StringComparer.Ordinal);

    public bool TryMarkProcessed(string? chargeId) =>
        !string.IsNullOrWhiteSpace(chargeId) && _processed.TryAdd(chargeId, 0);
}
