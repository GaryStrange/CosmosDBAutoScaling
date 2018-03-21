using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
//Add keyvault nuget: https://www.nuget.org/packages/Microsoft.Azure.KeyVault/
using Microsoft.Azure.KeyVault;
//Add nuget https://www.nuget.org/packages/Microsoft.Azure.Services.AppAuthentication
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http;

namespace CosmosDBAutoScaling
{
    public static class GetKeyVaultSecrets
    {
        private static HttpClient client = new HttpClient();

        static string endpointSecretUrl = "https://keystothekingdom.vault.azure.net/secrets/EndpointSecretUrl/ab7e3f66edef47f1b4a31fd5db979c7d";
        static string authKeySecretUrl = "https://keystothekingdom.vault.azure.net/secrets/WorkBenchCosmosDBMaster/d06735826ef9453bb4cb674ae97439b2";

        [FunctionName("GetKeyVaultSecrets")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            log.Info("Azure Service Token Provider created.");

            KeyVaultClient kvClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback)
                , client
                );
            log.Info("Key Vault client created.");

            string endPointUrl =
                kvClient.GetSecretAsync(endpointSecretUrl)
                .Result
                .Value;

            log.Info(endPointUrl);

            var authKey = kvClient.GetSecretAsync(authKeySecretUrl)
                .Result
                .Value;

            log.Info(authKey);

            return ValidateParameters(endPointUrl, authKey)
                ? new BadRequestObjectResult("Problems acessing endPointUrl or authkey secrets.")
                : (ActionResult)new OkObjectResult($"Response OK, check logs.");
        }

        private static bool ValidateParameters(string endPointUrl, string authKey)
        =>
            endPointUrl is null ||
            authKey is null;
    }
}
