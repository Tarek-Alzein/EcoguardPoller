using EcoguardPoller.Models;
using System.Net.Http.Json;

namespace EcoguardPoller.Services
{
    internal class EcoGuardApiClient
    {
        private readonly EcoGuardConfig _config;
        private readonly HttpClient _client = new();
        private const string BaseUrl = "https://integration.ecoguard.se";

        public EcoGuardApiClient(AppConfig appConfig)
        {
            _config = appConfig.EcoGuard;
        }

        public async Task<string> GetTokenAsync()
        {
            var data = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = _config.Username,
                ["password"] = _config.Password,
                ["domain"] = _config.DomainCode
            };

            var response = await _client.PostAsync($"{BaseUrl}/token", new FormUrlEncodedContent(data));
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            return json["access_token"]?.ToString() ?? throw new Exception("Token missing in response.");
        }

        public async Task<double> GetLastValAsync(string token, int from)
        {
            var url = $"{BaseUrl}/api/{_config.DomainCode}/data?nodeid={_config.NodeId}&interval=H&utl=ELEC[lastval]&from={from}";
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _client.DefaultRequestHeaders.Add("x-version", "1");

            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<List<EcoGuardDeviceResult>>();
            if (data == null || data.Count == 0)
                throw new Exception("No data returned from EcoGuard API.");

            // Search all Values for matching timestamp
            var allValues = data
                .SelectMany(d => d.Result)
                .Where(f => f.Func.Equals("lastval", StringComparison.OrdinalIgnoreCase))
                .SelectMany(f => f.Values)
                .ToList();

            Console.WriteLine($"Found {allValues.Count} values in response.");

            var match = allValues
                .Where(v => v.Time >= from)
                .OrderBy(v => v.Time)
                .FirstOrDefault();

            if (match != null)
            {
                Console.WriteLine($"Matched Value: {match.Value} at Time {match.Time}");
                return match.Value;
            }

            throw new Exception($"No reading found matching time {from}");
        }

        public static (int from, int to) GetLastHourWindow()
        {
            var now = DateTime.UtcNow;
            var end = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
            var from = end.AddHours(-1);

            return ((int)from.Subtract(DateTime.UnixEpoch).TotalSeconds,
                    (int)end.Subtract(DateTime.UnixEpoch).TotalSeconds);
        }
    }
}
