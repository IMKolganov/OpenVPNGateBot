namespace DataGateVPNBot.Models.Configurations;

public class BotConfiguration
{
    public string BotToken { get; set; } = null!;
    public string BotWebhookUrl { get; set; } = "TelegramWebHook";
    public bool UseCertificate { get; set; } = false;
    public string CertificatePath { get; set; } = null!;
}