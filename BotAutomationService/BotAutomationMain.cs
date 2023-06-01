using System.Data.SqlClient;

using AutomationUtilities.Models;

namespace BotAutomation
{
    public class BotAutomationMain
    {
        static void Main(string[] args)
        {
            List<Notice> notices = new();

            string connectionString = "Server=(localdb)\\mssqllocaldb;Database=BotAutomation_Website.Data;Trusted_Connection=True;MultipleActiveResultSets=true";
            SqlDataReader dataReader;
            string sql = "SELECT * FROM Notice WHERE Sent = 'False'";
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
                    Notice notice = new((int)dataReader["id"], dataReader["subject"].ToString(), dataReader["Message"].ToString(), dataReader["itemPath"].ToString(),
                                        (DateTime)dataReader["ScheduledTime"], (bool)dataReader["Sent"], (DateTime)dataReader["LastUpdated"]);

                    notices.Add(notice);


                }
                dataReader.Close();
                command.Dispose();
                connection.Close();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Can not open connection!\n{ex.Message}");
            }

            foreach(Notice notice in notices)
            {
                Console.WriteLine($"Notice: {notice.Id}\n\tSubject: {notice.Subject}\n\tMessage: {notice.Message}\n\tSent: {notice.Sent}");
            }
        }
    }
}