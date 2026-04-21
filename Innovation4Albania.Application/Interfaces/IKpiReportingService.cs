using Innovation4Albania.Application.DTOs.Dashboard;
using Innovation4Albania.Application.DTOs.Reporting;
using Innovation4Albania.Domain.Entities;

namespace Innovation4Albania.Application.Interfaces;

public interface IKpiReportingService
{
    PortfolioKpiSnapshotDto BuildKpiSnapshot(
        IReadOnlyList<InnovationProject> projects,
        int totalExperts,
        int totalTasks,
        IReadOnlyList<ProjectMilestone> milestones);

    OverviewCardDto BuildOverview(
        IReadOnlyList<InnovationProject> projects,
        int activeMinistries,
        int totalExperts,
        int totalTasks,
        IReadOnlyList<ProjectMilestone> milestones);

    IReadOnlyList<MinistryBoardItemDto> BuildMinistryBoard(
        IReadOnlyList<Ministry> ministries,
        IReadOnlyList<InnovationProject> projects,
        IReadOnlyList<Expert> experts);

    IReadOnlyList<TimelinePointDto> BuildTimeline(IReadOnlyList<InnovationProject> projects);

    PortfolioMonthlyReportDto BuildMonthlyReport(
        string monthLabel,
        IReadOnlyList<InnovationProject> projects,
        IReadOnlyList<Ministry> ministries);

    PortfolioPeriodicReportDto BuildDailyReport(
        string dayLabel,
        IReadOnlyList<InnovationProject> projects,
        IReadOnlyList<Ministry> ministries,
        int totalExperts,
        int totalTasks,
        IReadOnlyList<ProjectMilestone> milestones);

    PortfolioPeriodicReportDto BuildWeeklyReport(
        string weekLabel,
        IReadOnlyList<InnovationProject> projects,
        IReadOnlyList<Ministry> ministries,
        int totalExperts,
        int totalTasks,
        IReadOnlyList<ProjectMilestone> milestones);

    string GetProjectRiskLevel(InnovationProject project);
    string GetMinistryHealthStatus(IReadOnlyList<InnovationProject> projects);
}
