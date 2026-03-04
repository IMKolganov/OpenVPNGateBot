using Certes;

namespace DataGateVPNBot.Services.LetsEncrypt;

public static class LetsEncryptAccountStore
{
    private const string AccountKeyFile = "certs/account.pem";

    public static IKey LoadOrCreateAccountKey()
    {
        if (File.Exists(AccountKeyFile))
        {
            var pem = File.ReadAllText(AccountKeyFile);
            return KeyFactory.FromPem(pem);
        }

        var key = KeyFactory.NewKey(KeyAlgorithm.ES256);
        File.WriteAllText(AccountKeyFile, key.ToPem());
        return key;
    }
}
