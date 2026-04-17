namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record AiRiskFactorDto(
    string Key,
    string Label,
    string Value,
    int Contribution);

public sealed record AiRiskAnalysisDto(
    string ProjectId,
    int Score,
    string Level,
    string Label,
    string Color,
    string Explanation,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<AiRiskFactorDto> Factors,
    string AnalyzedOn,
    bool UsedAi,
    string? WarningMessage);

public sealed record AiHeatmapItemDto(
    string ProjectId,
    string Title,
    string MinistryName,
    int Score,
    string Label,
    string Color);

public sealed record SmartAlertRecommendationDto(
    string ProjectId,
    string ProjectTitle,
    string Severity,
    string Title,
    string Message,
    int UrgencyScore,
    IReadOnlyList<string> RecommendedActions,
    bool UsedAi);

public sealed class PdfExtractionRequestDto
{
    public required string FileName { get; set; }
    public required string PdfBase64 { get; set; }
}

public sealed record PdfWorkflowStepSuggestionDto(
    string Title,
    string Description);

public sealed record PdfExtractionResultDto(
    string Title,
    string? Description,
    string? Ministry,
    string? ResponsiblePerson,
    string? StartDate,
    string? Deadline,
    int? KpiPercent,
    IReadOnlyList<string> Objectives,
    IReadOnlyList<PdfWorkflowStepSuggestionDto> WorkflowSteps,
    string? Budget,
    int Confidence,
    bool UsedAi,
    string? WarningMessage);

public sealed record PdfExtractionHistoryDto(
    string FileName,
    int Confidence,
    string CreatedOn,
    bool UsedAi);

public sealed class AiChatRequestDto
{
    public required string Message { get; set; }
    public List<AiChatMessageDto> ConversationHistory { get; set; } = [];
}

public sealed record AiChatMessageDto(
    string Role,
    string Content);

public sealed record AiChatResponseDto(
    string Message,
    bool UsedAi,
    string? WarningMessage);

public sealed record AiProjectSummaryDto(
    string ProjectId,
    string Summary,
    string GeneratedOn,
    bool UsedAi,
    string? WarningMessage);

public sealed record BulkAiReportDto(
    string Html,
    int ProjectCount,
    string GeneratedOn,
    bool UsedAi,
    string? WarningMessage);
