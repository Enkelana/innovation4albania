namespace Innovation4Albania.Application.DTOs.Dashboard;

public sealed record OverviewCardDto(
    int ActiveMinistries,
    int TotalProjects,
    int TotalExperts,
    int RiskProjects,
    int AverageKpi,
    int UpcomingDeadlines,
    int PendingApprovals,
    int CompletedProjects,
    int RiskRate,
    int AtRiskDeadlines,
    int AverageProgress,
    int MilestoneCompletionRate,
    int AverageTasksPerExpert);
