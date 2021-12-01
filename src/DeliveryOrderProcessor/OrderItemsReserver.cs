using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace FunctionApp;

public static class OrderItemsReserver
{
    [FunctionName("OrderItemsReserver")]
    public static async Task Run(
        [ServiceBusTrigger("eshoporders", Connection = "ServiceBusConnectionString")] string myQueueItem,
        ILogger log,
        ExecutionContext context)
    {
        log.LogInformation($"C# ServiceBus queue trigger function processed message");

        try
        {
            await AddReservation(myQueueItem, context);
        }
        catch
        {
            await MailNotify(myQueueItem, context);
        }
    }

    private static async Task AddReservation(string reservation, ExecutionContext context)
    {
        var container = await GetBlobContainer(context);

        var blob = container.GetBlockBlobReference($"{DateTime.Now:yyyy-MM-dd-mm-ss}.json");
        blob.Properties.ContentType = "application/json";

        await blob.UploadTextAsync(reservation);

        await blob.SetPropertiesAsync();
    }

    private static async Task<CloudBlobContainer> GetBlobContainer(ExecutionContext context)
    {
        var config = GetConfiguration(context);

        var storageAccount = CloudStorageAccount.Parse(config["AzureWebJobsStorage"]);
        var blobClient = storageAccount.CreateCloudBlobClient();

        var blobContainer = blobClient.GetContainerReference("reserved");
        await blobContainer.CreateIfNotExistsAsync();

        return blobContainer;
    }

    private static IConfigurationRoot GetConfiguration(ExecutionContext context)
    {
        return new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", true, true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static async Task MailNotify(string myQueueItem, ExecutionContext context)
    {
        var emailUrl = GetConfiguration(context)["EmailUrl"];

        using var client = new HttpClient();
        await client.PostAsync(emailUrl, new StringContent(myQueueItem, Encoding.UTF8, "application/json"));
    }
}
