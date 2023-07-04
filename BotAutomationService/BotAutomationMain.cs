using System.Data.SqlClient;
using Newtonsoft.Json;

using AutomationUtilities.Models;
using AutomationUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using System.Text;

// Do I need these and can I do something without them? (Used in SendNoticeToBot)
using MQTTnet;
using MQTTnet.Packets;

namespace BotAutomation
{
    public class BotAutomationMain
    {
        static IConfigurationRoot? config;
        static MQTTClient? mqttClient;

        static async Task Main(string[] args)
        {
            //SendNoticeToBot("testSubject", "TestMessage", null);

            await InitializeServices();

            if(config == null || mqttClient == null)
                return;

            await GetNoticesToSendFromDB();

            await mqttClient.DisconnectFromMQTTServer();
        }

        public static async Task InitializeServices()
        {
            config = GetConfig();

            if(config == null)
                return;

            await ConfigureMQTT();
        }

        public static IConfigurationRoot? GetConfig()
        {
            // Make config from yml secondary to the secrets vault... someday

            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddYamlFile("config.yml")
                .Build();

            return config;
        }

        public static async Task<bool> ConfigureMQTT()
        {
            if(config == null)
                return false;

            string? mqttServer = config["servers:mqtt"];

            if(mqttServer is null)
                return false;

            if(mqttClient is null)
                mqttClient = new();

            await mqttClient.ConnectToMQTTServer(mqttServer);

            List<string>? topics = config.GetSection("mqttTopics").Get<List<string>>();

            if(topics is null)
                return false;

            foreach(string topic in topics)
                await mqttClient.SubscribeToTopic(topic);

            return true;
        }

        public static async Task GetNoticesToSendFromDB()
        {
            if(config is null)
                return;

            string? connectionString = config["servers:sql"];

            if(connectionString is null)
                return;

            Console.WriteLine($"Connection String: {connectionString}");

            //string connectionString = "Server=(localdb)\\mssqllocaldb;Database=BotAutomation_Website.Data;Trusted_Connection=True;MultipleActiveResultSets=true";
            //string connectionString = "Server=localhost\\SQLEXPRESS01;Database=BotAutomation_Website.Data;Trusted_Connection=True;MultipleActiveResultSets=true";
            SqlDataReader dataReader;
            string sql = @"
                SELECT *
                FROM [BotAutomation_Website.Data].dbo.Notice
                WHERE Sent = 0 AND
                ScheduledTime >= DATEADD(DAY, -1, GETDATE()) AND
                DATETRUNC(MINUTE, ScheduledTime) <= DATETRUNC(MINUTE, GETDATE());";
            SqlCommand command;
            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
                Console.WriteLine("Database Connection Open!");

                command = new SqlCommand(sql, connection);
                dataReader = command.ExecuteReader();
                while(dataReader.Read())
                {
                    // I feel like I should be doing this part in the main function? Somehow returning all results back and deciding when to send...
                    // MQTT Client can fail and causes it to exit out and run into this exception reporting database issues... NEED TO FIX (some day)
                    await SendNoticeToBot(dataReader["subject"].ToString(), dataReader["Message"].ToString(), dataReader["itemPath"].ToString());
                    await Task.Delay(10000);
                }
                dataReader.Close();
                command.Dispose();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Can not open database connection!\n{ex.Message}");
            }
            finally
            {
                connection.Close();
            }
        }

        public static async Task SendNoticeToBot(string? subject, string? message, string? itemPath)
        {
            if(config == null || mqttClient == null)
                return;

            string? group = config["mqttSettings:group"];
            string? password = config["mqttSettings:password"];

            if(group == null || password == null)
                return;

            Dictionary<string, string> test = new()
            {
                { "command", "notice" },
                { "group",  group },
                { "password",  password },
                { "action", "send" },
            };

            if(!string.IsNullOrEmpty(subject))
                test.Add("subject", subject);
            if(!string.IsNullOrEmpty(message))
                test.Add("message", message);
            if(!string.IsNullOrEmpty(itemPath))
                test.Add("item", itemPath);

            /*var json = JsonConvert.SerializeObject(test);
            Console.WriteLine(json);*/

            StringBuilder payload = new();
            foreach(KeyValuePair<string, string> kp in test)
            {
                payload.Append($"{kp.Key}={kp.Value}&");
            }
            // Removing the last '&' in the payload
            payload.Length--;

            //Console.WriteLine(payload.ToString());

            List<string>? topics = config.GetSection("mqttTopics").Get<List<string>>();

            if(topics is null)
                return;

            string topic = topics[0];

            await mqttClient.PublishMessage(topic, payload.ToString(), new MqttUserProperty("ID", "automation"));

            await Task.Delay(1000);

            while(mqttClient.ReceivedMessages.Count != 0)
            {
                MqttApplicationMessage receivedMessage = mqttClient.ReceivedMessages.Dequeue();

                // Not working due to MQTT Version
                /*if(ContinueToProcessMessage(receivedMessage))
                {
                    string result = ProcessMessage(receivedMessage);

                    Console.WriteLine(result);
                }*/

                string result = ProcessMessage(receivedMessage);

                if(result == "IGNORE")
                    continue;

                Console.WriteLine(result);
            }
        }

        public static bool ContinueToProcessMessage(MqttApplicationMessage message)
        {
            if(message.UserProperties != null)
            {
                foreach(MqttUserProperty userProperty in message.UserProperties)
                {
                    if(userProperty.Name == "ID" && userProperty.Value == "automation")
                        return false;
                }
            }

            return true;
        }

        public static string ProcessMessage(MqttApplicationMessage message)
        {
            string payload = message.ConvertPayloadToString();
            string[] payloadParameters = payload.Split("&");

            Dictionary<string, string> parametersAndValues = new();

            foreach(string parameter in payloadParameters)
            {
                string[] keyAndValue = parameter.Split("=");
                parametersAndValues.Add(keyAndValue[0], keyAndValue[1]);
            }

            if(parametersAndValues.ContainsKey("command") && parametersAndValues["command"] == "notice")
            {
                if(parametersAndValues.ContainsKey("action"))
                    return "IGNORE";

                if(parametersAndValues.ContainsKey("success") && parametersAndValues["success"] == "True")
                {
                    return "Success";
                }
                else
                {
                    return parametersAndValues.ContainsKey("error") ? parametersAndValues["error"] : "Not successful, error or unprocessed message";
                }
            }

            return "Unknown message and failed to process";
        }
    }
}