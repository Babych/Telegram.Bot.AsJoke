using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Services;
using Telegram.Bot.Types;
using Telegram.BotAsJoke.Polling;
using Telegram.BotAsJoke.Polling.Services;
using Telegram.BotAsJoke.Polling.Storage;

Log.Instance.Trace("App started");

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register Bot configuration
        services.Configure<BotConfiguration>(
            context.Configuration.GetSection(BotConfiguration.Configuration));

        // Register named HttpClient to benefits from IHttpClientFactory
        // and consume it with ITelegramBotClient typed client.
        // More read:
        //  https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#typed-clients
        //  https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>(httpClient =>
                {
                    TelegramBotClientOptions options = new(BotConfiguration.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });

        services.AddScoped<StorageTableProvider>();
        services.AddScoped<IUpdateHandler, BotUpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
    })
    .Build();

var botClient = host.Services.GetRequiredService<ITelegramBotClient>();

var commands = new BotCommand[]
{
    new() { Command = "random_meme", Description = "Випадковий мем." },
    new() { Command = "subscribe", Description = "Підписатися на розсилку." },
    new() { Command = "unsubscribe", Description = "Відписатися від розсилки." },
    new() { Command = "start", Description = "З чого почати." }
};

await botClient.SetMyCommandsAsync(commands);

await host.RunAsync();

