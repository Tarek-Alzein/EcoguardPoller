using EcoguardPoller.Models;

using Microsoft.Extensions.Hosting;

namespace EcoguardPoller.Services
{
    internal class AppService : BackgroundService
    {
        private readonly AppConfig _config;
        private readonly EcoGuardApiClient _ecoGuard;
        private readonly MeterReadingStore _store;
        private readonly MqttPublisher _mqtt;

        public AppService(
    AppConfig config,
    EcoGuardApiClient ecoGuard,
    MeterReadingStore store,
    MqttPublisher mqtt)
        {
            _config = config;
            _ecoGuard = ecoGuard;
            _store = store;
            _mqtt = mqtt;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("✅ EcoGuard Poller Service started");

            try
            {
                // Step 1: Authenticate and get token
                Console.WriteLine("🔑 Getting OAuth token...");
                var token = await _ecoGuard.GetTokenAsync();

                // Step 2: Determine last hour window
                var (start, _) = EcoGuardApiClient.GetLastHourWindow();
                Console.WriteLine($"🕒 Fetching reading for time boundary: {start}");

                // Step 3: Get cumulative reading
                var cumulative = await _ecoGuard.GetLastValAsync(token, start);
                Console.WriteLine($"📈 Current cumulative reading: {cumulative} kWh");

                // Step 4: Load previous reading
                var previous = _store.GetLastReading();
                if (previous != null)
                {
                    var delta = cumulative - previous.Value;
                    Console.WriteLine($"✅ Delta (this hour's usage): {delta} kWh");

                    // Step 5: Publish to MQTT
                    await _mqtt.PublishConsumptionAsync(delta);
                }
                else
                {
                    Console.WriteLine("⚠️ No previous reading found - skipping delta calculation this time.");
                }

                // Step 6: Store new reading
                _store.SaveReading(start, cumulative);
                Console.WriteLine("💾 New cumulative reading saved.");

                Console.WriteLine("✅ EcoGuard Poller Service completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.GetType().Name} - {ex.Message}");
            }
        }
    }
}
