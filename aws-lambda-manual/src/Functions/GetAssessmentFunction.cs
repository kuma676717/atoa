using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MigrationFortuneManual.Models;
using MigrationFortuneManual.Services;

namespace MigrationFortuneManual.Functions;

public class GetAssessmentFunction
{
    private readonly S3StorageService _s3;

    public GetAssessmentFunction()
    {
        _s3 = new S3StorageService();
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> HandleAsync(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        string? id = null;
        request.PathParameters?.TryGetValue("id", out id);

        if (string.IsNullOrEmpty(id))
        {
            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 400,
                Body = JsonSerializer.Serialize(new { error = "id path parameter is required" }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        AssessmentResult? result = await _s3.GetAsync(id);

        if (result == null)
        {
            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 404,
                Body = JsonSerializer.Serialize(new { error = "Assessment not found", id }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(result),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}
