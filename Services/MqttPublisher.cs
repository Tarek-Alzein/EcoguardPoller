using EcoguardPoller.Models;

using MQTTnet;
using MQTTnet.Protocol;

using System.Text;

namespace EcoguardPoller.Services
{
    internal class MqttPublisher : IAsyncDisposable
    {
        private readonly MqttConfig _config;
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttClientOptions;

        public MqttPublisher(AppConfig appConfig)
        {
            _config = appConfig.MQTT;

            var mqttFactory = new MqttClientFactory();
            _mqttClient = mqttFactory.CreateMqttClient();
            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.Broker, _config.Port)
                .WithCredentials(_config.Username, _config.Password)
                .WithClientId($"EcoGuardPoller-{Guid.NewGuid()}")
                .Build();
        }

        public async Task PublishConsumptionAsync(double consumption, CancellationToken cancellationToken)
        {
            if (!_mqttClient.IsConnected)
            {
                Console.WriteLine("⚠️ MQTT Client not connected. Attempting to reconnect before publishing delta.");
                await ConnectAsync(cancellationToken);
                if (!_mqttClient.IsConnected)
                {
                    Console.WriteLine("❌ Failed to reconnect. Cannot publish delta message.");
                    return;
                }
            }

            try
            {
                var payload = consumption.ToString("F2");
                var message = new MqttApplicationMessage
                {
                    Topic = _config.ConsumptionTopic,
                    PayloadSegment = Encoding.UTF8.GetBytes(payload),
                    Retain = true,
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
                };

                await _mqttClient.PublishAsync(message, cancellationToken);
                Console.WriteLine($"✅ Published {payload} kWh (delta) to MQTT topic '{_config.ConsumptionTopic}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR during MQTT delta publish: {ex.GetType().Name} - {ex.Message}");
            }
        }

        public async Task PublishMeterTotalAsync(double cumulativeReading, CancellationToken cancellationToken)
        {
            if (!_mqttClient.IsConnected)
            {
                Console.WriteLine("⚠️ MQTT Client not connected. Attempting to reconnect before publishing cumulative.");
                await ConnectAsync(cancellationToken);
                if (!_mqttClient.IsConnected)
                {
                    Console.WriteLine("❌ Failed to reconnect. Cannot publish cumulative message.");
                    return;
                }
            }

            try
            {
                var payload = cumulativeReading.ToString("F2");
                var message = new MqttApplicationMessage
                {
                    Topic = _config.MeterTotalTopic,
                    PayloadSegment = Encoding.UTF8.GetBytes(payload),
                    Retain = true,
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
                };

                await _mqttClient.PublishAsync(message, cancellationToken);
                Console.WriteLine($"✅ Published {payload} kWh (cumulative) to MQTT topic '{_config.MeterTotalTopic}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR during MQTT cumulative publish: {ex.GetType().Name} - {ex.Message}");
            }
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient.IsConnected)
            {
                Console.WriteLine("ℹ️ MQTT Client already connected.");
                return;
            }

            try
            {
                Console.WriteLine($"🔗 Connecting to MQTT Broker at {_config.Broker}:{_config.Port}...");
                await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR connecting to MQTT Broker: {ex.GetType().Name} - {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_mqttClient.IsConnected)
            {
                Console.WriteLine("🔌 Disconnecting MQTT Client during shutdown.");
                await _mqttClient.DisconnectAsync();
            }
            _mqttClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
