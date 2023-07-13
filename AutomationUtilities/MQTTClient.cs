using MQTTnet;
using MQTTnet.Client;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using MQTTnet.Packets;
using MQTTnet.Server;

using Serilog;

namespace AutomationUtilities
{
    public class MQTTClient
    {
        public IMqttClient MqttClient { get; set; }

        public Queue<MqttApplicationMessage> ReceivedMessages { get; set; }

        public MQTTClient()
        {
            MqttFactory mqttFactory = new();
            MqttClient = mqttFactory.CreateMqttClient();
            ReceivedMessages = new();
            Log.Debug("MqttClient created: {@client}", this);
        }

        /*static async Task Main(string[] args)
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
        }*/

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
                Log.Information("The MQTT Client is connected");
            }
        }

        public async Task SubscribeToTopic(string topic)
        {
            MqttClient.ApplicationMessageReceivedAsync += RecieveMessage;

            var response = await MqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .Build());

            response.DumpToConsole();
        }

        public async Task PublishMessage(string topic, string message, MqttUserProperty userProperty)
        {
            MqttApplicationMessage applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                // .WithUserProperty(userProperty.Name, userProperty.Value) Not currently supported until using MQTT 5.0.0
                .Build();

            var response = await MqttClient.PublishAsync(applicationMessage);
            //Console.WriteLine("Response to publish message:");
            //response.DumpToConsole();
        }

        public async Task DisconnectFromMQTTServer()
        {
            Console.WriteLine("Disconnecting from server...");
            await MqttClient.DisconnectAsync();
        }

        public Task RecieveMessage(MqttApplicationMessageReceivedEventArgs mqttEvent)
        {
            //Console.WriteLine("Received application message.");
            //mqttEvent.DumpToConsole();
            //Console.WriteLine(mqttEvent.ApplicationMessage);
            //Console.WriteLine($"payload converted:\n{mqttEvent.ApplicationMessage.ConvertPayloadToString()}");
            ReceivedMessages.Enqueue(mqttEvent.ApplicationMessage);

            return Task.CompletedTask;
        }
    }
}
