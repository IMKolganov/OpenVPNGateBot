namespace DataGateVPNBot.Models.Configurations;

public class TelegramStarsConfiguration
{
    public const string SectionName = "TelegramStars";

    public bool Enabled { get; set; } = true;

    /// <summary>Comma-separated Star amounts shown on /donate.</summary>
    public string Amounts { get; set; } = "50,100,250,500";

    public bool IsConfigured => Enabled;
}
