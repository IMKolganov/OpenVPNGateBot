using DataGateVPNBot.Services.Donations;
using DataGateVPNBot.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    private async Task OnChatBoostUpdateAsync(ChatBoostUpdated update, CancellationToken cancellationToken)
    {
        var boost = update.Boost;
        var (who, source) = DescribeBoostSource(boost?.Source);

        var text =
            "ℹ️ Chat boost added\n" +
            $"Chat: {DescribeChat(update.Chat)}\n" +
            $"User: {who}\n" +
            $"Source: {source}\n" +
            $"BoostId: {boost?.BoostId ?? "Unknown"}\n" +
            $"Added: {FormatUtc(boost?.AddDate)}\n" +
            $"Expires: {FormatUtc(boost?.ExpirationDate)}";

        _logger.LogInformation(
            "ChatBoost update. Chat={Chat}; Who={Who}; BoostId={BoostId}",
            DescribeChat(update.Chat), who, boost?.BoostId);

        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task OnRemovedChatBoostUpdateAsync(ChatBoostRemoved update, CancellationToken cancellationToken)
    {
        var (who, source) = DescribeBoostSource(update.Source);

        var text =
            "ℹ️ Chat boost removed\n" +
            $"Chat: {DescribeChat(update.Chat)}\n" +
            $"User: {who}\n" +
            $"Source: {source}\n" +
            $"BoostId: {update.BoostId}\n" +
            $"Removed: {FormatUtc(update.RemoveDate)}";

        _logger.LogInformation(
            "RemovedChatBoost update. Chat={Chat}; Who={Who}; BoostId={BoostId}",
            DescribeChat(update.Chat), who, update.BoostId);

        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task OnChatJoinRequestUpdateAsync(ChatJoinRequest update, CancellationToken cancellationToken)
    {
        var invite = update.InviteLink?.InviteLink
                     ?? (string.IsNullOrWhiteSpace(update.InviteLink?.Name) ? "—" : update.InviteLink.Name);

        var text =
            "ℹ️ Join request\n" +
            $"Chat: {DescribeChat(update.Chat)}\n" +
            $"User: {DescribeUser(update.From)}\n" +
            $"Invite: {invite}\n" +
            $"Bio: {Truncate(update.Bio, 200)}\n" +
            $"Time: {FormatUtc(update.Date)}";

        _logger.LogInformation(
            "ChatJoinRequest update. Chat={Chat}; Who={Who}",
            DescribeChat(update.Chat), DescribeUser(update.From));

        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task OnMessageReactionUpdateAsync(MessageReactionUpdated update, CancellationToken cancellationToken)
    {
        var actor = update.User != null
            ? DescribeUser(update.User)
            : update.ActorChat != null
                ? DescribeChat(update.ActorChat)
                : "Unknown";

        var text =
            "ℹ️ Message reaction\n" +
            $"Chat: {DescribeChat(update.Chat)}\n" +
            $"User: {actor}\n" +
            $"MessageId: {update.MessageId}\n" +
            $"{DescribeReactions(update.OldReaction)} -> {DescribeReactions(update.NewReaction)}\n" +
            $"Time: {FormatUtc(update.Date)}";

        _logger.LogInformation(
            "MessageReaction update. Chat={Chat}; MessageId={MessageId}; Who={Who}",
            DescribeChat(update.Chat), update.MessageId, actor);

        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task OnMessageReactionCountUpdateAsync(MessageReactionCountUpdated update, CancellationToken cancellationToken)
    {
        var counts = update.Reactions is { Length: > 0 }
            ? string.Join(", ", update.Reactions.Select(r => $"{DescribeReaction(r.Type)}×{r.TotalCount}"))
            : "none";

        var text =
            "ℹ️ Message reaction counts\n" +
            $"Chat: {DescribeChat(update.Chat)}\n" +
            $"MessageId: {update.MessageId}\n" +
            $"Counts: {counts}\n" +
            $"Time: {FormatUtc(update.Date)}";

        _logger.LogInformation(
            "MessageReactionCount update. Chat={Chat}; MessageId={MessageId}",
            DescribeChat(update.Chat), update.MessageId);

        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task OnBusinessConnectionUpdateAsync(BusinessConnection update, CancellationToken cancellationToken)
    {
        var text =
            "ℹ️ Business connection\n" +
            $"User: {DescribeUser(update.User)}\n" +
            $"Enabled: {update.IsEnabled}\n" +
            $"ConnectionId: {update.Id}\n" +
            $"UserChatId: {update.UserChatId}\n" +
            $"Time: {FormatUtc(update.Date)}";

        _logger.LogInformation(
            "BusinessConnection update. Id={Id}; User={User}; IsEnabled={IsEnabled}",
            update.Id, DescribeUser(update.User), update.IsEnabled);

        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task OnDeletedBusinessMessagesUpdateAsync(BusinessMessagesDeleted update, CancellationToken cancellationToken)
    {
        var ids = update.MessageIds is { Length: > 0 }
            ? string.Join(", ", update.MessageIds.Take(20)) + (update.MessageIds.Length > 20 ? "…" : "")
            : "none";

        var text =
            "ℹ️ Business messages deleted\n" +
            $"Chat: {DescribeChat(update.Chat)}\n" +
            $"ConnectionId: {update.BusinessConnectionId}\n" +
            $"Count: {update.MessageIds?.Length ?? 0}\n" +
            $"MessageIds: {ids}";

        _logger.LogInformation(
            "DeletedBusinessMessages update. ConnectionId={Id}; Chat={Chat}; Count={Count}",
            update.BusinessConnectionId, DescribeChat(update.Chat), update.MessageIds?.Length ?? 0);

        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task OnShippingQueryUpdateAsync(ShippingQuery update, CancellationToken cancellationToken)
    {
        var address = update.ShippingAddress;
        var addressText = address == null
            ? "—"
            : string.Join(", ", new[]
            {
                address.CountryCode,
                address.State,
                address.City,
                address.StreetLine1,
                address.StreetLine2,
                address.PostCode
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var text =
            "ℹ️ Shipping query\n" +
            $"User: {DescribeUser(update.From)}\n" +
            $"QueryId: {update.Id}\n" +
            $"Payload: {Truncate(update.InvoicePayload, 200)}\n" +
            $"Address: {addressText}";

        _logger.LogInformation("ShippingQuery update. QueryId={Id}; Who={Who}", update.Id, DescribeUser(update.From));

        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task OnPreCheckoutQueryUpdateAsync(PreCheckoutQuery update, CancellationToken cancellationToken)
    {
        if (StarsDonation.IsDonatePreCheckout(update.Currency, update.InvoicePayload))
        {
            _logger.LogInformation(
                "Donate pre-checkout. QueryId={Id}; Who={Who}; Amount={Amount} {Currency}",
                update.Id, DescribeUser(update.From), update.TotalAmount, update.Currency);
            await AnswerDonatePreCheckoutAsync(update, cancellationToken);
            return;
        }

        var text =
            "ℹ️ Pre-checkout query\n" +
            $"User: {DescribeUser(update.From)}\n" +
            $"QueryId: {update.Id}\n" +
            $"Amount: {update.TotalAmount} {update.Currency}\n" +
            $"Payload: {Truncate(update.InvoicePayload, 200)}\n" +
            $"ShippingOptionId: {update.ShippingOptionId ?? "—"}";

        _logger.LogInformation(
            "PreCheckoutQuery update. QueryId={Id}; Who={Who}; Amount={Amount} {Currency}",
            update.Id, DescribeUser(update.From), update.TotalAmount, update.Currency);

        await AnswerDonatePreCheckoutAsync(update, cancellationToken);
        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task OnPurchasedPaidMediaUpdateAsync(PaidMediaPurchased update, CancellationToken cancellationToken)
    {
        var text =
            "ℹ️ Paid media purchased\n" +
            $"User: {DescribeUser(update.From)}\n" +
            $"Payload: {Truncate(update.PaidMediaPayload, 200)}";

        _logger.LogInformation(
            "PurchasedPaidMedia update. Who={Who}; Payload={Payload}",
            DescribeUser(update.From), update.PaidMediaPayload);

        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task OnManagedBotUpdateAsync(ManagedBotUpdated update, CancellationToken cancellationToken)
    {
        var text =
            "ℹ️ Managed bot update\n" +
            $"Owner: {DescribeUser(update.User)}\n" +
            $"Bot: {DescribeUser(update.Bot)}";

        _logger.LogInformation(
            "ManagedBot update. User={User}; Bot={Bot}",
            DescribeUser(update.User), DescribeUser(update.Bot));

        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task OnSubscriptionUpdateAsync(BotSubscriptionUpdated update, CancellationToken cancellationToken)
    {
        var text =
            "ℹ️ Payment subscription\n" +
            $"User: {DescribeUser(update.User)}\n" +
            $"State: {update.State}\n" +
            $"Payload: {Truncate(update.InvoicePayload, 200)}";

        _logger.LogInformation(
            "Subscription update. Who={Who}; State={State}",
            DescribeUser(update.User), update.State);

        await NotifyAdminsInformationalAsync(text, cancellationToken);
    }

    private async Task NotifyAdminsInformationalAsync(string text, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
        await errorService.SendMessageToAdminsAsync(text, cancellationToken);
    }

    private static (string Who, string Source) DescribeBoostSource(ChatBoostSource? source) =>
        source switch
        {
            ChatBoostSourcePremium premium => (DescribeUser(premium.User), "Premium"),
            ChatBoostSourceGiftCode giftCode => (DescribeUser(giftCode.User), "GiftCode"),
            ChatBoostSourceGiveaway giveaway => (
                giveaway.User != null
                    ? DescribeUser(giveaway.User)
                    : giveaway.IsUnclaimed ? "unclaimed" : "—",
                $"Giveaway (msg={giveaway.GiveawayMessageId}" +
                (giveaway.PrizeStarCount is { } stars ? $", stars={stars}" : "") + ")"),
            null => ("Unknown", "—"),
            _ => ("Unknown", source.Source.ToString())
        };

    private static string DescribeReactions(ReactionType[]? reactions)
    {
        if (reactions is not { Length: > 0 })
            return "none";
        return string.Join(", ", reactions.Select(DescribeReaction));
    }

    private static string DescribeReaction(ReactionType? reaction) =>
        reaction switch
        {
            ReactionTypeEmoji emoji => emoji.Emoji,
            ReactionTypeCustomEmoji custom => $"custom:{custom.CustomEmojiId}",
            ReactionTypePaid => "paid",
            null => "none",
            _ => reaction.Type.ToString()
        };

    private static string FormatUtc(DateTime? dateTime)
    {
        if (dateTime is null || dateTime == default)
            return $"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        return $"{dateTime.Value:yyyy-MM-dd HH:mm:ss} UTC";
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "—";
        return value.Length <= maxLength ? value : value[..maxLength] + "…";
    }
}
