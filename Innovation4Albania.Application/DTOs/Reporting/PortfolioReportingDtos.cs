namespace Innovation4Albania.Application.DTOs.Reporting;

public sealed record MinistryPerformanceReportRowDto(
    string MinistryId,
    string MinistryName,
    int ActiveProjects,
    int CompletedProjects,
    int AverageKpi,
    string HealthStatus,
    string TrendIndicator);

public sealed record ReportProjectSummaryDto(
    string ProjectId,
    string Title,
    string MinistryName,
    int Kpi,
    string DueDateLabel);

public sealed record PortfolioMonthlyReportDto(
    string MonthLabel,
    int ActiveProjects,
    int TotalProjects,
    int CompletedProjects,
    int AverageKpi,
    int RiskProjects,
    IReadOnlyList<MinistryPerformanceReportRowDto> Ministries,
    IReadOnlyList<ReportProjectSummaryDto> AtRiskProjects,
    IReadOnlyList<ReportProjectSummaryDto> Highlights,
    string Html);
