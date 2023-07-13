using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using System.Text;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

using AutomationUtilities;

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

            bool servicesInitialized = await InitializeServices();

            if(config == null || mqttClient == null || !servicesInitialized)
                return;

            List<(string? subject, string? message, string? itemPath)>?  noticesToSend = GetNoticesToSendFromDB();

            if(noticesToSend != null && noticesToSend.Count != 0)
                await SendNotices(noticesToSend);

            await mqttClient.DisconnectFromMQTTServer();
        }

        public static async Task<bool> InitializeServices()
        {
            SetupLogger();

            config = GetConfig();

            if(config == null)
                return false;

            return await ConfigureMQTT();
        }

        public static void SetupLogger()
        {
            Log.Logger = new LoggerConfiguration()
                            // Add console as logging target
                            .WriteTo.Console()
                            // Add a logging target for warning and higher logs
                            // structrued in JSON format
                            .WriteTo.File(new JsonFormatter(),
                                            "Logs/important.json",
                                            restrictedToMinimumLevel: LogEventLevel.Warning)
                            // Add a rolling file for all logs
                            .WriteTo.File("Logs/all-logs",
                                            rollingInterval: RollingInterval.Day)
                            // Add debug output window as logging target
                            .WriteTo.Debug()
                            // Set default minimum level
                            .MinimumLevel.Debug()
                            .CreateLogger();
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
            List<string>? topics = config.GetSection("mqttTopics").Get<List<string>>();

            if(mqttServer is null || topics is null)
                return false;

            if(mqttClient is null)
                mqttClient = new();

            try
            {
                await mqttClient.ConnectToMQTTServer(mqttServer);

                foreach(string topic in topics)
                    await mqttClient.SubscribeToTopic(topic);
            }
            catch(Exception ex)
            {
                //Console.WriteLine($"An exception has occurred:\n{ex.Message}");
                Log.Fatal($"An exception has occurred: {ex.Message}");
                return false;
            }

            return true;
        }

        public static List<(string? subject, string? message, string? itemPath)>? GetNoticesToSendFromDB()
        {
            List<(string? subject, string? message, string? itemPath)> noticesToSend = new();

            if(config is null)
            {
                Log.Error("Config is null while trying get get notices from the database");
                return null;
            }

            string? connectionString = config["servers:sql"];

            if(connectionString is null)
            {
                Log.Fatal("Connection string was not found in the config file! exiting");
                return null;
            }

            //Console.WriteLine($"Connection String: {connectionString}");
            Log.Debug($"Connection String: {connectionString}");

            // connectionString = "Server=(localdb)\\mssqllocaldb;Database=BotAutomation_Website.Data;Trusted_Connection=True;MultipleActiveResultSets=true";
            // connectionString = "Server=localhost\\SQLEXPRESS01;Database=BotAutomation_Website.Data;Trusted_Connection=True;MultipleActiveResultSets=true";
            string sql = @"
                SELECT *
                FROM [BotAutomation_Website.Data].dbo.Notice
                WHERE Sent = 0 AND
                ScheduledTime >= DATEADD(DAY, -1, GETDATE()) AND
                DATETRUNC(MINUTE, ScheduledTime) <= DATETRUNC(MINUTE, GETDATE());";
            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
                //Console.WriteLine("Database Connection Open!");
                Log.Information("Database Connection Open!");

                SqlCommand command = new(sql, connection);
                SqlDataReader dataReader = command.ExecuteReader();
                while(dataReader.Read())
                {
                    noticesToSend.Add((dataReader["subject"].ToString(), dataReader["Message"].ToString(), dataReader["itemPath"].ToString()));
                }
                dataReader.Close();
                command.Dispose();
            }
            catch(Exception ex)
            {
                //Console.WriteLine($"Can not open database connection!\n{ex.Message}");
                Log.Fatal($"Can not open database connection! {ex.Message}");
            }
            finally
            {
                connection.Close();
            }

            return noticesToSend;
        }

        public static async Task SendNotices(List<(string? subject, string? message, string? itemPath)> noticesToSend)
        {
            foreach((string? subject, string? message, string? itemPath) notice in noticesToSend)
            {
                await SendNoticeToBot(notice.subject, notice.message, notice.itemPath);
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

                string result = ProcessMessage(receivedMessage);

                if(result == "IGNORE")
                    continue;

                //Console.WriteLine(result);
                Log.Information(result);
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