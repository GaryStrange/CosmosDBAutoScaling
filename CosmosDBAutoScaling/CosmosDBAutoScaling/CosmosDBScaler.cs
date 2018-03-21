using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDBAutoScaling
{
    public static class CosmosDBScaler
    {
        private static string endPointUrl = "https://workbench.documents.azure.com:443/";
        private static string authKey = "ezWnS1n99wkUstfhaWBdl0Ox0dPJAcdlMqIflwIdidwvzWTkb5Yl52Xy2J0oiC7UAYHKm7VrZ9glYirb9eFJmg==";

        [FunctionName("CosmosDBScaler")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

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

                foreach (Database db in client.CreateDatabaseQuery().ToList())
                {
                    foreach (DocumentCollection collection in client.CreateDocumentCollectionQuery(db.SelfLink))
                    {
                        scaleUpTasks.Add(
                            ChangeThroughput(client, collection, (ru) => ru + 100)
                                .ContinueWith(tsk =>
                                    log.Info(collection.Id + " scaled: " + (tsk.Result as OfferV2).Content.OfferThroughput)
                                    )
                            );
                    }
                }

            }

            Task.WaitAll(scaleUpTasks.ToArray());

            return endPointUrl is null
                ? new BadRequestObjectResult("EndPointUrl is null.")
                : (ActionResult)new OkObjectResult($"Response OK, check logs.");
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
           
            OfferV2 replacementOffer = new OfferV2(currentOffer, newThroughputOffer);

            return await client.ReplaceOfferAsync(replacementOffer);
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
}
