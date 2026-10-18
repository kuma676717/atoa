using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using MigrationFortune.Models;
using MigrationFortune.Services;

namespace MigrationFortune.Functions;

public class GetAssessmentFunction
{
    private readonly BlobStorageService _blob;

    public GetAssessmentFunction(BlobStorageService blob)
    {
        _blob = blob;
    }

    [Function("GetAssessment")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assessment/{id}")]
        HttpRequestData req,
        string id)
    {
        AssessmentResult? result = await _blob.GetAsync(id);

        if (result == null)
        {
            HttpResponseData notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "Assessment not found", id });
            return notFound;
        }

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result);
        return response;
    }
}
