using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;

namespace MQTTClientApplication
{
    internal class MQTTClientMain
    {
        static async Task Main(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddYamlFile("config.yml")
                .Build();

            await temp(config);
        }

        static async Task temp(IConfigurationRoot config)
        {
            if(config == null)
                return;

            MqttFactory mqttFactory = new();
            IMqttClient mqttClient = mqttFactory.CreateMqttClient();

            string? mqttServer = config["servers:mqtt"];

            if(mqttServer is null)
                return;

            await ConnectToMQTTServer(mqttClient, mqttServer);

            List<string>? topics = config.GetSection("mqttTopics").Get<List<string>>();

            if(topics is null)
                return;

            foreach(string topic in topics)
                await SubscribeToTopic(mqttClient, topic);


            Console.WriteLine("Hit 's' to send test message or any key to exit");
            string? input = Console.ReadLine();

            if(input == "s")
            {
                foreach(string topic in topics)
                {
                    await PublishMessage(mqttClient, topic, "TestFromC#Client");
                }
            }

            await mqttClient.DisconnectAsync();
        }

        public static async Task ConnectToMQTTServer(IMqttClient mqttClient, string mqttServer)
        {
            MqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(mqttServer)
                .WithTls(
                    o =>
                    {
                        o.CertificateValidationHandler = _ => true;
                    })
                .Build();

            using(CancellationTokenSource timeout = new(5000))
            {
                try
                {
                    await mqttClient.ConnectAsync(mqttClientOptions, timeout.Token);

                    Console.WriteLine("The MQTT Client is connected");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Failed to connect to MQTT Broker for the following reason:\n{ex.Message}");
                }
            }
        }

        public static async Task SubscribeToTopic(IMqttClient mqttClient, string topic)
        {
            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                Console.WriteLine("Received application message.");
                e.DumpToConsole();
                Console.WriteLine(e.ApplicationMessage);

                return Task.CompletedTask;
            };

            var response = await mqttClient.SubscribeAsync(topic);

            response.DumpToConsole();
        }

        public static async Task PublishMessage(IMqttClient mqttClient, string topic, string message)
        {
            MqttApplicationMessage applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .Build();

            var response = await mqttClient.PublishAsync(applicationMessage);
            response.DumpToConsole();
        }
    }

    static class ObjectExtenstions
    {
        public static TObject DumpToConsole<TObject>(this TObject @object)
        {
            var output = "NULL";
            if(@object != null)
            {
                output = JsonSerializer.Serialize(@object, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }

            Console.WriteLine($"[{@object?.GetType().Name}]:\r\n{output}");
            return @object;
        }
    }
}