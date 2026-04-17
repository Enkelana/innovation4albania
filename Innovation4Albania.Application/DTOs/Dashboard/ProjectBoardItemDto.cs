namespace Innovation4Albania.Application.DTOs.Dashboard;

public sealed record ProjectBoardItemDto(
    string ProjectId,
    string Title,
    string MinistryName,
    string Status,
    string ApprovalStage,
    string OwnerName,
    int Kpi,
    int Progress,
    DateOnly StartDate,
    DateOnly DueDate,
    string RiskLevel,
    string? CancellationReason);
