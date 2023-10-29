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
        
        public Log()
        {
            try
            {
                string instrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");

                telemetryClient = new TelemetryClient(new TelemetryConfiguration(instrumentationKey));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        public void Trace(string message)
        {
            try
            {
                telemetryClient.TrackTrace(message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
