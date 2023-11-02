using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.BotAsJoke.Polling.Storage;

namespace Telegram.Bot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly StorageTableProvider _storageTableProvider;

    static Dictionary<string, string> files = new()
    {
        { "1", "photo_5188245662809050727_x.jpg" },
        { "2", "photo_5190497462622737539_y.jpg" },
        { "3", "photo_5231469462056720799_y.jpg" },
        { "4", "photo_5233283389954577626_y.jpg" },
        { "5", "photo_5235535189768262857_x.jpg" },
        { "6", "photo_5235535189768262857_x.jpg" },
        { "7", "photo_5235643762246537613_x.jpg" },
        { "8", "photo_5267085624387687137_x.jpg" },
        { "9", "photo_5291932967972753702_y.jpg" },
        { "10", "photo_5467383988332121077_y.jpg" },
        { "11", "photo_5469634856137903679_x.jpg" },
        { "12", "photo_5474138455765274187_y.jpg" },
    };

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger, StorageTableProvider storageTableProvider)
    {
        _storageTableProvider = storageTableProvider;
        _botClient = botClient;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message }                       => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message }                 => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery }           => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery }               => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            _                                              => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;

        Random random = new Random();
        
        var action = messageText.Split(' ')[0] switch
        {
            "/inlinekeyboard"  => SendInlineKeyboard(_botClient, message, cancellationToken),
            "/keyboard"        => SendReplyKeyboard(_botClient, message, cancellationToken),
            "/normalkeyboard"  => SendNormalKeyboard(_botClient, message, cancellationToken),
            "/remove"          => RemoveKeyboard(_botClient, message, cancellationToken),
            "/randomMeme"      => SendFile(_botClient, message, random.Next(1, 12).ToString(), cancellationToken),
            "/request"         => RequestContactAndLocation(_botClient, message, cancellationToken),
            "/inline_mode"     => StartInlineQuery(_botClient, message, cancellationToken),
            "/throw"           => FailingHandler(_botClient, message, cancellationToken),
            _                  => Usage(_botClient, message, cancellationToken)
        };
        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

        static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                        new KeyboardButton[] { "1.1", "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
                })
            {
                ResizeKeyboard = true
            };

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Choose",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }

        static async Task<Message> RemoveKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Removing keyboard",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        static async Task<Message> RequestContactAndLocation(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup RequestReplyKeyboard = new(
                new[]
                {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Who or Where are you?",
                replyMarkup: RequestReplyKeyboard,
                cancellationToken: cancellationToken);
        }

        static async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            const string usage = "Використання:\n" +
                                 "/randomMeme          - Отримати випадковий мемас\n" +
                                 "/inlinekeyboard      - Показати клавіатуру\n" +
                                 "/normalkeyboard      - Показати normalkeyboard";

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: usage,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        static async Task<Message> StartInlineQuery(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            InlineKeyboardMarkup inlineKeyboard = new(
                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode"));

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Press the button to start Inline Query",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }

#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable IDE0060 // Remove unused parameter
        static Task<Message> FailingHandler(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            throw new IndexOutOfRangeException();
        }
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter.
    }


    static async Task<Message> SendNormalKeyboard(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        var keyboardButtons =
            new KeyboardButton[]
            {
                "Help",
                "About",
            };

        var keyboard = new ReplyKeyboardMarkup(keyboardButtons)
        {
            ResizeKeyboard = true
        };

        return await botClient.SendTextMessageAsync(message.Chat.Id, "My Keyboard", replyMarkup: keyboard, cancellationToken: cancellationToken);
    }

    // Send inline keyboard
    // You can process responses in BotOnCallbackQueryReceived handler
    static async Task<Message> SendInlineKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await botClient.SendChatActionAsync(
            chatId: message.Chat.Id,
            chatAction: ChatAction.Typing,
            cancellationToken: cancellationToken);

        // Simulate longer running task
        await Task.Delay(100, cancellationToken);

        InlineKeyboardMarkup inlineKeyboard = new(
            new[]
            {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1", "1"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("7", "7"),
                    }
            });

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Номер мема",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }

    static async Task<Message> SendFile(ITelegramBotClient botClient, Message message, string memeId, CancellationToken cancellationToken)
    {
        await botClient.SendChatActionAsync(
            message.Chat.Id,
            ChatAction.UploadPhoto,
            cancellationToken: cancellationToken);
        string filePath = files[memeId];
        await using FileStream fileStream = new($"Files/{filePath}", FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

        return await botClient.SendPhotoAsync(
            chatId: message.Chat.Id,
            photo: new InputFileStream(fileStream, fileName),
            caption: "Nice Picture",
            cancellationToken: cancellationToken);
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        await SendFile(_botClient, callbackQuery.Message, callbackQuery.Data, cancellationToken);
        await SendInlineKeyboard(_botClient, callbackQuery.Message, cancellationToken);
        //await _botClient.AnswerCallbackQueryAsync(
        //    callbackQueryId: callbackQuery.Id,
        //    text: $"Received {callbackQuery.Data}",
        //    cancellationToken: cancellationToken);

        //await _botClient.SendTextMessageAsync(
        //    chatId: callbackQuery.Message!.Chat.Id,
        //    text: $"Received {callbackQuery.Data}",
        //    cancellationToken: cancellationToken);
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion
    
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}
