using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MigrationFortuneManual.Models;
using MigrationFortuneManual.Services;

[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MigrationFortuneManual.Functions;

public class AssessFunction
{
    private readonly FortuneEngine _fortune;
    private readonly S3StorageService _s3;

    public AssessFunction()
    {
        _fortune = new FortuneEngine();
        _s3 = new S3StorageService();
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> HandleAsync(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        // HTTP/2 headers are lowercase in API Gateway v2
        string? seedHeader = null;
        request.Headers?.TryGetValue("x-force-seed", out seedHeader);
        bool hasSeed = seedHeader != null && int.TryParse(seedHeader, out int seedVal);
        Random rng = hasSeed ? new Random(int.Parse(seedHeader!)) : Random.Shared;

        // Handle base64-encoded body (API Gateway may base64-encode binary payloads)
        string body = string.Empty;
        if (!string.IsNullOrEmpty(request.Body))
        {
            body = request.IsBase64Encoded
                ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body))
                : request.Body;
        }

        // Deserialize request body
        AssessRequest? input;
        try
        {
            input = JsonSerializer.Deserialize<AssessRequest>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            input = null;
        }

        // Validation
        if (input == null)
            return BadRequest("invalid request body");

        if (string.IsNullOrWhiteSpace(input.WorkloadName))
            return BadRequest("workloadName is required");

        if (string.IsNullOrWhiteSpace(input.Description))
            return BadRequest("description is required");

        if (input.YearsInProduction.HasValue && input.YearsInProduction.Value < 0)
            return BadRequest("yearsInProduction must be >= 0");

        // Assess and persist
        AssessmentResult result = _fortune.Assess(input, "aws-lambda-manual", rng);
        await _s3.SaveAsync(result.AssessmentId, result);

        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(result),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }

    private static APIGatewayHttpApiV2ProxyResponse BadRequest(string message)
    {
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 400,
            Body = JsonSerializer.Serialize(new { error = message }),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}
