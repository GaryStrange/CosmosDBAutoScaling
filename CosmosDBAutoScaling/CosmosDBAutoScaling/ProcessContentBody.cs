
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Diagnostics.Contracts;
using System;
using System.Text;

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

            StringBuilder errors = new StringBuilder();

            return isValid(webhookName, "WebhookName", ref errors)
                && isValid(name, "name", ref errors)
                && isValid(status, "status", ref errors)
                && isValid(metricName, "metricName", ref errors)
                ? (ActionResult)new OkObjectResult($"Request good.")
                : new BadRequestObjectResult(errors.ToString());
        }

        private static bool isValid(object o, string parameterName, ref StringBuilder sb)
        {
            bool result = false;
            if (o is null) sb.AppendLine($"{parameterName} not found.");
            else result = true;

            return result;
        }
    }
}
