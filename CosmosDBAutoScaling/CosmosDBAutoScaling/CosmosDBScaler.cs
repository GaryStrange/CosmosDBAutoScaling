
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Linq;

namespace CosmosDBAutoScaling
{
    public static class CosmosDBScaler
    {
        private static string endPointUrl = "https://workbench.documents.azure.com:443/";
        private static string authKey = "ezWnS1n99wkUstfhaWBdl0Ox0dPJAcdlMqIflwIdidwvzWTkb5Yl52Xy2J0oiC7UAYHKm7VrZ9glYirb9eFJmg==";
        private static string databaseName = "ToDoList";



        [FunctionName("CosmosDBScaler")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string name;

            using (DocumentClient client = new DocumentClient(
                        new Uri(endPointUrl),
                        authKey,
                        new ConnectionPolicy
                        {
                            ConnectionMode = ConnectionMode.Direct,
                            ConnectionProtocol = Protocol.Tcp
                        }))
            {
                log.Info("DocumentClient open!");

                var collections = client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(databaseName)).ToList();

                foreach (DocumentCollection collection in collections)
                {
                    log.Info(collection.Id);
                }

                name = collections.Count.ToString();
            };


            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
