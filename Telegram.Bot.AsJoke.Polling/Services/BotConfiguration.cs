namespace Telegram.BotAsJoke.Polling.Services
{

#pragma warning disable CA1050 // Declare types in namespaces
#pragma warning disable RCS1110 // Declare type inside namespace.
    public class BotConfiguration
#pragma warning restore RCS1110 // Declare type inside namespace.
#pragma warning restore CA1050 // Declare types in namespaces
    {
        public static readonly string Configuration = "BotConfiguration";

        public static string BotToken { get; set; } = Environment.GetEnvironmentVariable("API_KEY") ?? string.Empty;
        public static string SaConnectionString { get; set; } = Environment.GetEnvironmentVariable("SESSION_STORAGE_CS") ?? string.Empty;
    }
}
