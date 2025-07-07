using System.Text.Json.Serialization;

namespace EcoguardPoller.Models
{
    internal class EcoGuardToken
    {
        public string AccessToken { get; set; } = "";
        public long ExpiresAt { get; set; }  // Unix timestamp

    }
    internal class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "bearer";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

}
