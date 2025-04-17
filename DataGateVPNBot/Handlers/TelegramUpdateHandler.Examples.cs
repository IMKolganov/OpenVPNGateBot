using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    private readonly InputPollOption[] _pollOptions =
    [
        new InputPollOption("Hello"),
        new InputPollOption("World!")
    ];

    private async Task<Message> SendPhoto(Message msg)
    {
        await _botClient.SendChatAction(msg.Chat, ChatAction.UploadPhoto);
        // await Task.Delay(2000); // simulate a long task
        await using var fileStream = new FileStream("Photo/bot.gif", FileMode.Open, FileAccess.Read);
        return await _botClient.SendAnimation(msg.Chat, fileStream, caption: "Read https://github.com/IMKolganov/DataGateVPNBot");
    }

    // Send inline keyboard. You can process responses in OnCallbackQuery handler
    private async Task<Message> SendInlineKeyboard(Message msg)
    {
        var inlineMarkup = new InlineKeyboardMarkup()
            .AddNewRow("1.1", "1.2", "1.3")
            .AddNewRow()
                .AddButton("WithCallbackData", "CallbackData")
                .AddButton(InlineKeyboardButton.WithUrl("WithUrl", "https://github.com/TelegramBots/Telegram.Bot"));
        return await _botClient.SendMessage(msg.Chat, "Inline buttons:", replyMarkup: inlineMarkup);
    }

    private async Task<Message> SendReplyKeyboard(Message msg)
    {
        var replyMarkup = new ReplyKeyboardMarkup(true)//WARNING! ReplyKeyboardMarkup is not support at all clients
            .AddNewRow("1.1", "1.2", "1.3")
            .AddNewRow().AddButton("2.1").AddButton("2.2");
        return await _botClient.SendMessage(msg.Chat, "Keyboard buttons:", replyMarkup: replyMarkup);
    }

    private async Task<Message> RemoveKeyboard(Message msg)
    {
        return await _botClient.SendMessage(msg.Chat, "Removing keyboard", replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> RequestContactAndLocation(Message msg)
    {
        var replyMarkup = new ReplyKeyboardMarkup(true)//WARNING! ReplyKeyboardMarkup is not support at all clients
            .AddButton(KeyboardButton.WithRequestLocation("Location"))
            .AddButton(KeyboardButton.WithRequestContact("Contact"));
        return await _botClient.SendMessage(msg.Chat, "Who or Where are you?", replyMarkup: replyMarkup);
    }

    private async Task<Message> StartInlineQuery(Message msg)
    {
        var button = InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode");
        return await _botClient.SendMessage(msg.Chat, "Press the button to start Inline Query\n\n" +
                                                      "(Make sure you enabled Inline Mode in @BotFather)", replyMarkup: new InlineKeyboardMarkup(button));
    }

    private async Task<Message> SendPoll(Message msg)
    {
        return await _botClient.SendPoll(msg.Chat, "Question", _pollOptions, isAnonymous: false);
    }

    private async Task<Message> SendAnonymousPoll(Message msg)
    {
        return await _botClient.SendPoll(chatId: msg.Chat, "Question", _pollOptions);
    }

    private static Task<Message> FailingHandler()
    {
        throw new NotImplementedException("FailingHandler");
    }


    #region Inline Mode

    private async Task OnInlineQuery(InlineQuery inlineQuery)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results =
        [
            new InlineQueryResultArticle("1", "Telegram.Bot", new InputTextMessageContent("hello")),
            new InlineQueryResultArticle("2", "is the best", new InputTextMessageContent("world"))
        ];
        await _botClient.AnswerInlineQuery(inlineQuery.Id, results, cacheTime: 0, isPersonal: true);
    }

    private async Task OnChosenInlineResult(ChosenInlineResult chosenInlineResult)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);
        await _botClient.SendMessage(chosenInlineResult.From.Id, $"You chose result with Id: {chosenInlineResult.ResultId}");
    }

    #endregion

    private Task OnPoll(Poll poll)
    {
        _logger.LogInformation("Received Poll info: {Question}", poll.Question);
        return Task.CompletedTask;
    }

    private async Task OnPollAnswer(PollAnswer pollAnswer)
    {
        var answer = pollAnswer.OptionIds.FirstOrDefault();
        var selectedOption = _pollOptions[answer];
        if (pollAnswer.User != null)
            await _botClient.SendMessage(pollAnswer.User.Id, $"You've chosen: {selectedOption.Text} in poll");
    }
}