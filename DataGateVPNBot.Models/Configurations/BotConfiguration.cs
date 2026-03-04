namespace DataGateVPNBot.Models.Configurations;

public class BotConfiguration
{
    public string BotToken { get; set; } = string.Empty;
    public string HostAddress { get; set; } = "127.0.0.1";
    public string Email { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 0;
    public bool UseCertificate { get; set; } = false;
    public bool AutoGenerateCertificate { get; set; } = false;
    public string? CertificatePfxPath { get; set; } = "app/resources/certs/datagatetgbot.pfx";
    public string? CertificatePemPath { get; set; } = "app/resources/certs/datagatetgbot.pem";
    public TimeSpan? InitDataLifetime { get; set; }
}