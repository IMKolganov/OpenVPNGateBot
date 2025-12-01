using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Directory = System.IO.Directory;

namespace DataGateVPNBot.Services.LetsEncrypt
{
    public class LetsEncryptCertificateGenerator
    {
        private readonly ILogger<LetsEncryptCertificateGenerator> _logger;
        private readonly string _pemPath;
        private readonly string _keyPath;
        private readonly string _accountPath;
        private readonly string _challengeDir;

        public LetsEncryptCertificateGenerator(
            ILogger<LetsEncryptCertificateGenerator> logger,
            IConfiguration configuration)
        {
            _logger = logger;

            // Resolve paths from environment/configuration with sensible defaults
            _pemPath = configuration["CERTIFICATE_PEM_PATH"] 
                       ?? "/app/resources/certs/datagatetgbot.pem";
            _keyPath = configuration["CERTIFICATE_KEY_PATH"] 
                       ?? "/app/resources/certs/datagatetgbot.key";

            // Account key path: default next to the PEM file
            _accountPath = configuration["CERTIFICATE_ACCOUNT_PATH"]
                           ?? Path.Combine(Path.GetDirectoryName(_pemPath) ?? "/app/resources/certs", "account.pem");

            // Challenge directory for http-01 files (also stored in memory store)
            _challengeDir = configuration["CHALLENGE_DIR"]
                            ?? "/app/resources/wwwroot/.well-known/acme-challenge";
        }

        public async Task EnsureCertificateAsync(string domain, string email, CancellationToken cancellationToken)
        {
            var pemExists = File.Exists(_pemPath);
            var keyExists = File.Exists(_keyPath);

            _logger.LogInformation(
                "Checking existing certificate and key. PemPath={PemPath}, PemExists={PemExists}, KeyPath={KeyPath}, KeyExists={KeyExists}",
                _pemPath,
                pemExists,
                _keyPath,
                keyExists);

            if (pemExists && keyExists)
            {
                try
                {
                    var certPem = await File.ReadAllTextAsync(_pemPath, cancellationToken);
                    var keyPem = await File.ReadAllTextAsync(_keyPath, cancellationToken);

                    _logger.LogInformation(
                        "Loaded existing PEM files. CertLength={CertLength}, KeyLength={KeyLength}",
                        certPem.Length,
                        keyPem.Length);

                    using var certWithKey = X509Certificate2.CreateFromPem(certPem, keyPem);

                    var notAfterUtc = certWithKey.NotAfter.ToUniversalTime();
                    var nowUtc = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Current cert (with key) valid until (UTC): {Expiry}",
                        notAfterUtc);

                    if (notAfterUtc > nowUtc.AddDays(14))
                    {
                        _logger.LogInformation(
                            "Certificate with matching key is still valid. Skipping LetsEncrypt generation.");
                        return;
                    }

                    _logger.LogWarning(
                        "Certificate with matching key is expiring soon or expired. Will request new certificate.");
                }
                catch (CryptographicException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Existing PEM+KEY pair is invalid. Will generate new certificate.");
                }
            }
            else if (pemExists || keyExists)
            {
                _logger.LogWarning(
                    "Only one of PEM/KEY exists (PemExists={PemExists}, KeyExists={KeyExists}). Will generate new certificate.",
                    pemExists,
                    keyExists);
            }
            
            // Ensure directories for cert files and challenge files
            EnsureDirectoryForFile(_pemPath);
            EnsureDirectoryForFile(_keyPath);
            EnsureDirectory(_challengeDir);

            _logger.LogInformation(
                "Starting LetsEncrypt certificate generation for domain={Domain}...",
                domain);

            // Normalize domain (strip protocol and port)
            var normalizedDomain = NormalizeDomain(domain);

            // Load or create ACME account key
            IKey accountKey;
            if (File.Exists(_accountPath))
            {
                _logger.LogInformation(
                    "Using existing ACME account key. AccountPath={AccountPath}",
                    _accountPath);

                var pem = await File.ReadAllTextAsync(_accountPath, cancellationToken);
                accountKey = KeyFactory.FromPem(pem);
            }
            else
            {
                _logger.LogInformation(
                    "Creating new ACME account key. AccountPath={AccountPath}",
                    _accountPath);

                accountKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                await File.WriteAllTextAsync(_accountPath, accountKey.ToPem(), cancellationToken);
            }

            var acme = new AcmeContext(WellKnownServers.LetsEncryptV2, accountKey);
            await acme.NewAccount(email, true);

            var order = await acme.NewOrder(new[] { normalizedDomain });
            var authz = (await order.Authorizations()).First();
            var httpChallenge = await authz.Http();

            var token = httpChallenge.Token;
            var keyAuth = httpChallenge.KeyAuthz;

            // Expose challenge: in-memory + filesystem (optional http-serve from disk)
            AcmeChallengeStore.Add(token, keyAuth);

            var challengePath = Path.Combine(_challengeDir, token);
            await File.WriteAllTextAsync(challengePath, keyAuth, cancellationToken);

            _logger.LogInformation(
                "📥 Challenge token written. Path={Path}",
                challengePath);

            await httpChallenge.Validate();

            _logger.LogInformation("Waiting for challenge validation...");
            var retries = 30;
            while (retries-- > 0)
            {
                var updatedAuthz = await authz.Resource();

                if (updatedAuthz.Status == AuthorizationStatus.Valid)
                {
                    _logger.LogInformation("Challenge validated successfully.");
                    break;
                }

                if (updatedAuthz.Status == AuthorizationStatus.Invalid)
                {
                    var httpChallengeError = updatedAuthz.Challenges
                        ?.FirstOrDefault(c => c.Type == "http-01")
                        ?.Error;

                    var detail = httpChallengeError?.Detail ?? "Unknown challenge validation error";
                    var errorType = httpChallengeError?.Type ?? "Unknown type";

                    _logger.LogWarning(
                        "Challenge invalid. Type={Type}, Detail={Detail}. Will retry...",
                        errorType,
                        detail);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }

            var domainKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var csr = new CsrInfo
            {
                CommonName = normalizedDomain,//todo: move to settings
                CountryName = "CY",
                State = "Nicosia",
                Locality = "Nicosia",
                Organization = "DataGateVPNBot",
                OrganizationUnit = "Bot Department"
            };

            var certChain = await order.Generate(csr, domainKey);

            await File.WriteAllTextAsync(_pemPath, certChain.ToPem(), cancellationToken);
            await File.WriteAllTextAsync(_keyPath, domainKey.ToPem(), cancellationToken);

            _logger.LogInformation(
                "LetsEncrypt certificate created. PemPath={PemPath}, KeyPath={KeyPath}",
                _pemPath,
                _keyPath);
        }

        private static string NormalizeDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Domain is required.", nameof(domain));

            if (domain.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(domain);
                return uri.Host;
            }

            // Strip optional ":port"
            var colon = domain.LastIndexOf(':');
            if (colon > -1)
            {
                var hostPart = domain[..colon];
                if (!string.IsNullOrWhiteSpace(hostPart)) return hostPart;
            }

            return domain;
        }

        private static void EnsureDirectoryForFile(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir))
                EnsureDirectory(dir);
        }

        private static void EnsureDirectory(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
