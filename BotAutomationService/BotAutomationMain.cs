using System.Data.SqlClient;
using Newtonsoft.Json;

using AutomationUtilities.Models;
using AutomationUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using System.Text;

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
                    await SendNoticeToBot(dataReader["subject"].ToString(), dataReader["Message"].ToString(), dataReader["itemPath"].ToString());
                    //await Task.Delay(5000);
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

            Console.WriteLine(payload.ToString());

            List<string>? topics = config.GetSection("mqttTopics").Get<List<string>>();

            if(topics is null)
                return;

            string topic = topics[0];

            await mqttClient.PublishMessage(topic, payload.ToString());
        }
    }
}