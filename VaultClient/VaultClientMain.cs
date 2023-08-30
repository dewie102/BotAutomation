using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.Database;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Newtonsoft.Json.Linq;

namespace VaultClientApplication
{
    internal class VaultClientMain
    {
        static async Task Main(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddYamlFile("config.yml")
                .Build();

            if(config is null)
                return;

            string? vaultServer = config["servers:vault"];
            if(vaultServer is null)
                return;

            VaultClientWrapper vault = new VaultClientWrapper(vaultServer);
            string secretID = await vault.GetSecretID();

            string? roleID = config["roleID"];
            if(roleID is null)
                return;

            Secret<StaticCredentials> creds = await vault.getStaticCredentials("ExtrabotAutomation", roleID, secretID);

            Console.WriteLine(creds.Data.Username);
            Console.WriteLine(creds.Data.Password);
            
        }
    }
}