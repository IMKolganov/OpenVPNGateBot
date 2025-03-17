using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    private async Task<Message> AboutBot(Message msg, CancellationToken cancellationToken)
    {
        return await _botClient.SendMessage(
            msg.Chat,
            await GetLocalizationTextAsync("AboutBot", msg.From!.Id, cancellationToken), 
            cancellationToken: cancellationToken);
    }
    
    private async Task<Message> HowToUseVpn(Message msg, CancellationToken cancellationToken)
    {
        return await _botClient.SendMessage(
            msg.Chat,
            await GetLocalizationTextAsync("HowToUseVPN", msg.From!.Id, cancellationToken), 
            cancellationToken: cancellationToken);
    }
    
    private async Task<Message> InstallClient(Message msg, CancellationToken cancellationToken)
    {
        var inlineMarkup = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithUrl("🖥 Windows", "https://openvpn.net/client-connect-vpn-for-windows/"),
                InlineKeyboardButton.WithUrl("📱 Android", "https://play.google.com/store/apps/details?id=net.openvpn.openvpn"),
                InlineKeyboardButton.WithUrl("🍎 iPhone", "https://apps.apple.com/app/openvpn-connect/id590379981")
            ],
            [
                InlineKeyboardButton.WithUrl(
                    await GetLocalizationTextAsync("AboutOpenVPN", msg.Chat.Id, cancellationToken), 
                    "https://openvpn.net/faq/what-is-openvpn/")
            ]
        ]);

        return await _botClient.SendMessage(
            msg.Chat,
            await GetLocalizationTextAsync("ChoosePlatform", msg.Chat.Id, cancellationToken),
            replyMarkup: inlineMarkup, 
            cancellationToken: cancellationToken);
    }
    
    private async Task<Message> AboutProject(Message msg, CancellationToken cancellationToken)
    {
        var inlineMarkup = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithUrl(
                    await GetLocalizationTextAsync("WhatIsRaspberryPi", msg.From!.Id, cancellationToken),
                    "https://www.raspberrypi.org/about/")
            ]
        ]);
        
        return await _botClient.SendMessage(
            msg.Chat, 
            await GetLocalizationTextAsync("AboutProject", msg.From!.Id, cancellationToken),
            replyMarkup: inlineMarkup, 
            cancellationToken: cancellationToken);
    }
    
    private async Task<Message> Contacts(Message msg, CancellationToken cancellationToken)
    {
        var inlineMarkup = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithUrl("Telegram", "https://t.me/KolganovIvan"),
                InlineKeyboardButton.WithUrl("GitHub", "https://github.com/IMKolganov")
            ]
        ]);
        
        return await _botClient.SendMessage(
            msg.Chat,
            await GetLocalizationTextAsync("DeveloperContacts", msg.From!.Id, cancellationToken),
            replyMarkup: inlineMarkup, 
            cancellationToken: cancellationToken);
    }
}