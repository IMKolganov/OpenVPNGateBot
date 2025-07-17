using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Directory = System.IO.Directory;

namespace DataGateVPNBot.Services.LetsEncrypt;

public class LetsEncryptCertificateGenerator(ILogger<LetsEncryptCertificateGenerator> logger)
{
    private const string PemPath = "certificates/datagatetgbot.pem";
    private const string KeyPath = "certificates/domain.key";
    private const string AccountPath = "certificates/account.pem";

    public async Task EnsureCertificateAsync(string domain, string email, CancellationToken cancellationToken)
    {
        if (File.Exists(PemPath) && File.Exists(KeyPath))
        {
            logger.LogInformation("✅ Certificate already exists, skipping generation.");
            return;
        }

        Directory.CreateDirectory("certificates");

        logger.LogInformation("🚀 Starting Let's Encrypt certificate generation for {Domain}...", domain);

        // Normalize domain (strip protocol and port)
        var normalizedDomain = domain.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? new Uri(domain).Host
            : domain;

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

        var order = await acme.NewOrder([normalizedDomain]);
        var authz = (await order.Authorizations()).First();
        var httpChallenge = await authz.Http();

        var token = httpChallenge.Token;
        var keyAuth = httpChallenge.KeyAuthz;
        
        AcmeChallengeStore.Add(token, keyAuth);

        // Write token for challenge
        var challengeDir = Path.Combine("wwwroot", ".well-known", "acme-challenge");
        Directory.CreateDirectory(challengeDir);
        var challengePath = Path.Combine(challengeDir, token);
        await File.WriteAllTextAsync(challengePath, keyAuth, cancellationToken);

        logger.LogInformation("📥 Challenge token written to: {Path}", challengePath);

        // Trigger validation
        await httpChallenge.Validate();

        // Wait for status = valid
        logger.LogInformation("⏳ Waiting for challenge validation...");
        var retries = 10;
        while (retries-- > 0)
        {
            var updatedAuthz = await authz.Resource();

            if (updatedAuthz.Status == AuthorizationStatus.Valid)
            {
                logger.LogInformation("✅ Challenge validated successfully.");
                break;
            }
            
            if (updatedAuthz.Status == AuthorizationStatus.Invalid)
            {
                var httpChallengeError = updatedAuthz.Challenges
                    ?.FirstOrDefault(c => c.Type == "http-01")
                    ?.Error;

                var detail = httpChallengeError?.Detail ?? "Unknown challenge validation error";
                var errorType = httpChallengeError?.Type ?? "Unknown type";

                throw new InvalidOperationException($"❌ Challenge validation failed:\nType: {errorType}\nDetail: {detail}");
            }

            await Task.Delay(2000, cancellationToken);
        }

        // Generate certificate
        var domainKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
        var csr = new CsrInfo
        {
            CommonName = normalizedDomain,
            CountryName = "CY",
            State = "Nicosia",
            Locality = "Nicosia",
            Organization = "DataGateVPNBot",
            OrganizationUnit = "Bot Department"
        };

        var certChain = await order.Generate(csr, domainKey);

        await File.WriteAllTextAsync(PemPath, certChain.ToPem(), cancellationToken);
        await File.WriteAllTextAsync(KeyPath, domainKey.ToPem(), cancellationToken);

        logger.LogInformation("🎉 Let's Encrypt certificate successfully created and saved to:");
        logger.LogInformation("  - {PemPath}", PemPath);
        logger.LogInformation("  - {KeyPath}", KeyPath);
    }
}
