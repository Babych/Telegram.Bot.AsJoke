using System.Globalization;

using Newtonsoft.Json;

using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.BotAsJoke.Polling;
using Telegram.BotAsJoke.Polling.Storage;

namespace Telegram.Bot.Services;

public class BotUpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient botClient;
    private readonly ILogger<UpdateHandler> logger;
    private readonly StorageTableProvider storageTableProvider;


    public BotUpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger, StorageTableProvider storageTableProvider)
    {
        this.storageTableProvider = storageTableProvider;
        this.botClient = botClient;
        this.logger = logger;
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
        logger.LogInformation("Receive message type: {MessageType}", message.Type);

        if (message.Type == MessageType.Photo)
        {
            await PutPhoto(message, cancellationToken);
        }

        if (message.Text is not { } messageText)
            return;

        var action = messageText.Split(' ')[0] switch
        {
            "/start"           => OkMamaBot(message, cancellationToken),
            "/startmamabot"    => StartMamaBot(message, cancellationToken),
            "/endmamabot"      => EndMamaBot(message, cancellationToken),
            "/random_meme"     => GetRandomMeme(message, cancellationToken),
            "/subscribe"       => SubscribeForDelivery(message, cancellationToken),
            "/unsubscribe"     => UnsubscribeForDelivery(message, cancellationToken),
            _                  => Text(message, cancellationToken)
        };

        Message sentMessage = await action;

        Log.Instance.Trace($"The message was sent with id: {sentMessage.MessageId}",
            new Dictionary<string, string>() { { nameof(message), JsonConvert.SerializeObject(message) } });
    }
    async Task<Message> Text(Message message, CancellationToken cancellationToken)
    {
        string response = "Використання:\n" +
                             "/random_meme - Отримати випадковий мемас\n" +
                             "/subscribe - Підписатися";

        IReplyMarkup replyMarkup = new ReplyKeyboardRemove();

        if (await this.storageTableProvider.IsMamaBot(message.Chat.Id.ToString(CultureInfo.InvariantCulture)))
        {
            var textId = await this.storageTableProvider.PutTextItem(message.Text, message.From.Id.ToString(CultureInfo.InvariantCulture));

            response = message.Text;

            replyMarkup = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Розіслати", $"SEND {textId}"),
                    InlineKeyboardButton.WithCallbackData("Переглянути підписників", "LIST_RECIPIENTS"),
                }
            });
        }

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: response,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    async Task<Message> PutPhoto(Message message,
        CancellationToken cancellationToken)
    {
        var storeAsMeme =
            await this.storageTableProvider.IsMamaBot(message.Chat.Id.ToString(CultureInfo.InvariantCulture));

        await this.storageTableProvider.PutPhotoItem(message, storeAsMeme);

        return await botClient.SendTextMessageAsync(message.Chat.Id,
            "Мем добавлено в колекцію\n /random_meme", cancellationToken: cancellationToken);
    }

    async Task<Message> StartMamaBot(Message message,
        CancellationToken cancellationToken)
    {
        await this.storageTableProvider.UpsertMamaBot(message, true);

        return await botClient.SendTextMessageAsync(message.Chat.Id, "Ok mamabot, пришліть текст для розсилки, або мем в колекцію.", cancellationToken: cancellationToken);
    }

    async Task<Message> EndMamaBot(Message message,
        CancellationToken cancellationToken)
    {
        await this.storageTableProvider.UpsertMamaBot(message, false);

        return await botClient.SendTextMessageAsync(message.Chat.Id, "Вийшли з мамабота", cancellationToken: cancellationToken);
    }

    async Task<Message> OkMamaBot(Message message,
        CancellationToken cancellationToken)
    {
        if (await this.storageTableProvider.IsMamaBot(message.Chat.Id.ToString(CultureInfo.InvariantCulture)))
        {
            await this.storageTableProvider.UpsertUserItem(message, false);

            return await botClient.SendTextMessageAsync(message.Chat.Id, "Ok mamabot, відправте ваш текст, або мем.", cancellationToken: cancellationToken);
        }

        return await Text(message, cancellationToken);
    }


    async Task<Message> GetRandomMeme(Message message, CancellationToken cancellationToken)
    {
        await botClient.SendChatActionAsync(
            message.Chat.Id,
            ChatAction.UploadPhoto,
            cancellationToken: cancellationToken);

        var memes = (await this.storageTableProvider.GetMemes()).ToArray();


        if (memes.Length > 0)
        {
            var rnd = new Random();
            var meme = memes[rnd.Next(0, memes.Length)];

            IReplyMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Like", $"LIKE_MEME_{meme.FileUniqueId}"),
                    InlineKeyboardButton.WithCallbackData("Dislike", $"DISLIKE_MEME_{meme.FileUniqueId}"),
                    InlineKeyboardButton.WithCallbackData("X", $"DELETE_MEME_{meme.FileUniqueId}"),
                }
            });

            inlineKeyboard = new ReplyKeyboardRemove();

            return await botClient.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: new InputFileId(meme.FileId),
                caption: "Nice Picture",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }

        return await botClient.SendTextMessageAsync(message.Chat.Id, "Немає мемів.", cancellationToken: cancellationToken);
    }


    async Task<Message> SubscribeForDelivery(Message message, CancellationToken cancellationToken)
    {
        var result = await storageTableProvider.UpsertUserItem(message,  true);

        var resultText = result switch
        {
            true => "Підписано.",
            false => "Перепідписано.",
            null => "Не вдалося підписати.",
        };

        return await botClient.SendTextMessageAsync(message.Chat.Id, resultText,
            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    async Task<Message> UnsubscribeForDelivery(Message message, CancellationToken cancellationToken)
    {
        var result = await storageTableProvider.UpsertUserItem(message,  false);

        var resultText = result switch
        {
            true => "Не було підписано.",
            false => "Відписано.",
            null => "Не вдалося відписатися.",
        };

        return await botClient.SendTextMessageAsync(message.Chat.Id, resultText,
            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }


    async Task<Message> DeleteConfirm(Message message, string memeCallback,  CancellationToken cancellationToken)
    {
        InlineKeyboardMarkup inlineKeyboard = new(
            new[]
            {
                // first row
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Так",  $"CONFIRM {memeCallback}"),
                    InlineKeyboardButton.WithCallbackData("Ні", $"NOT_DELETE_MEME {message.MessageId}"),
                }
            });

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Видаляти?",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }

    async Task LitOfSubscribers(Message? message, CancellationToken cancellationToken)
    {
        var listOfSubscribers = await storageTableProvider.GetListOfSubscribers();

        var subscribersText = string.Join(",\n", listOfSubscribers.Select(x => $"{x.DisplayUserName}"));

        await botClient.SendTextMessageAsync(message.Chat.Id, subscribersText,
            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    async Task ConfirmSendToSubscribers(Message? message, string textId, CancellationToken cancellationToken)
    {
        var text = (await storageTableProvider.GetTextItem(textId))?.Text;
        try
        {
            IReplyMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Підтвердити відправку", $"CONFIRM_SEND_TO_SUBSCRIBERS {textId}"),
                }
            });

            await botClient.SendTextMessageAsync(message!.Chat.Id, text!,
                replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    async Task SendToSubscribers(Message? message, string textId, CancellationToken cancellationToken)
    {
        var text = (await this.storageTableProvider.GetTextItem(textId)).Text;
        var listOfSubscribers = await storageTableProvider.GetListOfSubscribers();
        foreach (var item in listOfSubscribers)
        {
            try
            {
                await botClient.SendTextMessageAsync(item.ChatId, text,
                    replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        await botClient.SendTextMessageAsync(message!.Chat.Id, "Розіслано",
            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var res = callbackQuery.Data;
        
        var action = res.Split(' ')[0] switch
        {
            "DELETE_MEME" => DeleteConfirm(callbackQuery.Message, callbackQuery.Data, cancellationToken),
            "CONFIRM_DELETE_MEME" => this.storageTableProvider
                .DeleteMeme(
                    res.Replace("CONFIRM_DELETE_MEME ",
                        string.Empty,
                        StringComparison.InvariantCulture),
                    callbackQuery.Message.From.Id.ToString()),
            _ => Task.CompletedTask
        };

        await action;

        if (res.StartsWith("NOT_DELETE_MEME", StringComparison.InvariantCulture) && int.TryParse(res.Replace("NOT_DELETE_MEME ", string.Empty, StringComparison.InvariantCulture), out var messageId))
        {
            await botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, messageId, cancellationToken: cancellationToken);
        }

        if (res.StartsWith("SEND", StringComparison.InvariantCulture))
        {
            var textId = res.Replace("SEND ", string.Empty, StringComparison.InvariantCulture);
            await ConfirmSendToSubscribers(callbackQuery.Message, textId, cancellationToken);
        }

        if (res.StartsWith("CONFIRM_SEND_TO_SUBSCRIBERS", StringComparison.InvariantCulture))
        {
            var textId = res.Replace("CONFIRM_SEND_TO_SUBSCRIBERS ", string.Empty, StringComparison.InvariantCulture);
            await SendToSubscribers(callbackQuery.Message, textId, cancellationToken);
        }

        if (res.StartsWith("LIST_RECIPIENTS", StringComparison.InvariantCulture))
        {
            await LitOfSubscribers(callbackQuery.Message, cancellationToken);
        }

        logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);
    }

    #endregion
    
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}
