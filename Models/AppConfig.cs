namespace EcoguardPoller.Models
{
    internal class AppConfig
    {
        public EcoGuardConfig EcoGuard { get; set; } = new();
        public MqttConfig MQTT { get; set; } = new();
        public DatabaseConfig Database { get; set; } = new();


    }
    public class EcoGuardConfig
    {
        public string DomainCode { get; set; } = "";
        public int NodeId { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class MqttConfig
    {
        public string Broker { get; set; } = "localhost";
        public int Port { get; set; } = 1883;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Topic { get; set; } = "ecoguard/consumption";
    }
    public class DatabaseConfig
    {
        public string Path { get; set; } = "/consumption.db";
    }

}
