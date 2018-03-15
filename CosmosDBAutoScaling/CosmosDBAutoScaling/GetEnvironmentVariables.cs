
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;

namespace CosmosDBAutoScaling
{
    public static class GetEnvironmentVariables
    {
        static string endpointUrl = Environment.GetEnvironmentVariable("ProtectedEndpointUrl");
        static string authKeyUrl = Environment.GetEnvironmentVariable("ProtectedAuthKeyUrl");

        [FunctionName("GetEnvironmentVariables")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            log.Info($"ProtectedEndpointUrl {endpointUrl}");
            log.Info($"ProtectedAuthKeyUrl {authKeyUrl}");

            return (endpointUrl == null || authKeyUrl == null)
                ? new BadRequestObjectResult("Please check ProtectedEndpointUrl and ProtectedAuthKeyUrl variables are set in Application Settings")
                : (ActionResult)new OkObjectResult($"Response OK, check logs.");
        }
    }
}