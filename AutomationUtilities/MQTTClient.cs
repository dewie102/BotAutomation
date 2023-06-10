using MQTTnet;
using MQTTnet.Client;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;

namespace AutomationUtilities
{
    public class MQTTClient
    {
        public IMqttClient MqttClient { get; set; }

        public MQTTClient()
        {
            MqttFactory mqttFactory = new();
            MqttClient = mqttFactory.CreateMqttClient();
        }

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

            MQTTClient client = new();

            string? mqttServer = config["servers:mqtt"];

            if(mqttServer is null)
                return;

            await client.ConnectToMQTTServer(mqttServer);

            List<string>? topics = config.GetSection("mqttTopics").Get<List<string>>();

            if(topics is null)
                return;

            foreach(string topic in topics)
                await client.SubscribeToTopic(topic);


            Console.WriteLine("Hit 's' to send test message or any key to exit");
            string? input = Console.ReadLine();

            if(input == "s")
            {
                foreach(string topic in topics)
                {
                    await client.PublishMessage(topic, "TestFromC#Client");
                }
            }

            await client.DisconnectFromMQTTServer();
        }

        public async Task ConnectToMQTTServer(string mqttServer)
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
                await MqttClient.ConnectAsync(mqttClientOptions, timeout.Token);

                Console.WriteLine("The MQTT Client is connected");
            }
        }

        public async Task SubscribeToTopic(string topic)
        {
            MqttClient.ApplicationMessageReceivedAsync += e =>
            {
                Console.WriteLine("Received application message.");
                e.DumpToConsole();
                Console.WriteLine(e.ApplicationMessage);

                return Task.CompletedTask;
            };

            var response = await MqttClient.SubscribeAsync(topic);

            response.DumpToConsole();
        }

        public async Task PublishMessage(string topic, string message)
        {
            MqttApplicationMessage applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .Build();

            var response = await MqttClient.PublishAsync(applicationMessage);
            response.DumpToConsole();
        }

        public async Task DisconnectFromMQTTServer()
        {
            Console.WriteLine("Disconnecting from server...");
            await MqttClient.DisconnectAsync();
        }
    }
}
