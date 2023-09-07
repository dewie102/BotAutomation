using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BotAutomation_Website.Data;
using Microsoft.Extensions.Configuration.Yaml;
using Serilog;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.Database;
using AutomationUtilities;
using System.Text;

namespace BotAutomation_Website
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddYamlFile("config.yml")
                .Build();

            var builder = WebApplication.CreateBuilder(args);

            string? dbConnectionString = GetDBConnectionString(builder, config);
            if(dbConnectionString is null)
            {
                Log.Fatal("Could not find sutible database connection string, exiting...");
                return;
            }

            Secret<StaticCredentials>? credentials = VaultClientWrapper.GetDBCredentials(config).Result;
            if(credentials is null)
            {
                Log.Fatal("Could not fetch credentials from vault, exiting...");
                return;
            }
            
            StringBuilder conString = new(dbConnectionString);
            conString.Append($";user={credentials.Data.Username}");
            conString.Append($";password={credentials.Data.Password}");

            dbConnectionString = conString.ToString();

            builder.Services.AddDbContext<BotAutomation_WebsiteContext>(options =>
                options.UseSqlServer(dbConnectionString));

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if(!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        public static string? GetDBConnectionString(WebApplicationBuilder builder, IConfigurationRoot config)
        {
            string? dbConnectionString;

            // Try setting the connection string with config.yml
            dbConnectionString = config["servers:sql"];

            if(dbConnectionString is not null)
                return dbConnectionString;

            // If that didn't work then do the appsettings.json
            dbConnectionString = builder.Configuration.GetConnectionString("BotAutomation_WebsiteContext");

            return dbConnectionString;
        }
    }
}