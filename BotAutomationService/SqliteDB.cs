using Serilog;

using AutomationUtilities.Models;
using Microsoft.Data.Sqlite;

namespace BotAutomationService
{
    internal class SqliteDB : IDataAccess
    {
        SqliteConnection Connection { get; set; }

        public SqliteDB(string databaseFilePath, bool createIfNotExist = true) 
        {
            if(!Path.Exists(databaseFilePath) && !createIfNotExist)
            {
                Log.Fatal($"Datbase file does not exist: {databaseFilePath}");
                throw new FileNotFoundException($"Datbase file does not exist: {databaseFilePath}");
            }

            Connection = new SqliteConnection($"Data Source={databaseFilePath}");
        }

        public Notice GetNotice()
        {
            Notice test = new Notice();

            return test;
        }

        public void SaveNotice()
        {

        }
    }
}
