using Telegram.Bot.Abstract;
using Telegram.Bot.Polling;

namespace Telegram.Bot.Services;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService : ReceiverServiceBase<IUpdateHandler>
{
    public ReceiverService(
        ITelegramBotClient botClient,
        IUpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<IUpdateHandler>> logger)
        : base(botClient, updateHandler, logger)
    {
    }
}
