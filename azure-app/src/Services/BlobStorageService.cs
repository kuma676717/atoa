using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MigrationFortune.Models;

namespace MigrationFortune.Services;

public class BlobStorageService
{
    private const string ContainerName = "assessments";
    private readonly BlobServiceClient _client;

    public BlobStorageService()
    {
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? throw new InvalidOperationException("AzureWebJobsStorage environment variable is not set.");
        _client = new BlobServiceClient(connectionString);
    }

    public async Task SaveAsync(string id, AssessmentResult result)
    {
        BlobContainerClient container = _client.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None);

        BlobClient blob = container.GetBlobClient($"{id}.json");
        string json = JsonSerializer.Serialize(result);
        await blob.UploadAsync(BinaryData.FromString(json), overwrite: true);
    }

    public async Task<AssessmentResult?> GetAsync(string id)
    {
        BlobContainerClient container = _client.GetBlobContainerClient(ContainerName);
        BlobClient blob = container.GetBlobClient($"{id}.json");

        try
        {
            Azure.Response<BlobDownloadResult> response = await blob.DownloadContentAsync();
            string json = response.Value.Content.ToString();
            return JsonSerializer.Deserialize<AssessmentResult>(json);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
        {
            return null;
        }
    }
}
