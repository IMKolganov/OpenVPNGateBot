using Certes;
using Certes.Acme;

namespace DataGateVPNBot.Services.LetsEncrypt;

public class CertificateGenerator(ILogger<CertificateGenerator> logger)
{
    private const string PemPath = "certs/datagatetgbot.pem";
    private const string KeyPath = "certs/domain.key";
    private const string AccountPath = "certs/account.pem";

    public async Task EnsureCertificateAsync(string domain, string email, CancellationToken cancellationToken)
    {
        if (File.Exists(PemPath) && File.Exists(KeyPath))
        {
            logger.LogInformation("Certificate already exists, skipping generation.");
            return;
        }

        Directory.CreateDirectory("certs");

        logger.LogInformation("Starting Let's Encrypt certificate generation for {Domain}...", domain);

        // Load or create ACME account key
        IKey accountKey;
        if (File.Exists(AccountPath))
        {
            accountKey = KeyFactory.FromPem(await File.ReadAllTextAsync(AccountPath, cancellationToken));
        }
        else
        {
            accountKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            await File.WriteAllTextAsync(AccountPath, accountKey.ToPem(), cancellationToken);
        }

        var acme = new AcmeContext(WellKnownServers.LetsEncryptV2, accountKey);
        await acme.NewAccount(email, true);

        var order = await acme.NewOrder([domain]);
        var authz = (await order.Authorizations()).First();
        var httpChallenge = await authz.Http();

        var token = httpChallenge.Token;
        var keyAuth = httpChallenge.KeyAuthz;

        var challengeDir = Path.Combine("wwwroot", ".well-known", "acme-challenge");
        Directory.CreateDirectory(challengeDir);
        await File.WriteAllTextAsync(Path.Combine(challengeDir, token), keyAuth, cancellationToken);

        logger.LogInformation("Challenge token written. Waiting for validation...");

        await httpChallenge.Validate();
        await Task.Delay(5000, cancellationToken); // wait for Let's Encrypt

        var domainKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
        var csr = new CsrInfo
        {
            CommonName = domain,
            CountryName = "CY",// Two-letter ISO country code
            State = "Nicosia",
            Locality = "Nicosia",
            Organization = "DataGateVPNBot",
            OrganizationUnit = "Bot Department"
        };
        
        var certChain = await order.Generate(csr, domainKey);

        await File.WriteAllTextAsync(PemPath, certChain.ToPem(), cancellationToken);
        await File.WriteAllTextAsync(KeyPath, domainKey.ToPem(), cancellationToken);

        logger.LogInformation("Let's Encrypt certificate successfully created and saved.");
    }
}
