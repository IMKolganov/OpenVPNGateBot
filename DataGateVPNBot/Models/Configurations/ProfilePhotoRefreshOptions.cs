namespace DataGateVPNBot.Models.Configurations;

public sealed class ProfilePhotoRefreshOptions
{
    public const string SectionName = "ProfilePhotoRefresh";

    /// <summary>When false, the periodic background refresh is not registered.</summary>
    public bool PeriodicRefreshEnabled { get; set; } = true;

    /// <summary>Interval between automatic refresh runs.</summary>
    public int IntervalDays { get; set; } = 30;

    /// <summary>Optional delay between Telegram API calls per user (rate limiting).</summary>
    public int DelayMillisecondsBetweenUsers { get; set; } = 75;
}
