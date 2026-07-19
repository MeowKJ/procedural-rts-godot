using System.Text.Json.Serialization;

namespace ProjectReadyQueue;

public sealed record ProjectStateInput(
    [property: JsonPropertyName("schemaVersion")] int? SchemaVersion,
    [property: JsonPropertyName("projectNumber")] int? ProjectNumber,
    [property: JsonPropertyName("issues")] List<ProjectIssue> Issues);

public sealed record ProjectIssue(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("projectItemCount")] int? ProjectItemCount,
    [property: JsonPropertyName("workflow")] string? Workflow,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("priority")] string? Priority,
    [property: JsonPropertyName("size")] string? Size,
    [property: JsonPropertyName("agent")] string? Agent,
    [property: JsonPropertyName("verificationGate")] string? VerificationGate,
    [property: JsonPropertyName("body")] string? Body,
    [property: JsonPropertyName("labels")] List<string>? Labels,
    [property: JsonPropertyName("openSubIssues")] List<int>? OpenSubIssues,
    [property: JsonPropertyName("openBlockers")] List<int>? OpenBlockers);

public sealed record ProjectReadyEvaluation(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("workflow")] string? Workflow,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("priority")] string? Priority,
    [property: JsonPropertyName("size")] string? Size,
    [property: JsonPropertyName("agent")] string? Agent,
    [property: JsonPropertyName("verificationGate")] string? VerificationGate,
    [property: JsonPropertyName("paused")] bool Paused,
    [property: JsonPropertyName("openSubIssues")] IReadOnlyList<int> OpenSubIssues,
    [property: JsonPropertyName("openBlockers")] IReadOnlyList<int> OpenBlockers,
    [property: JsonPropertyName("eligible")] bool Eligible,
    [property: JsonPropertyName("reasonCodes")] IReadOnlyList<string> ReasonCodes,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings);

public sealed record ProjectReadyOutput(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("projectNumber")] int ProjectNumber,
    [property: JsonPropertyName("eligibleCount")] int EligibleCount,
    [property: JsonPropertyName("evaluatedCount")] int EvaluatedCount,
    [property: JsonPropertyName("eligible")] IReadOnlyList<ProjectReadyEvaluation> Eligible,
    [property: JsonPropertyName("evaluated")] IReadOnlyList<ProjectReadyEvaluation> Evaluated);
