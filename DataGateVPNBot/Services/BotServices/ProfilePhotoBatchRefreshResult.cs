namespace DataGateVPNBot.Services.BotServices;

public sealed class ProfilePhotoBatchRefreshResult
{
    public int TotalUsers { get; init; }
    public int Updated { get; init; }
    public int Unchanged { get; init; }
    public int SkippedNoProfilePhoto { get; init; }

    /// <summary>
    /// Telegram refused access (e.g. user blocked the bot, deleted account, invalid id) — expected, not a server bug.
    /// </summary>
    public int SkippedUserUnavailable { get; init; }

    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public bool AnyDatabaseChange => Updated > 0;
}
