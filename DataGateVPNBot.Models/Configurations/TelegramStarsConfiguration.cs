namespace DataGateVPNBot.Models.Configurations;

public class TelegramStarsConfiguration
{
    public const string SectionName = "TelegramStars";

    /// <summary>Kill switch (default off). Set <c>STARS_ENABLED=true</c> to show Stars on /donate.</summary>
    public bool Enabled { get; set; }

    /// <summary>Comma-separated Star amounts shown on /donate.</summary>
    public string Amounts { get; set; } = "50,100,250,500";

    public bool IsConfigured => Enabled;
}
