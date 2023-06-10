using System.Data.SqlClient;
using Newtonsoft.Json;

using AutomationUtilities.Models;
using AutomationUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;

namespace BotAutomation
{
    public class BotAutomationMain
    {
        static async Task Main(string[] args)
        {
            MQTTClient mqttClient = new MQTTClient();

            SendNoticeToBot("testSubject", "TestMessage", null);

            //await InitializeServices(mqttClient);

            //await mqttClient.DisconnectFromMQTTServer();
        }

        public static async Task<bool> InitializeServices(MQTTClient mqttClient)
        {
            IConfigurationRoot? config = GetConfig();

            if(config == null)
                return false;

            bool success = await Task.Run(() => ConfigureMQTT(config, mqttClient));

            Console.WriteLine($"{success}");
            
            if(!success)
                return false;



            return true;
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

        public static async Task<bool> ConfigureMQTT(IConfigurationRoot config, MQTTClient mqttClient)
        {
            string? mqttServer = config["servers:mqtt"];

            if(mqttServer is null)
                return false;

            await mqttClient.ConnectToMQTTServer(mqttServer);

            List<string>? topics = config.GetSection("mqttTopics").Get<List<string>>();

            if(topics is null)
                return false;

            foreach(string topic in topics)
                await mqttClient.SubscribeToTopic(topic);

            return true;
        }

        public static void GetNoticesToSendFromDB(IConfigurationRoot config)
        {
            if(config is null)
                return;

            string? connectionString = config["servers:sql"];

            if(connectionString is null)
                return;

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
                Console.WriteLine("Connection Open!");

                command = new SqlCommand(sql, connection);
                dataReader = command.ExecuteReader();
                while(dataReader.Read())
                {
                    // I feel like I should be doing this part in the main function? Somehow returning all results back and deciding when to send...
                    SendNoticeToBot(dataReader["subject"].ToString(), dataReader["Message"].ToString(), dataReader["itemPath"].ToString());
                }
                dataReader.Close();
                command.Dispose();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Can not open connection!\n{ex.Message}");
            }
            finally
            {
                connection.Close();
            }
        }

        public static void SendNoticeToBot(string? subject, string? message, string? itemPath)
        {
            Dictionary<string, string> test = new()
            {
                { "command", "notice" },
                { "group", "test" },
                { "password", "testpw" },
                { "action", "send" },
            };

            if(!string.IsNullOrEmpty(subject))
                test.Add("subject", subject);
            if(!string.IsNullOrEmpty(message))
                test.Add("message", message);
            if(!string.IsNullOrEmpty(itemPath))
                test.Add("item", itemPath);

            var json = JsonConvert.SerializeObject(test);
            Console.WriteLine(json);


        }
    }
}