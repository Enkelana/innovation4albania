namespace Innovation4Albania.Application.DTOs.Reporting;

public sealed record PortfolioPeriodicReportDto(
    string PeriodType,
    string PeriodLabel,
    PortfolioKpiSnapshotDto Snapshot,
    IReadOnlyList<ReportProjectSummaryDto> PriorityProjects,
    IReadOnlyList<ReportProjectSummaryDto> Highlights,
    string Html);
