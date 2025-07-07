namespace EcoguardPoller.Models
{
    internal class AppConfig
    {
        public required EcoGuardConfig EcoGuard { get; set; }
        public required MqttConfig MQTT { get; set; }
        public required DatabaseConfig Database { get; set; }
        public double PollIntervalHours { get; set; } = 1;
    }
    public class EcoGuardConfig
    {
        public required string DomainCode { get; set; }
        public required int NodeId { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class MqttConfig
    {
        public required string Broker { get; set; }
        public int Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public required string Topic { get; set; }
    }
    public class DatabaseConfig
    {
        public required string Path { get; set; }
    }

}
