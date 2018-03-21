
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace CosmosDBAutoScaling
{
    public static class ProcessContentBody
    {
        [FunctionName("ProcessContentBody")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string webhookName = data?.WebhookName;
            log.Info($"WebhookName {webhookName}");

            string name = data?.RequestBody?.context?.name;
            log.Info($"name {name}");
            string status = data?.RequestBody?.status;
            log.Info($"status2 {status}");
            string metricName = data?.RequestBody?.context?.condition?.metricName;
            log.Info($"metricName {metricName}");

            

            return webhookName != null
                ? (ActionResult)new OkObjectResult($"Hello, {webhookName}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
