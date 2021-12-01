using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionApp;

public static class DeliveryOrderProcessor
{
    [FunctionName("DeliveryOrderProcessor")]
    public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(databaseName: "Orders", collectionName: "eshop-base-container", PartitionKey = "/orderId", ConnectionStringSetting = "CosmosDBConnection")]IAsyncCollector<dynamic> documentsOut)
    {
        var name = req.Query["name"];
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<object>(requestBody);
        // Add a JSON document to the output container.
        await documentsOut.AddAsync(new
        {
            // create a random ID
            orderId = System.Guid.NewGuid().ToString(),
            data = data,
        });
        var responseMessage = string.IsNullOrEmpty(name)
                                     ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                                     : $"Hello, {name}. This HTTP triggered function executed successfully.";
        return new OkObjectResult(responseMessage);
    }
}
