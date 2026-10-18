using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using MigrationFortune.Models;
using MigrationFortune.Services;

namespace MigrationFortune.Functions;

public class AssessFunction
{
    private readonly FortuneEngine _fortune;
    private readonly BlobStorageService _blob;

    public AssessFunction(FortuneEngine fortune, BlobStorageService blob)
    {
        _fortune = fortune;
        _blob = blob;
    }

    [Function("Assess")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "assess")]
        HttpRequestData req)
    {
        // Determine RNG: seeded if X-Force-Seed header present
        req.Headers.TryGetValues("X-Force-Seed", out IEnumerable<string>? seeds);
        int seedVal = 0;
        bool hasSeed = seeds != null
            && seeds.Any()
            && int.TryParse(seeds.First(), out seedVal);
        Random rng = hasSeed ? new Random(seedVal) : Random.Shared;

        // Read and deserialize body
        string body = await req.ReadAsStringAsync() ?? string.Empty;
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
            return await BadRequestAsync(req, "invalid request body");

        if (input.WorkloadName == null || string.IsNullOrWhiteSpace(input.WorkloadName))
            return await BadRequestAsync(req, "workloadName is required");

        if (input.Description == null || string.IsNullOrWhiteSpace(input.Description))
            return await BadRequestAsync(req, "description is required");

        if (input.YearsInProduction.HasValue && input.YearsInProduction.Value < 0)
            return await BadRequestAsync(req, "yearsInProduction must be >= 0");

        // Assess and persist
        AssessmentResult result = _fortune.Assess(input, "azure-functions", rng);
        await _blob.SaveAsync(result.AssessmentId, result);

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    private static async Task<HttpResponseData> BadRequestAsync(HttpRequestData req, string message)
    {
        HttpResponseData r = req.CreateResponse(HttpStatusCode.BadRequest);
        await r.WriteAsJsonAsync(new { error = message });
        return r;
    }
}
