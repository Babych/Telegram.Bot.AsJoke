using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Telegram.BotAsJoke.Polling
{
    internal class Log
    {
        private static Log? instance;
        public static Log Instance
        {
            get { return instance ??= new Log(); }
        }

        private readonly TelemetryClient telemetryClient;

        [Obsolete]
        public Log()
        {
            try
            {
                string instrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY") ?? string.Empty;

                telemetryClient = new TelemetryClient(new TelemetryConfiguration(instrumentationKey));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        public void Trace(string message, Dictionary<string, string> properties = null)
        {
            try
            {
                telemetryClient.TrackTrace(message, properties);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void TrackException(Exception exception, string eventName)
        {
            try
            {
                telemetryClient.TrackException(exception,
                    new Dictionary<string, string>() { { nameof(eventName), eventName } });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
