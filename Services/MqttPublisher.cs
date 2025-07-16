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

        public async Task PublishConsumptionAsync(double consumption, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"🔗 Connecting to MQTT Broker at {_config.Broker}:{_config.Port}...");

                var mqttFactory = new MqttClientFactory();
                using var mqttClient = mqttFactory.CreateMqttClient();
                var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.Broker, _config.Port)
                .WithCredentials(_config.Username, _config.Password)
                .WithClientId($"EcoGuardPoller-{Guid.NewGuid()}")
                .Build();

                await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);

                Console.WriteLine("✅ MQTT Connected!");

                var payload = consumption.ToString("F2");

                var message = new MqttApplicationMessage
                {
                    Topic = _config.Topic,
                    PayloadSegment = Encoding.UTF8.GetBytes(payload),
                    Retain = true,
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
                };

                await mqttClient.PublishAsync(message, cancellationToken);
                Console.WriteLine($"✅ Published {payload} kWh to MQTT topic '{_config.Topic}'");

                await mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
                Console.WriteLine("🔌 MQTT Disconnected.");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR during MQTT publish: {ex.GetType().Name} - {ex.Message}");
                throw; // Re-throw to handle in calling code if needed
            }
        }
    }
}
