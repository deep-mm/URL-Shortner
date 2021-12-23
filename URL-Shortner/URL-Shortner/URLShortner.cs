using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace URL_Shortner
{
    public class URLObject
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string DecodedUrl { get; set; }
    }

    public class AddUrlObject
    {
        public string encodedUrl { get; set; }
        public string decodedUrl { get; set; }
    }

    public static class URLShortner
    {
        [FunctionName("urlShortner")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{url}")] HttpRequest req,
            [Table("DecodedURLs", "URL", "{url}", Connection = "AzureWebJobsStorage")] URLObject uRLObject,
            ILogger log, string url)
        {
            log.LogInformation("Decoding following url: " + url);

            if (uRLObject != null)
            {
                return new RedirectResult(uRLObject.DecodedUrl, true);
            }
            else
            {
                return new NotFoundResult();
            }
        }

        [FunctionName("addUrl")]
        public static async Task<IActionResult> AddUrl(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "addUrl")] HttpRequest req,
            [Table("DecodedURLs", "URL", Connection = "AzureWebJobsStorage")] IAsyncCollector<URLObject> urlTable,
            ILogger log)
        {
            log.LogInformation("Adding new url");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<AddUrlObject>(requestBody);

                if (input != null)
                {
                    log.LogInformation("Adding following url: " + input.decodedUrl);
                    var uRLObject = new URLObject();
                    uRLObject.PartitionKey = "URL";
                    uRLObject.RowKey = input.encodedUrl;
                    uRLObject.DecodedUrl = input.decodedUrl;
                    await urlTable.AddAsync(uRLObject);
                    return new OkObjectResult(uRLObject);
                }
                else
                {
                    return new BadRequestResult();
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(e);
            }
        }
    }
}
