
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using Microsoft.Azure.Services.AppAuthentication;
using System.Net.Http;
using Microsoft.Azure.KeyVault;
using System.Text;
using System.Collections.Generic;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDBAutoScaling
{
    public static class SecurelyScaleCosmosDB
    {
        private static string endpointSecretUrl = Environment.GetEnvironmentVariable("ProtectedEndpointUrl");
        private static string authKeySecretUrl = Environment.GetEnvironmentVariable("ProtectedAuthKeyUrl");

        private static double scaleFactory = 2.236;
        
        [FunctionName("SecurelyScaleCosmosDB")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("SecurelyScaleCosmosDB execution started.");

            RequestData requestData = new RequestData(req, log);
            StringBuilder validationMessages = new StringBuilder();
            if (requestData.isNotValid(ref validationMessages))
                throw new Exception(validationMessages.ToString());
            log.Info("Request body valid.");
            log.Info(requestData.Description);

            if (isNotValid(endpointSecretUrl, authKeySecretUrl))
                throw new Exception("Please check ProtectedEndpointUrl and ProtectedAuthKeyUrl variables are set in Application Settings");
            log.Info("Application Settings read without error.");

            SecretsStore secretsStore = new SecretsStore(log);

            string endPointUrl = secretsStore.GetSecret(endpointSecretUrl);
            string authKey = secretsStore.GetSecret(authKeySecretUrl);

            if (isNotValid(endPointUrl, authKey))
                throw new Exception("Information retrieved from Key Vault is invalid");
            log.Info("Sensitive data read without error.");

            Func<int, int> scaleUp = (ru)
                 =>
             {
                 double vulgarRu = Convert.ToDouble(ru) * SecurelyScaleCosmosDB.scaleFactory;
                 return Convert.ToInt32(Math.Round(vulgarRu, MidpointRounding.AwayFromZero));
             };
            Func<int, int> scaleDown = (ru)
                  =>
            {
                double vulgarRu = Convert.ToDouble(ru) / SecurelyScaleCosmosDB.scaleFactory;
                return Convert.ToInt32(Math.Round(vulgarRu, MidpointRounding.AwayFromZero));
            };

            bool scalingSucess = false;
            if (requestData.MetricName == "Total Request Units" && requestData.Status == "Activated")
            {
                log.Info("Total Request Units Activated Scale Up.");
                scalingSucess = CosmosDBScaler.ScaleAccount(endPointUrl, authKey, ru => scaleUp(ru), log);
            }
            else if (requestData.MetricName == "Total Request Units" && requestData.Status == "Resolved")
            {
                log.Info("Total Request Units Resolved Scale Down.");
                scalingSucess = CosmosDBScaler.ScaleAccount(endPointUrl, authKey, ru => scaleDown(ru), log);
            }
            return scalingSucess
                ? (ActionResult)new OkObjectResult($"CosmosDB account scaling sucessful. See Azure Function log for details.")
                : new BadRequestObjectResult("Scaling error. See Azure Function log for details.");
        }

        internal static class CosmosDBScaler
        {
            public static bool ScaleAccount(string endPointUrl, string authKey, Func<int,int> scaleExpression, TraceWriter log)
            {
                log.Info("Attempting to scale account.");

                var scaleUpTasks = new List<Task>();

                using (DocumentClient client = new DocumentClient(
                    new Uri(endPointUrl),
                        authKey,
                    new ConnectionPolicy
                    {
                        ConnectionMode = ConnectionMode.Direct,
                        ConnectionProtocol = Protocol.Tcp
                    })
                )
                {
                    IEnumerable<Database> dbList = client.CreateDatabaseQuery();
                    if (!dbList.Any()) log.Info("No databases found.");

                    foreach (Database db in dbList)
                    {
                        IEnumerable<DocumentCollection> collectionList = client.CreateDocumentCollectionQuery(db.SelfLink);
                        if (!collectionList.Any()) log.Info($"{db.Id}: No collections found.");

                        foreach (DocumentCollection collection in collectionList)
                        {
                            scaleUpTasks.Add(
                                ChangeThroughput(client, collection, (ru) => scaleExpression(ru))
                                    .ContinueWith(tsk =>
                                        log.Info($"{db.Id}: {collection.Id} scaled RU: {(tsk.Result as OfferV2).Content.OfferThroughput}")
                                        )
                                );
                        }
                    }

                }

                Task.WaitAll(scaleUpTasks.ToArray());

                return true;
            }

            private static Offer ReadOffer(DocumentClient client, DocumentCollection collection)
            {
                var response = client.ReadOffersFeedAsync().Result;
                Offer offer = response
                    .Single(o => o.ResourceLink == collection.SelfLink);

                return offer;
            }

            private static async Task<Offer> ReplaceOffer(DocumentClient client, Offer currentOffer, int newThroughputOffer)
            {
                //Potentially the new throughput may have been calculated using a factional multiplier.
                //CosmosDB throughputs must be specified in hundred units.
                newThroughputOffer = RoundSignaficantFigures(newThroughputOffer, 2);

                OfferV2 replacementOffer = new OfferV2(currentOffer, newThroughputOffer);

                return await client.ReplaceOfferAsync(replacementOffer);
            }

            private static int RoundSignaficantFigures(int number, int significantFigures)
            {
                Double scale = Math.Pow(10, significantFigures);
                return (int)(Math.Round(number / scale, 0, MidpointRounding.AwayFromZero) * scale);  
            }

            private static async Task<Offer> ChangeThroughput(DocumentClient client, DocumentCollection collection, Func<int, int> relativeThroughputChange)
            {
                var currentOffer = ReadOffer(client, collection);
                int currentOfferThoughput = (currentOffer as OfferV2).Content.OfferThroughput;

                return await ReplaceOffer(
                    client,
                    currentOffer,
                    relativeThroughputChange(currentOfferThoughput)
                    );
            }
        }

        internal class RequestData
        {
            dynamic data;
            public RequestData(HttpRequest req, TraceWriter log)
            {
                string requestBody = new StreamReader(req.Body).ReadToEnd();
                data = JsonConvert.DeserializeObject(requestBody);

                log.Info("Request read.");
            }
            private static bool isValid(object o, string attributeName, ref StringBuilder sb)
            {
                bool result = false;
                if (o is null) sb.AppendLine($"Unable to locate attribute: {attributeName}.");
                else result = true;

                return result;
            }

            public string Name => data?.context?.name;
            public string Description => data?.context?.description;
            public string Status => data?.status;
            public string MetricName => data?.context?.condition?.metricName;
            public string MetricValue => data?.context?.condition?.metricValue;

            public bool isValid(ref StringBuilder sb)
            {
                return RequestData.isValid(Status, "status", ref sb)
                    && RequestData.isValid(MetricName, "context\\condition\\metricName", ref sb)
                    && RequestData.isValid(MetricValue, "context\\condition\\metricValue", ref sb)
                    && RequestData.isValid(Name, "context\\name", ref sb)
                    && RequestData.isValid(Description, "context\\description", ref sb);
            }

            public bool isNotValid(ref StringBuilder sb) => !this.isValid(ref sb);

        }

        private static bool isNotValid(string endpointUrl, string authKeyUrl)
        =>
            endpointUrl is null ||
            endpointUrl == "Not Implemented Yet" ||
            authKeyUrl is null ||
            authKeyUrl == "Not Implemented Yet";

        internal class SecretsStore
        {
            private AzureServiceTokenProvider azureServiceTokenProvider;
            private static HttpClient client = new HttpClient();
            private KeyVaultClient kvClient;

            public SecretsStore(TraceWriter log)
            {
                azureServiceTokenProvider = new AzureServiceTokenProvider();
                log.Info("Azure Service Token Provider created.");

                kvClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback)
                    , client
                    );
                log.Info("Key Vault client created.");
            }

            public string GetSecret(string secretIdentifier)
            {
                return
                    kvClient.GetSecretAsync(secretIdentifier)
                    .Result
                    .Value;
            }
        }
 
    }
}
