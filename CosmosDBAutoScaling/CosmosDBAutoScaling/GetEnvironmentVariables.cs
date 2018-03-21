using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;



namespace CosmosDBAutoScaling
{
    public static class GetEnvironmentVariables
    {
        private static string endpointUrl = Environment.GetEnvironmentVariable("ProtectedEndpointUrl");
        private static string authKeyUrl = Environment.GetEnvironmentVariable("ProtectedAuthKeyUrl");

        [FunctionName("GetEnvironmentVariables")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            log.Info($"ProtectedEndpointUrl {endpointUrl}");
            log.Info($"ProtectedAuthKeyUrl {authKeyUrl}");

            return ValidateParameters(endpointUrl, authKeyUrl)
                ? new BadRequestObjectResult("Please check ProtectedEndpointUrl and ProtectedAuthKeyUrl variables are set in Application Settings")
                : (ActionResult)new OkObjectResult($"Response OK, check logs.");
        }

        private static bool ValidateParameters(string endpointUrl, string authKeyUrl)
        =>
            endpointUrl is null ||
            endpointUrl == "Not Implemented Yet" ||
            authKeyUrl is null ||
            authKeyUrl == "Not Implemented Yet";
    }
}
