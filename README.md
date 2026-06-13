# DataGateVPNBot

Telegram bot for [DataGate](https://datagateapp.com/) / DataGate Monitor: registration, VPN config delivery, and user-facing commands.

Part of the [DataGateMonitor](https://github.com/IMKolganov/DataGateMonitor) monorepo (`telegrambot/` submodule). Standalone repo: [DataGateVPNBot](https://github.com/IMKolganov/DataGateVPNBot).

## Links

| | |
|---|---|
| **App download** | [datagateapp.com/download](https://datagateapp.com/download) |
| **Dashboard** | [dash.datagateapp.com](https://dash.datagateapp.com/) |
| **Telegram channel** | [@datagateapp](https://t.me/datagateapp) |

## Key features

- User registration
- OpenVPN / client configuration delivery
- Multi-language support (English, Russian, Greek)
- Inline and reply keyboards, polls

## Commands

| Command | Description |
|---------|-------------|
| `/about_bot` | Information about the bot |
| `/how_to_use` | How to use VPN |
| `/register` | Register in the system |
| `/get_my_files` | Retrieve VPN client configurations |
| `/make_new_file` | Create a new configuration file |
| `/install_client` | Download links for OpenVPN clients |
| `/about_project` | About the project |
| `/contacts` | Developer contacts |
| `/change_language` | Language selection |
| `/poll` | Anonymous poll |
| `/inline_buttons` | Inline keyboard example |
| `/keyboard` | Reply keyboard example |
| `/remove` | Remove keyboard |

## Configuration

Generate `appsettings.json` from template:

```bash
./generate_appsettings.sh
```

Requires a `.env` in the project root (see `appsettings.json.template`).

## Installation and launch

### Prerequisites

- .NET SDK (see project `TargetFramework`)
- PostgreSQL (when using DB features)
- Telegram Bot token from [@BotFather](https://t.me/BotFather)
- OpenVPN / backend integration as configured in compose

### Local run

```bash
git clone https://github.com/IMKolganov/DataGateVPNBot.git
cd DataGateVPNBot
dotnet restore
dotnet build
dotnet run
```

### Docker (monorepo)

From the monorepo root:

```bash
docker compose -f docker-compose-local.yml --env-file .env.dev.x64 up -d --build telegrambot
```

Image: `imkolganov/datagate-monitor-telegrambot`.

Env: `TELEGRAMBOT_BOT_TOKEN`, `DASHBOARDAPI_*`, `ELASTIC_*`, etc. (see monorepo compose).

### Deployment behind nginx

Set `USE_CERTIFICATE=false`, `ForwardedHeaders__Enabled=true`, and `ForwardedHeaders__AllowAll=true`. The app listens on HTTP at `TELEGRAMBOT_LISTEN_PORT` (default `5050`).

CI/CD: see [.github/workflows/deploy.yml](.github/workflows/deploy.yml).

## Main components

- **UpdateHandler** — Telegram update pipeline
- **IOpenVpnClientService** — OpenVPN client configs
- **ILocalizationService** — localization
- **ITelegramRegistrationService** — registration

## External links

- [OpenVPN Connect (Windows)](https://openvpn.net/client-connect-vpn-for-windows/)
- [OpenVPN Connect (Android)](https://play.google.com/store/apps/details?id=net.openvpn.openvpn)
- [OpenVPN Connect (iOS)](https://apps.apple.com/app/openvpn-connect/id590379981)
- [Telegram Bot API](https://core.telegram.org/bots/api)

## Author

**Ivan Kolganov**

- [LinkedIn](https://www.linkedin.com/in/imkolganov/?locale=en)
- [Telegram](https://t.me/KolganovIvan)
- [Buy Me a Coffee](https://buymeacoffee.com/imkolganov)
- GitHub: [IMKolganov](https://github.com/IMKolganov)

Product channel: [@datagateapp](https://t.me/datagateapp)
