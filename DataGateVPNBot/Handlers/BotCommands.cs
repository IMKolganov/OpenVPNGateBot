namespace DataGateVPNBot.Handlers;

public static class BotCommands
{
    public const string CommandStart = "/start";
    public const string CommandAboutBot = "/about_bot";
    public const string CommandHowToUse = "/how_to_use";
    public const string CommandRegister = "/register";
    /// <summary>One-time dashboard login code (5 minutes).</summary>
    public const string CommandLoginCode = "/login_code";
    /// <summary>Link dashboard (Google/password) account using a code from the client app.</summary>
    public const string CommandLinkAccount = "/link_account";
    public const string CommandGetMyFiles = "/get_my_files";
    public const string CommandGetMyFilesWithToken = "/get_my_files_with_token";
    public const string CommandGetMyFilesWithoutToken = "/get_my_files_without_token";
    public const string CommandMakeNewFile = "/make_new_file";
    public const string CommandMakeNewFileWithoutToken = "/make_new_file_without_token";
    public const string CommandMakeNewFileWithToken = "/make_new_file_with_token";
    public const string CommandDeleteSelectedFile = "/delete_selected_file";
    public const string CommandDeleteAllFiles = "/delete_all_files";
    public const string CommandInstallClient = "/install_client";
    public const string CommandAboutProject = "/about_project";
    public const string CommandContacts = "/contacts";
    /// <summary>Voluntary /donate via Telegram Stars. Does not change VPN plan.</summary>
    public const string CommandDonate = "/donate";
    public const string CommandChangeLanguage = "/change_language";
    public const string CommandRegisterCommands = "/register_commands";
    public const string CommandEnglish = "/english";
    public const string CommandRussian = "/русский";
    public const string CommandGreek = "/ελληνικά";
    public const string CommandDashboardApiGetToken = "/dashboard_api_get_token";
    public const string CommandPhoto = "/photo";
    public const string CommandInlineButtons = "/inline_buttons";
    public const string CommandKeyboard = "/keyboard";
    public const string CommandRemove = "/remove";
    public const string CommandRequest = "/request";
    public const string CommandInlineMode = "/inline_mode";
    public const string CommandPoll = "/poll";
    public const string CommandPollAnonymous = "/poll_anonymous";
    public const string CommandThrow = "/throw";

    /// <summary>Admin only: push current Telegram profile photos into the dashboard for every registered user.</summary>
    public const string CommandRefreshProfilePhotos = "/refresh_profile_photos";

    /// <summary>Admin only: force the Free/Default unsubscribed-online VPN digest (same as daily admin alert).</summary>
    public const string CommandUnsubscribedVpnUsers = "/unsubscribed_vpn_users";

    /// <summary>Admin only: send a localized channel-subscribe reminder to a user (userId or Telegram id).</summary>
    public const string CommandRemindChannelSubscribe = "/remind_channel_subscribe";

    /// <summary>Admin only: email a channel-subscribe reminder to a dashboard userId.</summary>
    public const string CommandRemindChannelEmail = "/remind_channel_email";

    /// <summary>
    /// Argument for remind commands / digest keyboard: send to every actionable digest candidate
    /// for that channel (not a user id).
    /// </summary>
    public const string RemindAllTarget = "all";
}