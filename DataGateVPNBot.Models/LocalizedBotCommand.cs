using Telegram.Bot.Types;

namespace DataGateVPNBot.Models;

public class LocalizedBotCommand
{
    public string Command { get; init; } = null!;

    public Dictionary<string, string> Descriptions { get; init; } = new();

    public BotCommand ToTelegramCommand(string langCode)
    {
        var description = Descriptions.TryGetValue(langCode, out var value)
            ? value
            : Descriptions.GetValueOrDefault("en", Command);

        return new BotCommand
        {
            Command = Command,
            Description = description
        };
    }
}