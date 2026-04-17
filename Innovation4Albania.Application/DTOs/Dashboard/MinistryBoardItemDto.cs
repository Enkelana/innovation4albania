namespace Innovation4Albania.Application.DTOs.Dashboard;

public sealed record MinistryBoardItemDto(
    string MinistryId,
    string MinistryName,
    string Acronym,
    int Experts,
    int TotalProjects,
    int ActiveProjects,
    int InProcessProjects,
    int CompletedProjects,
    int CancelledProjects,
    int AverageKpi,
    string HealthStatus);
