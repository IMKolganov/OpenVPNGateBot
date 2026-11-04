namespace DataGateVPNBot.Configurations;

/// <summary>
/// Process-level runtime facts for the public root page (uptime since this instance started).
/// </summary>
public sealed class ApplicationRuntimeInfo
{
    public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;

    public TimeSpan Uptime => DateTimeOffset.UtcNow - StartedAtUtc;
}
