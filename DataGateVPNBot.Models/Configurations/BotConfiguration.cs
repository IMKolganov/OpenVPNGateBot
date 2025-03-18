namespace DataGateVPNBot.Models.Configurations;

public class BotConfiguration
{
    public string BotToken { get; init; } = null!;
    public string TelegramWebHook { get; init; } = "TelegramWebHook";
    public string CertificatePath { get; init; } = null!;
    public string BotPhotoPath { get; init; } = "Photo/bot.gif";
}