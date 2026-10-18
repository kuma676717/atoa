using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using MigrationFortuneManual.Models;

namespace MigrationFortuneManual.Services;

public class S3StorageService
{
    private readonly string _bucketName;
    private readonly AmazonS3Client _s3Client;

    public S3StorageService()
    {
        _bucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME")
            ?? throw new InvalidOperationException("S3_BUCKET_NAME environment variable is not set.");
        _s3Client = new AmazonS3Client();
    }

    public async Task SaveAsync(string id, AssessmentResult result)
    {
        string json = JsonSerializer.Serialize(result);
        PutObjectRequest request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"assessments/{id}.json",
            ContentBody = json,
            ContentType = "application/json"
        };
        await _s3Client.PutObjectAsync(request);
    }

    public async Task<AssessmentResult?> GetAsync(string id)
    {
        GetObjectRequest request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = $"assessments/{id}.json"
        };

        try
        {
            using GetObjectResponse response = await _s3Client.GetObjectAsync(request);
            using StreamReader reader = new StreamReader(response.ResponseStream);
            string json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<AssessmentResult>(json);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey" || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
