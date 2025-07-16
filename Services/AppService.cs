using EcoguardPoller.Models;

using Microsoft.Extensions.Hosting;

namespace EcoguardPoller.Services
{
    internal class AppService : BackgroundService
    {
        private readonly AppConfig _appConfig;
        private readonly EcoGuardApiClient _ecoGuard;
        private readonly MeterReadingStore _store;
        private readonly MqttPublisher _mqtt;

        public AppService(
            AppConfig config,
    EcoGuardApiClient ecoGuard,
    MeterReadingStore store,
    MqttPublisher mqtt)
        {
            _appConfig = config;
            _ecoGuard = ecoGuard;
            _store = store;
            _mqtt = mqtt;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("✅ EcoGuard Poller Service started and waiting for polling interval.");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"🕒 Polling job started at {DateTime.Now}");

                    await RunPollOnceAsync(cancellationToken);

                    Console.WriteLine($"✅ Polling job completed at {DateTime.Now}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ERROR during polling cycle: {ex.GetType().Name} - {ex.Message}");
                }

                // Wait for next interval (1 hour)
                Console.WriteLine($"⏳ Sleeping for {_appConfig.PollIntervalHours} hour until next poll...");
                try
                {
                    await Task.Delay(TimeSpan.FromHours(_appConfig.PollIntervalHours), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // Stop was requested while sleeping
                    break;
                }
            }

            Console.WriteLine("🛑 EcoGuard Poller Service is stopping due to cancellation request.");
        }

        private async Task RunPollOnceAsync(CancellationToken stoppingToken)
        {
            var (start, _) = EcoGuardApiClient.GetLastHourWindow();
            Console.WriteLine($"📌 Requesting reading for time boundary: {start}");

            string token;

            try
            {
                // First attempt with cached token
                token = await _ecoGuard.GetTokenAsync();
                await FetchAndStoreReading(token, start, stoppingToken);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("⚠️ Unauthorized! Token expired or invalid. Refreshing and retrying...");

                // Refresh token and retry once
                token = await _ecoGuard.GetTokenAsync(forceRefresh: true);

                try
                {
                    await FetchAndStoreReading(token, start, stoppingToken);
                }
                catch (UnauthorizedAccessException) 
                {
                    Console.WriteLine("❌ Still getting Unauthorized after refreshing token. Giving up this cycle.");
                }
            }
        }

        private async Task FetchAndStoreReading(string token, int from, CancellationToken cancellationToken)
        {
            // Get cumulative reading from EcoGuard
            var cumulative = await _ecoGuard.GetLastValAsync(token, from, cancellationToken);
            Console.WriteLine($"📈 Current cumulative reading: {cumulative} kWh");

            // Load previous reading from SQLite
            var previous = _store.GetLastReading();

            if (previous != null)
            {
                if (Math.Abs(cumulative - previous.Value) < 0.0001)
                {
                    Console.WriteLine("ℹ️ Reading has not changed since last poll.");
                    return;
                }

                var delta = cumulative - previous.Value;
                Console.WriteLine($"✅ Calculated delta for this hour: {delta} kWh");

                // Publish delta to MQTT
                await _mqtt.PublishConsumptionAsync(delta, cancellationToken);
            }
            else
            {
                Console.WriteLine("⚠️ No previous reading found in DB. Skipping delta calculation this time.");
            }

            // Save new cumulative reading
            _store.SaveReading(from, cumulative);
            Console.WriteLine("💾 New cumulative reading saved to SQLite.");
        }

    }
}
