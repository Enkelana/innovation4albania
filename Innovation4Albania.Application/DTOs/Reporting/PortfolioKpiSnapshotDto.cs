namespace Innovation4Albania.Application.DTOs.Reporting;

public sealed record PortfolioKpiSnapshotDto(
    int ActiveProjects,
    int CompletedProjects,
    int OnTimeCompletionRate,
    int RiskRate,
    int AverageDelayDays,
    int PlanDeviationDays,
    int AtRiskDeadlines,
    int AverageProgress,
    int MilestoneCompletionRate,
    int AverageTasksPerExpert,
    int CriticalRiskProjects,
    decimal RiskIndex);
