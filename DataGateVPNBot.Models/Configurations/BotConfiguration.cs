namespace DataGateVPNBot.Models.Configurations;

public class BotConfiguration
{
    public string BotToken { get; set; } = null!;
    public string HostAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 443;
    public bool UseCertificate { get; set; } = false;
    public bool AutoGenerateCertificate { get; set; } = false;
    public string CertificatePath { get; set; } = string.Empty;
}