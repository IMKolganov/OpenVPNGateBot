using System.Text.Json;

namespace DataGateVPNBot.Services;

/// <summary>
/// Persists dashboard admin Telegram IDs. Env fallback is reserve-only when cache is empty.
/// </summary>
public sealed class AdminRecipientStore
{
    private readonly object _lock = new();
    private readonly ILogger<AdminRecipientStore> _logger;
    private List<long> _cachedIds = [];
    private readonly string _cacheFilePath;
    private readonly long[] _fallbackIds;

    public AdminRecipientStore(ILogger<AdminRecipientStore> logger)
    {
        _logger = logger;
        _cacheFilePath = Environment.GetEnvironmentVariable("ADMIN_CACHE_PATH") ?? "/app/resources/admin-cache.json";
        _fallbackIds = ParseFallbackIds(Environment.GetEnvironmentVariable("TELEGRAMBOT_FALLBACK_ADMIN_IDS"));
        LoadFromDisk();
    }

    public void Update(IEnumerable<long> telegramIds)
    {
        var ids = telegramIds
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (ids.Count == 0)
            return;

        lock (_lock)
        {
            _cachedIds = ids;
            SaveToDiskLocked();
        }

        _logger.LogInformation("Admin recipient cache updated ({Count} id(s)).", ids.Count);
    }

    public IReadOnlyList<long> GetCachedIds()
    {
        lock (_lock)
        {
            return _cachedIds.ToList();
        }
    }

    public IReadOnlyList<long> GetFallbackIds() => _fallbackIds.ToList();

    /// <summary>
    /// Dashboard cache first; <see cref="TELEGRAMBOT_FALLBACK_ADMIN_IDS"/> only when cache is empty.
    /// </summary>
    public IReadOnlyList<long> ResolveRecipients()
    {
        lock (_lock)
        {
            if (_cachedIds.Count > 0)
                return _cachedIds.ToList();

            return _fallbackIds.ToList();
        }
    }

    public bool HasAnyRecipients => ResolveRecipients().Count > 0;

    public bool IsUsingFallbackReserve
    {
        get
        {
            lock (_lock)
            {
                return _cachedIds.Count == 0 && _fallbackIds.Length > 0;
            }
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
                return;

            var json = File.ReadAllText(_cacheFilePath);
            var payload = JsonSerializer.Deserialize<AdminCacheFile>(json);
            if (payload?.TelegramIds is not { Count: > 0 })
                return;

            lock (_lock)
            {
                _cachedIds = payload.TelegramIds
                    .Where(id => id > 0)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();
            }

            _logger.LogInformation(
                "Loaded {Count} admin recipient id(s) from {Path}.",
                _cachedIds.Count,
                _cacheFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load admin recipient cache from {Path}.", _cacheFilePath);
        }
    }

    private void SaveToDiskLocked()
    {
        try
        {
            var directory = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var payload = new AdminCacheFile
            {
                UpdatedAtUtc = DateTime.UtcNow,
                TelegramIds = _cachedIds,
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist admin recipient cache to {Path}.", _cacheFilePath);
        }
    }

    private static long[] ParseFallbackIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => long.TryParse(part, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
    }

    private sealed class AdminCacheFile
    {
        public DateTime UpdatedAtUtc { get; set; }
        public List<long> TelegramIds { get; set; } = [];
    }
}
