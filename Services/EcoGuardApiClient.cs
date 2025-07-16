using EcoguardPoller.Models;

using System.Linq;
using System.Net;
using System.Net.Http.Json;

namespace EcoguardPoller.Services
{
    internal class EcoGuardApiClient(AppConfig appConfig)
    {
        private const string BaseUrl = "https://integration.ecoguard.se";

        private readonly EcoGuardConfig _config = appConfig.EcoGuard;
        private readonly HttpClient _client = new();

        private EcoGuardToken? _cachedToken;

        public async Task<EcoGuardToken> RequestNewTokenAsync()
        {
            var request = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = _config.Username,
                ["password"] = _config.Password,
                ["domain"] = _config.DomainCode
            };

            var response = await _client.PostAsync($"{BaseUrl}/token", new FormUrlEncodedContent(request));
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();

            if (result == null)
            {
                throw new Exception("Failed to parse token response from EcoGuard");
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var expiresAt = now + result.ExpiresIn - 60; // Buffer 1 min

            Console.WriteLine($"✅ Received new token, valid until {DateTimeOffset.FromUnixTimeSeconds(expiresAt)}");

            return new EcoGuardToken
            {
                AccessToken = result.AccessToken,
                ExpiresAt = expiresAt
            };
        }

        public async Task<string> GetTokenAsync(bool forceRefresh = false)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (!forceRefresh && _cachedToken != null && _cachedToken.ExpiresAt > now)
            {
                Console.WriteLine($"✅ Using cached EcoGuard token (expires at {DateTimeOffset.FromUnixTimeSeconds(_cachedToken.ExpiresAt)})");
                return _cachedToken.AccessToken;
            }

            // Otherwise fetch a new token
            Console.WriteLine("🔑 Requesting new EcoGuard token from server...");
            _cachedToken = await RequestNewTokenAsync();

            return _cachedToken.AccessToken;
        }


        public async Task<double> GetLastValAsync(string token, int from, CancellationToken cancellationToken)
        {
            var url = $"{BaseUrl}/api/{_config.DomainCode}/data?nodeid={_config.NodeId}&interval=H&utl=ELEC[lastval]&from={from}";

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _client.DefaultRequestHeaders.Add("x-version", "1");

            var response = await _client.GetAsync(url,cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("⚠️ EcoGuard returned 403 Unauthorized.");
                throw new UnauthorizedAccessException();
            }

            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<List<EcoGuardDeviceResult>>(cancellationToken);
            var allValues = data?.FirstOrDefault()?.Result.FirstOrDefault()?.Values;
            if (allValues == null || allValues.Count == 0)
                throw new Exception("No data returned from EcoGuard API.");

            return allValues.LastOrDefault()?.Value ?? throw new Exception("No values found in EcoGuard response.");
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
