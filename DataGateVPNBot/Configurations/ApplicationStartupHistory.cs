using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataGateVPNBot.Configurations;

public sealed class ApplicationStartupRecord
{
    public DateTimeOffset StartedAtUtc { get; init; }

    public string Version { get; init; } = "";

    public string Environment { get; init; } = "";
}

public interface IApplicationStartupHistory
{
    IReadOnlyList<ApplicationStartupRecord> GetRecords();
}

/// <summary>
/// Persists recent process startups. Path resolution:
/// <list>
/// <item><c>STARTUP_HISTORY_PATH</c> env when set</item>
/// <item><c>{ContentRootPath}/resources/startup-history.json</c> in containers (bind to <c>/app/resources</c> volume)</item>
/// <item><c>{ContentRootPath}/data/startup-history.json</c> for local dev</item>
/// </list>
/// </summary>
public sealed class ApplicationStartupHistory : IApplicationStartupHistory
{
    private const int MaxRecords = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly List<ApplicationStartupRecord> _records;
    private readonly object _sync = new();

    public ApplicationStartupHistory(
        IWebHostEnvironment environment,
        ApplicationRuntimeInfo runtimeInfo,
        string version,
        string environmentName)
    {
        var path = GetHistoryFilePath(environment);
        _records = Load(path);

        _records.Insert(0, new ApplicationStartupRecord
        {
            StartedAtUtc = runtimeInfo.StartedAtUtc,
            Version = version,
            Environment = environmentName,
        });

        if (_records.Count > MaxRecords)
            _records.RemoveRange(MaxRecords, _records.Count - MaxRecords);

        Save(path, _records);
    }

    public IReadOnlyList<ApplicationStartupRecord> GetRecords()
    {
        lock (_sync)
            return _records.ToList();
    }

    public static string GetHistoryFilePath(IWebHostEnvironment environment)
    {
        var configured = Environment.GetEnvironmentVariable("STARTUP_HISTORY_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            return Path.Combine(environment.ContentRootPath, "resources", "startup-history.json");

        return Path.Combine(environment.ContentRootPath, "data", "startup-history.json");
    }

    private static List<ApplicationStartupRecord> Load(string path)
    {
        if (!File.Exists(path))
            return [];

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<ApplicationStartupRecord>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void Save(string path, List<ApplicationStartupRecord> records)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonSerializer.Serialize(records, JsonOptions));
        }
        catch
        {
            // Root page history is best-effort; startup must not fail if the file is unavailable.
        }
    }
}
