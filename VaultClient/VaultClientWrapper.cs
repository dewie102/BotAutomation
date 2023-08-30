using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.Database;

using Newtonsoft.Json.Linq;

namespace VaultClientApplication
{
    internal class VaultClientWrapper {
        public string VaultServer { get; set; }
        private Uri? VaultSecretIDUri { get; set; } = null;
        private IVaultClient? VaultClient { get; set; } = null;


        public VaultClientWrapper(string vaultServer) {
            this.VaultServer = vaultServer;
            SetSecretIDUri();
        }

        public async Task<string> GetSecretID() {
            string result = "";

            HttpClient client = new HttpClient();
            client.BaseAddress = VaultSecretIDUri;
            HttpResponseMessage response = await client.PostAsync(client.BaseAddress, null);
            HttpContent responseContent = response.Content;

            JObject jsonResponse = await getResponseAsJson(responseContent);

            JToken? responseData = getToken(jsonResponse, "data");
            if(responseData is null)
                throw new KeyNotFoundException("'data' not found in JSON Response.");

            JToken? secretIDToken = getToken((JObject)responseData, "secret_id");
            if(secretIDToken is null)
                throw new KeyNotFoundException("'secret_id' not found in JSON Response.");

            result = secretIDToken.ToString();

            return result;
        }

        private async Task<JObject> getResponseAsJson(HttpContent content) {
            JObject response = JObject.Parse(await content.ReadAsStringAsync());

            return response;
        }

        private JToken? getToken(JObject jsonObject, string propertyName)
        {
            JToken? token = jsonObject.GetValue(propertyName);

            return token;
        }

        public async Task<Secret<StaticCredentials>> getStaticCredentials(string roleName, string roleID, string secretID) {
            SetVaultClientWithAppRole(roleID, secretID);

            Secret<StaticCredentials> creds = await VaultClient.V1.Secrets.Database.GetStaticCredentialsAsync(roleName);

            return creds;
        }

        private void SetVaultClientWithAppRole(string roleID, string secretID) {
            IAuthMethodInfo authMethod = new AppRoleAuthMethodInfo(roleID, secretID);
            VaultClientSettings vaultClientSettings = new VaultClientSettings($"http://{VaultSecretIDUri.GetComponents(UriComponents.HostAndPort, UriFormat.Unescaped)}", authMethod);
            VaultClient = new VaultClient(vaultClientSettings);

            VaultClient.Settings.UseVaultTokenHeaderInsteadOfAuthorizationHeader = true;
        }

        private void SetSecretIDUri()
        {
            VaultSecretIDUri = new Uri(
                $"http://{VaultServer}/v1/auth/approle/role/extrabot_automation/secret-id");
        }
    }
}
