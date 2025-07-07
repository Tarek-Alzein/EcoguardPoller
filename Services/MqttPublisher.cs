using EcoguardPoller.Models;

using MQTTnet;
using MQTTnet.Protocol;

using System.Text;

namespace EcoguardPoller.Services
{
    internal class MqttPublisher
    {
        private readonly MqttConfig _config;

        public MqttPublisher(AppConfig appConfig) => _config = appConfig.MQTT;

        public async Task PublishConsumptionAsync(double consumption)
        {
            Console.WriteLine($"🔗 Connecting to MQTT Broker at {_config.Broker}:{_config.Port}...");

            var mqttFactory = new MqttClientFactory();
            using var mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_config.Broker, _config.Port)
            .WithCredentials(_config.Username, _config.Password)
            .Build();

            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("✅ MQTT Connected!");

            var payload = consumption.ToString("F2");

            var message = new MqttApplicationMessage
            {
                Topic = _config.Topic,
                PayloadSegment = Encoding.UTF8.GetBytes(payload),
                Retain = true,
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };

            await mqttClient.PublishAsync(message);
            Console.WriteLine($"✅ Published {payload} kWh to MQTT topic '{_config.Topic}'");

            await mqttClient.DisconnectAsync();
            Console.WriteLine("🔌 MQTT Disconnected.");
        }
    }
}
