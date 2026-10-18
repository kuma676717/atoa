using System.Text.Json.Serialization;

namespace MigrationFortune.Models;

public class AssessRequest
{
    [JsonPropertyName("workloadName")]
    public string? WorkloadName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("techStack")]
    public string[]? TechStack { get; set; }

    [JsonPropertyName("yearsInProduction")]
    public int? YearsInProduction { get; set; }
}
