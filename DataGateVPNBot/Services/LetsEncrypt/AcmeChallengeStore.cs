namespace DataGateVPNBot.Services.LetsEncrypt;

public static class AcmeChallengeStore
{
    private static readonly Dictionary<string, string> _store = new();

    public static void Add(string token, string keyAuth)
    {
        _store[token] = keyAuth;
    }

    public static bool TryGet(string token, out string? keyAuth)
    {
        return _store.TryGetValue(token, out keyAuth);
    }
}
