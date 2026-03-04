using Newtonsoft.Json;

namespace DataGateVPNBot.Helpers;

public sealed record TelegramUser(long Id, string? Username, string? FirstName, string? LastName);

public sealed class TelegramInitData
{
    public TelegramUser? User { get; init; }
    public long AuthDateUnix { get; init; }
    public string? ChatType { get; init; }
    public string? ChatInstance { get; init; }

    public static TelegramInitData FromDictionary(Dictionary<string, string> kv)
    {
        TelegramUser? user = null;
        if (kv.TryGetValue("user", out var userJson) && !string.IsNullOrWhiteSpace(userJson))
        {
            try
            {
                var u = JsonConvert.DeserializeObject<UserJson>(userJson);
                if (u?.Id != null)
                    user = new TelegramUser(u.Id.Value, u.Username, u.FirstName, u.LastName);
            }
            catch { /* ignore parse errors */ }
        }

        long auth = 0;
        if (kv.TryGetValue("auth_date", out var ad) && long.TryParse(ad, out var val))
            auth = val;

        kv.TryGetValue("chat_type", out var chatType);
        kv.TryGetValue("chat_instance", out var chatInstance);

        return new TelegramInitData
        {
            User = user,
            AuthDateUnix = auth,
            ChatType = chatType,
            ChatInstance = chatInstance
        };
    }

    private sealed class UserJson
    {
        [JsonProperty("id")]
        public long? Id { get; set; }

        [JsonProperty("username")]
        public string? Username { get; set; }

        [JsonProperty("first_name")]
        public string? FirstName { get; set; }

        [JsonProperty("last_name")]
        public string? LastName { get; set; }
    }
}