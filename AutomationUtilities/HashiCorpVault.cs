using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.Database;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Newtonsoft.Json.Linq;

namespace AutomationUtilities
{
    public class HashiCorpVault
    {
        public async Task Init()
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

            var client = new HttpClient();
            client.BaseAddress = new Uri($"http://{vaultServer}/v1/auth/approle/role/extrabot_automation/secret-id");
            HttpResponseMessage response = await client.PostAsync(client.BaseAddress, null);
            HttpContent responseContent = response.Content;

            JObject responseAsJson = JObject.Parse(await responseContent.ReadAsStringAsync());
            if(responseContent is null)
                return;

            JToken? responseData = responseAsJson.GetValue("data");
            if(responseData is null)
                return;

            JToken? secretIDToken = responseData["secret_id"];
            if(secretIDToken is null)
                return;

            string secretID = secretIDToken.ToString();

            string? roleID = config["roleID"];
            if(roleID is null)
                return;

            IAuthMethodInfo authMethod = new AppRoleAuthMethodInfo(roleID, secretID);
            VaultClientSettings vaultClientSettings = new VaultClientSettings($"http://{vaultServer}", authMethod);
            IVaultClient vaultClient = new VaultClient(vaultClientSettings);

            vaultClient.Settings.UseVaultTokenHeaderInsteadOfAuthorizationHeader = true;

            Secret<StaticCredentials> creds = await vaultClient.V1.Secrets.Database.GetStaticCredentialsAsync("ExtrabotAutomation");

            Console.WriteLine(creds.Data.Username);
            Console.WriteLine(creds.Data.Password);
        }
    }
}