using DataGateVPNBot.Extensions;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.Interfaces;
using DataGateVPNBot.Services.LetsEncrypt;
using DataGateVPNBot.Services.TelegramApi;
using Microsoft.Extensions.Options;

namespace DataGateVPNBot.Handlers;

public class StartupBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<StartupBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StartupBackgroundService started. Waiting for server health endpoint.");

        try
        {
            await WaitForServerAsync(stoppingToken);
            logger.LogInformation("Healthcheck succeeded. Proceeding with startup initialization.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Healthcheck failed before initialization.");
            throw;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();

            var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
            var webhookService = scope.ServiceProvider.GetRequiredService<WebhookService>();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<BotConfiguration>>();
            var opensslCertificateGenerator = scope.ServiceProvider.GetRequiredService<OpensslCertificateGenerator>();
            var letsEncryptCertificateGenerator = 
                scope.ServiceProvider.GetRequiredService<LetsEncryptCertificateGenerator>();

            var botConfig = options.Value;

            logger.LogInformation(
                "Startup iteration. UseCertificate={UseCertificate}, " +
                "AutoGenerateCertificate={AutoGenerateCertificate}, " +
                "HostAddress={HostAddress}, CertificatePemPath={CertificatePemPath}",
                botConfig.UseCertificate,
                botConfig.AutoGenerateCertificate,
                botConfig.HostAddress,
                botConfig.CertificatePemPath);

            try
            {
                logger.LogInformation("Notifying admins about startup.");
                await errorService.NotifyAdminsAboutStartAsync(stoppingToken);
                logger.LogInformation("Admins notified successfully.");

                if (botConfig.UseCertificate || botConfig.AutoGenerateCertificate)
                {
                    logger.LogInformation("Certificate processing enabled.");

                    if (botConfig.AutoGenerateCertificate)
                    {
                        var host = botConfig.HostAddress?.Trim();

                        if (string.IsNullOrWhiteSpace(host))
                            throw new InvalidOperationException("HostAddress is empty while " +
                                                                "AutoGenerateCertificate is enabled.");

                        var isDomain = host.IsDomainName();

                        logger.LogInformation(
                            "Host classification completed. Host={Host}, IsDomainName={IsDomainName}",
                            host,
                            isDomain);

                        if (isDomain)
                        {
                            logger.LogInformation("Running LetsEncrypt certificate generator.");

                            await letsEncryptCertificateGenerator.EnsureCertificateAsync(
                                host,
                                botConfig.Email,
                                stoppingToken);

                            logger.LogInformation("LetsEncrypt generation finished.");
                        }
                        else
                        {
                            logger.LogInformation("Running OpenSSL self-signed certificate generator.");

                            await opensslCertificateGenerator.EnsureCertificateAsync(host, stoppingToken);

                            logger.LogInformation("OpenSSL generation finished.");
                        }
                    }

                    if (string.IsNullOrEmpty(botConfig.CertificatePemPath))
                        throw new NullReferenceException("CertificatePemPath missing.");

                    logger.LogInformation(
                        "Checking certificate file existence. Path={CertificatePemPath}",
                        botConfig.CertificatePemPath);

                    if (!File.Exists(botConfig.CertificatePemPath))
                        throw new FileNotFoundException(
                            $"Certificate file '{botConfig.CertificatePemPath}' not found.");

                    logger.LogInformation("Certificate file exists.");
                }
                else
                {
                    logger.LogInformation("Certificate processing is disabled.");
                }

                logger.LogInformation("Checking webhook state.");

                var isWebhookSet = await webhookService.IsWebhookSetAsync(stoppingToken);

                logger.LogInformation(
                    "Webhook status checked. IsWebhookSet={IsWebhookSet}",
                    isWebhookSet);

                if (!isWebhookSet)
                {
                    try
                    {
                        logger.LogWarning("Webhook is not set. Executing reset sequence.");

                        await webhookService.DeleteWebhookAsync(stoppingToken);
                        logger.LogInformation("Webhook deleted.");

                        await webhookService.SetWebhookAsync(stoppingToken);
                        logger.LogInformation("Webhook set successfully.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Webhook setup failed.");
                        await errorService.NotifyAdminsAboutExceptionAsync(ex, null, stoppingToken);
                    }
                }

                logger.LogInformation("Startup initialization finished successfully.");
                break;
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("StartupBackgroundService cancellation requested.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Startup failed. Retrying in 10 seconds.");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    logger.LogWarning("Retry loop cancelled by token.");
                    break;
                }
            }
        }

        logger.LogInformation("StartupBackgroundService execution finished.");
    }

    private async Task WaitForServerAsync(CancellationToken token)
    {
        var healthPort = GetHealthCheckPort();
        var healthUrl = healthPort == 80
            ? "http://localhost/.well-known/healthcheck"
            : $"http://localhost:{healthPort}/.well-known/healthcheck";

        logger.LogInformation("Waiting for health endpoint. Url={HealthUrl}", healthUrl);

        using var http = new HttpClient();

        for (var attempt = 1; attempt <= 30; attempt++)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                logger.LogDebug(
                    "Healthcheck attempt started. Attempt={Attempt}/30",
                    attempt);

                var response = await http.GetAsync(healthUrl, token);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation(
                        "Healthcheck succeeded. Attempt={Attempt}, StatusCode={StatusCode}",
                        attempt,
                        response.StatusCode);

                    return;
                }

                logger.LogWarning(
                    "Healthcheck failed. Attempt={Attempt}, StatusCode={StatusCode}",
                    attempt,
                    response.StatusCode);
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Healthcheck attempt failed with exception. Attempt={Attempt}",
                    attempt);
            }

            await Task.Delay(1000, token);
        }

        logger.LogError("Healthcheck timeout exceeded.");
        throw new Exception("Server didn't start within timeout.");
    }

    private static int GetHealthCheckPort()
    {
        if (bool.TryParse(Environment.GetEnvironmentVariable("USE_CERTIFICATE"), out var useCert) && !useCert)
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("TELEGRAMBOT_LISTEN_PORT"), out var port) && port > 0)
                return port;
            return 5050;
        }
        return 80;
    }
}
