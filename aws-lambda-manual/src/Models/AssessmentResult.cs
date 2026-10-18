using System.Text.Json.Serialization;

namespace MigrationFortuneManual.Models;

public class AssessmentResult
{
    [JsonPropertyName("assessmentId")]
    public string AssessmentId { get; set; } = string.Empty;

    [JsonPropertyName("workloadName")]
    public string WorkloadName { get; set; } = string.Empty;

    [JsonPropertyName("migrationComplexity")]
    public string MigrationComplexity { get; set; } = string.Empty;

    [JsonPropertyName("recommendedStrategy")]
    public string RecommendedStrategy { get; set; } = string.Empty;

    [JsonPropertyName("successProbability")]
    public int SuccessProbability { get; set; }

    [JsonPropertyName("fortune")]
    public string Fortune { get; set; } = string.Empty;

    [JsonPropertyName("divineAdvice")]
    public string DivineAdvice { get; set; } = string.Empty;

    [JsonPropertyName("assessedAt")]
    public string AssessedAt { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("storagePath")]
    public string StoragePath { get; set; } = string.Empty;
}
