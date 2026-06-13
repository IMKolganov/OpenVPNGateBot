# DataGateVPNBot

Telegram bot for [DataGate](https://datagateapp.com/) / DataGate Monitor: registration, VPN config delivery, and user-facing commands.

Part of the [DataGateMonitor](https://github.com/IMKolganov/DataGateMonitor) monorepo (`telegrambot/` submodule). Standalone repo: [DataGateVPNBot](https://github.com/IMKolganov/DataGateVPNBot).

## Links

| Resource | Link |
|----------|------|
| <img src="https://raw.githubusercontent.com/IMKolganov/DataGateMonitorFrontend/main/public/favicon.svg" width="16" height="16" alt="" /> **Product** | [datagateapp.com](https://datagateapp.com/) |
| <img src="https://cdn.simpleicons.org/googleplay/414141" width="16" height="16" alt="" /> **Download** | [datagateapp.com/download](https://datagateapp.com/download) |
| <img src="https://cdn.simpleicons.org/grafana/F46800" width="16" height="16" alt="" /> **Dashboard** | [dash.datagateapp.com](https://dash.datagateapp.com/) |
| <img src="https://cdn.simpleicons.org/telegram/26A5E4" width="16" height="16" alt="" /> **Telegram channel** | [@datagateapp](https://t.me/datagateapp) |

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

| Contact | Link |
|---------|------|
| <img src="https://cdn.simpleicons.org/linkedin/0A66C2" width="16" height="16" alt="" /> **LinkedIn** | [linkedin.com/in/imkolganov](https://www.linkedin.com/in/imkolganov/?locale=en) |
| <img src="https://cdn.simpleicons.org/telegram/26A5E4" width="16" height="16" alt="" /> **Telegram** | [@KolganovIvan](https://t.me/KolganovIvan) |
| <img src="https://cdn.simpleicons.org/buymeacoffee/FFDD00" width="16" height="16" alt="" /> **Buy Me a Coffee** | [buymeacoffee.com/imkolganov](https://buymeacoffee.com/imkolganov) |
| <img src="https://cdn.simpleicons.org/github/181717" width="16" height="16" alt="" /> **GitHub** | [IMKolganov](https://github.com/IMKolganov) |
| <img src="https://cdn.simpleicons.org/telegram/26A5E4" width="16" height="16" alt="" /> **Product channel** | [@datagateapp](https://t.me/datagateapp) |
