# DataGateVPNBot

**DataGateVPNBot** is a Telegram bot for managing OpenVPN. It provides users with functions such as registration, configuration file generation, retrieving client configurations, and other useful information for working with VPN.

## Key Features

- **Registration**: Register users in the system.
- **OpenVPN Management**: Create and retrieve client configuration files.
- **Language Selection**: Support for multiple languages (English, Russian, Greek).
- **Interactive Keyboards**: Support for inline and reply keyboards.
- **Polls**: Create and handle polls.
- **Multiple Commands**: A variety of commands for bot interaction.

---

## Commands

The bot supports the following commands:

| Command                   | Description                                                             |
|---------------------------|-------------------------------------------------------------------------|
| `/about_bot`              | Information about the bot.                                              |
| `/how_to_use`             | Instructions on how to use VPN.                                         |
| `/register`               | User registration.                                                      |
| `/get_my_files`           | Retrieve VPN client configurations.                                     |
| `/make_new_file`          | Create a new configuration file.                                        |
| `/install_client`         | Links to download OpenVPN clients for various platforms.                |
| `/about_project`          | Information about the project.                                          |
| `/contacts`               | Developer contact information.                                          |
| `/change_language`        | Language selection.                                                     |
| `/poll`                   | Create an anonymous poll.                                               |
| `/inline_buttons`         | Example of using inline keyboards.                                      |
| `/keyboard`               | Example of using reply keyboards.                                       |
| `/remove`                 | Remove the keyboard.                                                    |

---
## Generate Configuration Files

To generate `appsettings.json` and `appsettings.Development.json` from the template:

1. Run the following command in the project root:
   ```bash
   ./generate_appsettings.sh
   
2. Ensure that the .env file exists in the project root with the necessary environment variables.
This script uses appsettings.json.template and populates it with values from .env to create the required configuration files.
   ```env
   DB_HOST=localhost
   DB_PORT=5432
   DB_NAME=mydatabase
   DB_USER=myuser
   DB_PASS=mypassword
   DB_SCHEMA=mydbschema
   DB_MIGRATION_TABLE=__EFMigrationsHistory
   BOT_TOKEN=mybottoken
   BOT_WEBHOOK_URL=https://example.com/bot
   OPENVPN_SERVER_IP=0.0.0.0

## Installation and Launch

### Prerequisites
- Raspberry Pi with .NET 6 Runtime installed.
- OpenVPN server.
- Telegram Bot API Token (obtain the token via [@BotFather](https://t.me/BotFather)).

### Installation Instructions
1. Clone the repository:
   ```bash
   git clone https://github.com/IMKolganov/DataGateVPNBot.git
   cd DataGateVPNBot
   ```

2. Install dependencies:
   ```bash
   dotnet restore
   ```

3. Build the application:
   ```bash
   dotnet build
   ```

4. Run the bot:
   ```bash
   dotnet run
   ```

### Deployment on Raspberry Pi
Automatic deployment is handled via **GitHub Actions**. See the [workflow file](.github/workflows/deploy.yml) for configuration details.

### Deployment behind nginx
Set `USE_CERTIFICATE=false`, `ForwardedHeaders__Enabled=true`, and `ForwardedHeaders__AllowAll=true`; app listens HTTP on `TELEGRAMBOT_LISTEN_PORT` (default 5050).

---

## Main Components

- **UpdateHandler**: Main handler for Telegram updates. Implements the core bot functionality.
- **IOpenVpnClientService**: Interface for handling OpenVPN client configurations.
- **ILocalizationService**: Interface for text localization.
- **ITelegramRegistrationService**: Interface for user registration management.

---

## Links

- [OpenVPN Client for Windows](https://openvpn.net/client-connect-vpn-for-windows/)
- [OpenVPN Client for Android](https://play.google.com/store/apps/details?id=net.openvpn.openvpn)
- [OpenVPN Client for iPhone](https://apps.apple.com/app/openvpn-connect/id590379981)
- [Telegram API](https://core.telegram.org/bots/api)

---

## Contacts

Developer: [Kolganov Ivan](https://github.com/IMKolganov)  
Contact via [Telegram](https://t.me/KolganovIvan)
