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
        private static string SECRET_ID_URI_PATH = "/v1/auth/approle/role/extrabot_automation/secret-id";

        public string VaultServerHostAndPort { get; set; }
        private IVaultClient? VaultClient { get; set; } = null;


        public VaultClientWrapper(string vaultServerHostAndPort) {
            this.VaultServerHostAndPort = vaultServerHostAndPort;
        }

        public async Task<string> GetSecretIDFromVault() {
            string result = "";

            HttpClient client = new HttpClient();
            client.BaseAddress = GetSecretIDUri();
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

            if(VaultClient == null)  {
                throw new ArgumentNullException(nameof(VaultClient));
            }

            Secret<StaticCredentials> creds = await VaultClient.V1.Secrets.Database.GetStaticCredentialsAsync(roleName);

            return creds;
        }

        private void SetVaultClientWithAppRole(string roleID, string secretID) {
            IAuthMethodInfo authMethod = new AppRoleAuthMethodInfo(roleID, secretID);
            VaultClientSettings vaultClientSettings = new VaultClientSettings(
                $"http://{VaultServerHostAndPort}", authMethod);
            VaultClient = new VaultClient(vaultClientSettings);

            VaultClient.Settings.UseVaultTokenHeaderInsteadOfAuthorizationHeader = true;
        }

        private Uri GetSecretIDUri()
        {
            string[] hostAndPort = VaultServerHostAndPort.Split(':');
            string host = hostAndPort[0];
            int port = -1;
            bool succeeded = int.TryParse(hostAndPort[1], out port);

            if(!succeeded)
                throw new FormatException($"Value after host is not a valid int: {hostAndPort[1]}");

            UriBuilder builder = new UriBuilder();
            builder.Host = host;
            builder.Port = port;
            builder.Path = SECRET_ID_URI_PATH;
            builder.Scheme = "http";

            return builder.Uri;
        }
    }
}
