using DataGateVPNBot.Handlers;
using DataGateVPNBot.Models;
using DataGateVPNBot.Services.BotServices.Interfaces;
using OpenVPNGateMonitor.SharedModels.Enums;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices;

public class TelegramSettingsService : ITelegramSettingsService
{
    private static readonly LocalizedBotCommand[] AllCommands =
    [
        new()
        {
            Command = BotCommands.CommandGetMyFiles,
            Descriptions = new()
            {
                ["en"] = "Get your files for connecting to the VPN",
                ["ru"] = "Получите свои файлы для подключения к VPN",
                ["el"] = "Αποκτήστε τα αρχεία σας για σύνδεση στο VPN"
            }
        },
        new()
        {
            Command = BotCommands.CommandMakeNewFile,
            Descriptions = new()
            {
                ["en"] = "Create a new file for connecting to the VPN",
                ["ru"] = "Создайте новый файл для подключения к VPN",
                ["el"] = "Δημιουργήστε ένα νέο αρχείο για σύνδεση στο VPN"
            }
        },
        new()
        {
            Command = BotCommands.CommandDeleteSelectedFile,
            Descriptions = new()
            {
                ["en"] = "Delete a specific file",
                ["ru"] = "Удалить выбранный файл",
                ["el"] = "Διαγραφή συγκεκριμένου αρχείου"
            }
        },
        new()
        {
            Command = BotCommands.CommandDeleteAllFiles,
            Descriptions = new()
            {
                ["en"] = "Delete all files",
                ["ru"] = "Удалить все файлы",
                ["el"] = "Διαγραφή όλων των αρχείων"
            }
        },
        new()
        {
            Command = BotCommands.CommandHowToUse,
            Descriptions = new()
            {
                ["en"] = "Instructions on how to use the VPN",
                ["ru"] = "Инструкция по использованию VPN",
                ["el"] = "Οδηγίες χρήσης VPN"
            }
        },
        new()
        {
            Command = BotCommands.CommandInstallClient,
            Descriptions = new()
            {
                ["en"] = "Get a link to download OpenVPN client",
                ["ru"] = "Ссылка на загрузку клиента OpenVPN",
                ["el"] = "Λήψη του OpenVPN client"
            }
        },
        new()
        {
            Command = BotCommands.CommandAboutBot,
            Descriptions = new()
            {
                ["en"] = "Information about the bot",
                ["ru"] = "Информация о боте",
                ["el"] = "Πληροφορίες για το bot"
            }
        },
        new()
        {
            Command = BotCommands.CommandAboutProject,
            Descriptions = new()
            {
                ["en"] = "Information about the project",
                ["ru"] = "Информация о проекте",
                ["el"] = "Πληροφορίες για το έργο"
            }
        },
        new()
        {
            Command = BotCommands.CommandContacts,
            Descriptions = new()
            {
                ["en"] = "Developer contacts",
                ["ru"] = "Контакты разработчика",
                ["el"] = "Στοιχεία επικοινωνίας του προγραμματιστή"
            }
        },
        new()
        {
            Command = BotCommands.CommandChangeLanguage,
            Descriptions = new()
            {
                ["en"] = "Change your language",
                ["ru"] = "Изменить язык",
                ["el"] = "Αλλάξτε τη γλώσσα σας"
            }
        }
    ];

    public BotCommand[] GetTelegramMenuByLanguage(Language language)
    {
        var langCode = language switch
        {
            Language.English => "en",
            Language.Russian => "ru",
            Language.Greek => "el",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };

        return AllCommands.Select(c => c.ToTelegramCommand(langCode)).ToArray();
    }
}
