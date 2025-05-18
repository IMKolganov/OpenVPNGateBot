using DataGateVPNBot.Services.BotServices.Interfaces;
using OpenVPNGateMonitor.SharedModels.Enums;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices;

public class TelegramSettingsService : ITelegramSettingsService
{
    public BotCommand[] GetTelegramMenuByLanguage(Language language)
    {
        switch (language)
        {
            case Language.English:
                return
                [
                    // new BotCommand { Command = "register", Description = "Register to use the VPN" },
                    new BotCommand { Command = "get_my_files", Description = "Get your files for connecting to the VPN" },
                    new BotCommand { Command = "make_new_file", Description = "Create a new file for connecting to the VPN" },
                    new BotCommand { Command = "delete_selected_file", Description = "Delete a specific file" },
                    new BotCommand { Command = "delete_all_files", Description = "Delete all files" },
                    new BotCommand { Command = "how_to_use", Description = "Instructions on how to use the VPN" },
                    new BotCommand { Command = "install_client", Description = "Get a link to download OpenVPN client" },
                    new BotCommand { Command = "about_bot", Description = "Information about the bot" },
                    new BotCommand { Command = "about_project", Description = "Information about the project" },
                    new BotCommand { Command = "contacts", Description = "Developer contacts" },
                    new BotCommand { Command = "change_language", Description = "Change your language" }
                ];
            case Language.Russian:
                return
                [
                    // new BotCommand { Command = "register", Description = "Зарегистрируйтесь для использования VPN" },
                    new BotCommand { Command = "get_my_files", Description = "Получите свои файлы для подключения к VPN" },
                    new BotCommand { Command = "make_new_file", Description = "Создайте новый файл для подключения к VPN" },
                    new BotCommand { Command = "delete_selected_file", Description = "Удалить выбранный файл" },
                    new BotCommand { Command = "delete_all_files", Description = "Удалить все файлы" },
                    new BotCommand { Command = "how_to_use", Description = "Инструкция по использованию VPN" },
                    new BotCommand { Command = "install_client", Description = "Ссылка на загрузку клиента OpenVPN" },
                    new BotCommand { Command = "about_bot", Description = "Информация о боте" },
                    new BotCommand { Command = "about_project", Description = "Информация о проекте" },
                    new BotCommand { Command = "contacts", Description = "Контакты разработчика" },
                    new BotCommand { Command = "change_language", Description = "Изменить язык" }
                ];
            case Language.Greek:
                return
                [
                    // new BotCommand { Command = "register", Description = "Εγγραφείτε για να χρησιμοποιήσετε το VPN" },
                    new BotCommand { Command = "get_my_files", Description = "Αποκτήστε τα αρχεία σας για σύνδεση στο VPN" },
                    new BotCommand { Command = "make_new_file", Description = "Δημιουργήστε ένα νέο αρχείο για σύνδεση στο VPN" },
                    new BotCommand { Command = "delete_selected_file", Description = "Διαγραφή συγκεκριμένου αρχείου" },
                    new BotCommand { Command = "delete_all_files", Description = "Διαγραφή όλων των αρχείων" },
                    new BotCommand { Command = "how_to_use", Description = "Οδηγίες χρήσης VPN" },
                    new BotCommand { Command = "install_client", Description = "Λήψη του OpenVPN client" },
                    new BotCommand { Command = "about_bot", Description = "Πληροφορίες για το bot" },
                    new BotCommand { Command = "about_project", Description = "Πληροφορίες για το έργο" },
                    new BotCommand { Command = "contacts", Description = "Στοιχεία επικοινωνίας του προγραμματιστή" },
                    new BotCommand { Command = "change_language", Description = "Αλλάξτε τη γλώσσα σας" }
                ];
            default:
                throw new ArgumentOutOfRangeException(nameof(language), language, null);
        }
    }
}