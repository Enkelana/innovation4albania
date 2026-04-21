using Innovation4Albania.Application.DTOs.Dashboard;
using Innovation4Albania.Application.DTOs.Reporting;
using Innovation4Albania.Application.Interfaces;
using Innovation4Albania.Domain.Entities;
using Innovation4Albania.Domain.Enums;

namespace Innovation4Albania.Infrastructure.Services;

public sealed class KpiReportingService : IKpiReportingService
{
    public PortfolioKpiSnapshotDto BuildKpiSnapshot(
        IReadOnlyList<InnovationProject> projects,
        int totalExperts,
        int totalTasks,
        IReadOnlyList<ProjectMilestone> milestones)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var activeProjects = projects.Where(item => item.Status is ProjectStatus.Active or ProjectStatus.InProcess).ToList();
        var completedProjects = projects.Where(item => item.Status == ProjectStatus.Completed).ToList();
        var onTimeCompleted = completedProjects.Count(item => item.Progress >= 100 && item.DueDate >= today);
        var riskProjects = activeProjects.Where(item => GetProjectRiskLevel(item) == "High").ToList();
        var overdueProjects = activeProjects.Where(item => item.DueDate < today).ToList();
        var totalDelayDays = overdueProjects.Sum(item => Math.Max(0, today.DayNumber - item.DueDate.DayNumber));
        var totalDeviationDays = activeProjects.Sum(item =>
        {
            var expected = ExpectedProgress(item, today);
            return Math.Max(0, expected - item.Progress);
        });
        var achievedMilestones = milestones.Count(item => item.AchievedAtUtc.HasValue);
        var criticalRiskProjects = activeProjects.Count(item => item.Kpi < 40);

        return new PortfolioKpiSnapshotDto(
            ActiveProjects: activeProjects.Count,
            CompletedProjects: completedProjects.Count,
            OnTimeCompletionRate: completedProjects.Count == 0 ? 0 : (int)Math.Round((double)onTimeCompleted / completedProjects.Count * 100),
            RiskRate: activeProjects.Count == 0 ? 0 : (int)Math.Round((double)riskProjects.Count / activeProjects.Count * 100),
            AverageDelayDays: overdueProjects.Count == 0 ? 0 : (int)Math.Round((double)totalDelayDays / overdueProjects.Count),
            PlanDeviationDays: activeProjects.Count == 0 ? 0 : (int)Math.Round((double)totalDeviationDays / activeProjects.Count),
            AtRiskDeadlines: activeProjects.Count(item => item.Progress < 50 && item.DueDate >= today && item.DueDate <= today.AddDays(14)),
            AverageProgress: projects.Count == 0 ? 0 : (int)Math.Round(projects.Average(item => item.Progress)),
            MilestoneCompletionRate: milestones.Count == 0 ? 0 : (int)Math.Round((double)achievedMilestones / milestones.Count * 100),
            AverageTasksPerExpert: totalExperts == 0 ? 0 : (int)Math.Round((double)totalTasks / totalExperts),
            CriticalRiskProjects: criticalRiskProjects,
            RiskIndex: CalculateRiskIndex(projects));
    }

    public OverviewCardDto BuildOverview(
        IReadOnlyList<InnovationProject> projects,
        int activeMinistries,
        int totalExperts,
        int totalTasks,
        IReadOnlyList<ProjectMilestone> milestones)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var snapshot = BuildKpiSnapshot(projects, totalExperts, totalTasks, milestones);
        var riskProjectsCount = projects.Count(project => GetProjectRiskLevel(project) == "High");

        return new OverviewCardDto(
            ActiveMinistries: activeMinistries,
            TotalProjects: projects.Count,
            TotalExperts: totalExperts,
            RiskProjects: riskProjectsCount,
            AverageKpi: projects.Count == 0 ? 0 : (int)Math.Round(projects.Average(project => project.Kpi)),
            UpcomingDeadlines: projects.Count(project => project.DueDate >= today && project.DueDate <= today.AddDays(14)),
            PendingApprovals: projects.Count(project => project.ApprovalStage == ApprovalStage.UnderReview),
            CompletedProjects: snapshot.CompletedProjects,
            RiskRate: snapshot.RiskRate,
            AtRiskDeadlines: snapshot.AtRiskDeadlines,
            AverageProgress: snapshot.AverageProgress,
            MilestoneCompletionRate: snapshot.MilestoneCompletionRate,
            AverageTasksPerExpert: snapshot.AverageTasksPerExpert);
    }

    public IReadOnlyList<MinistryBoardItemDto> BuildMinistryBoard(
        IReadOnlyList<Ministry> ministries,
        IReadOnlyList<InnovationProject> projects,
        IReadOnlyList<Expert> experts) =>
        ministries
            .Select(ministry =>
            {
                var ministryProjects = projects.Where(project => project.MinistryId == ministry.Id).ToList();
                return new MinistryBoardItemDto(
                    ministry.Id,
                    ministry.Name,
                    ministry.Acronym,
                    experts.Count(expert => expert.MinistryId == ministry.Id),
                    ministryProjects.Count,
                    ministryProjects.Count(project => project.Status == ProjectStatus.Active),
                    ministryProjects.Count(project => project.Status == ProjectStatus.InProcess),
                    ministryProjects.Count(project => project.Status == ProjectStatus.Completed),
                    ministryProjects.Count(project => project.Status == ProjectStatus.Cancelled),
                    ministryProjects.Count == 0 ? 0 : (int)Math.Round(ministryProjects.Average(project => project.Kpi)),
                    GetMinistryHealthStatus(ministryProjects));
            })
            .OrderBy(item => item.MinistryName)
            .ToList();

    public IReadOnlyList<TimelinePointDto> BuildTimeline(IReadOnlyList<InnovationProject> projects)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var monthAnchor = new DateOnly(today.Year, today.Month, 1);
        var points = new List<TimelinePointDto>();

        for (var monthsBack = 5; monthsBack >= 0; monthsBack--)
        {
            var monthStart = monthAnchor.AddMonths(-monthsBack);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            points.Add(new TimelinePointDto(
                monthStart.ToString("MMM yyyy"),
                projects.Count(project => project.StartDate >= monthStart && project.StartDate <= monthEnd),
                projects.Count(project => project.Status == ProjectStatus.Completed && project.DueDate >= monthStart && project.DueDate <= monthEnd)));
        }

        return points;
    }

    public PortfolioMonthlyReportDto BuildMonthlyReport(
        string monthLabel,
        IReadOnlyList<InnovationProject> projects,
        IReadOnlyList<Ministry> ministries)
    {
        var activeProjects = projects.Count(item => item.Status is ProjectStatus.Active or ProjectStatus.InProcess);
        var completedProjects = projects.Count(item => item.Status == ProjectStatus.Completed);
        var averageKpi = projects.Count == 0 ? 0 : (int)Math.Round(projects.Average(item => item.Kpi));
        var riskProjects = projects
            .Where(item => item.Kpi < 60 || item.DueDate < DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(item => item.Kpi)
            .ThenBy(item => item.DueDate)
            .Take(10)
            .ToList();
        var highlights = projects
            .Where(item => item.Status == ProjectStatus.Completed)
            .OrderByDescending(item => item.Kpi)
            .Take(5)
            .ToList();
        var ministryMap = ministries.ToDictionary(item => item.Id, item => item.Name, StringComparer.OrdinalIgnoreCase);

        var ministryRows = ministries
            .Select(ministry =>
            {
                var related = projects.Where(project => project.MinistryId == ministry.Id).ToList();
                var avg = related.Count == 0 ? 0 : (int)Math.Round(related.Average(project => project.Kpi));
                return new MinistryPerformanceReportRowDto(
                    ministry.Id,
                    ministry.Name,
                    related.Count(project => project.Status is ProjectStatus.Active or ProjectStatus.InProcess),
                    related.Count(project => project.Status == ProjectStatus.Completed),
                    avg,
                    GetMinistryHealthStatus(related),
                    avg >= 70 ? "↑" : avg >= 50 ? "→" : "↓");
            })
            .ToList();

        var atRiskRows = riskProjects
            .Select(project => new ReportProjectSummaryDto(
                project.Id,
                project.Title,
                ministryMap[project.MinistryId],
                project.Kpi,
                project.DueDate.ToString("dd/MM/yyyy")))
            .ToList();

        var highlightRows = highlights
            .Select(project => new ReportProjectSummaryDto(
                project.Id,
                project.Title,
                ministryMap[project.MinistryId],
                project.Kpi,
                project.DueDate.ToString("dd/MM/yyyy")))
            .ToList();

        var html = BuildMonthlyReportHtml(
            monthLabel,
            activeProjects,
            projects.Count,
            completedProjects,
            averageKpi,
            atRiskRows,
            highlightRows,
            ministryRows);

        return new PortfolioMonthlyReportDto(
            monthLabel,
            activeProjects,
            projects.Count,
            completedProjects,
            averageKpi,
            atRiskRows.Count,
            ministryRows,
            atRiskRows,
            highlightRows,
            html);
    }

    public PortfolioPeriodicReportDto BuildDailyReport(
        string dayLabel,
        IReadOnlyList<InnovationProject> projects,
        IReadOnlyList<Ministry> ministries,
        int totalExperts,
        int totalTasks,
        IReadOnlyList<ProjectMilestone> milestones) =>
        BuildPeriodicReport(
            "daily",
            dayLabel,
            "Raporti Ditor i Portofolit",
            projects,
            ministries,
            totalExperts,
            totalTasks,
            milestones,
            priorityProjectSelector: items => items
                .Where(item => item.Status is ProjectStatus.Active or ProjectStatus.InProcess)
                .Where(item => item.DueDate <= DateOnly.FromDateTime(DateTime.Today).AddDays(7) || item.Kpi < 60)
                .OrderBy(item => item.DueDate)
                .ThenBy(item => item.Kpi)
                .Take(8)
                .ToList(),
            highlightSelector: items => items
                .Where(item => item.Status == ProjectStatus.Completed)
                .OrderByDescending(item => item.Kpi)
                .Take(3)
                .ToList());

    public PortfolioPeriodicReportDto BuildWeeklyReport(
        string weekLabel,
        IReadOnlyList<InnovationProject> projects,
        IReadOnlyList<Ministry> ministries,
        int totalExperts,
        int totalTasks,
        IReadOnlyList<ProjectMilestone> milestones) =>
        BuildPeriodicReport(
            "weekly",
            weekLabel,
            "Raporti Javor i Portofolit",
            projects,
            ministries,
            totalExperts,
            totalTasks,
            milestones,
            priorityProjectSelector: items => items
                .Where(item => item.Status is ProjectStatus.Active or ProjectStatus.InProcess)
                .Where(item => GetProjectRiskLevel(item) == "High" || item.Progress < 50)
                .OrderBy(item => item.Kpi)
                .ThenBy(item => item.DueDate)
                .Take(10)
                .ToList(),
            highlightSelector: items => items
                .Where(item => item.Status == ProjectStatus.Completed || item.Kpi >= 80)
                .OrderByDescending(item => item.Kpi)
                .Take(5)
                .ToList());

    public string GetProjectRiskLevel(InnovationProject project)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (project.Status == ProjectStatus.Cancelled || project.Kpi < 40 || project.DueDate < today)
        {
            return "High";
        }

        if (project.Kpi <= 70 || project.DueDate <= today.AddDays(14))
        {
            return "Medium";
        }

        return "Low";
    }

    public string GetMinistryHealthStatus(IReadOnlyList<InnovationProject> projects)
    {
        if (projects.Count == 0)
        {
            return "No data";
        }

        var averageKpi = projects.Average(project => project.Kpi);
        if (averageKpi < 50 || projects.Any(project => GetProjectRiskLevel(project) == "High"))
        {
            return "Needs attention";
        }

        if (averageKpi < 70)
        {
            return "Watchlist";
        }

        return "On track";
    }

    private PortfolioPeriodicReportDto BuildPeriodicReport(
        string periodType,
        string periodLabel,
        string title,
        IReadOnlyList<InnovationProject> projects,
        IReadOnlyList<Ministry> ministries,
        int totalExperts,
        int totalTasks,
        IReadOnlyList<ProjectMilestone> milestones,
        Func<IReadOnlyList<InnovationProject>, IReadOnlyList<InnovationProject>> priorityProjectSelector,
        Func<IReadOnlyList<InnovationProject>, IReadOnlyList<InnovationProject>> highlightSelector)
    {
        var snapshot = BuildKpiSnapshot(projects, totalExperts, totalTasks, milestones);
        var ministryMap = ministries.ToDictionary(item => item.Id, item => item.Name, StringComparer.OrdinalIgnoreCase);
        var priorityProjects = priorityProjectSelector(projects)
            .Select(project => new ReportProjectSummaryDto(
                project.Id,
                project.Title,
                ministryMap[project.MinistryId],
                project.Kpi,
                project.DueDate.ToString("dd/MM/yyyy")))
            .ToList();
        var highlights = highlightSelector(projects)
            .Select(project => new ReportProjectSummaryDto(
                project.Id,
                project.Title,
                ministryMap[project.MinistryId],
                project.Kpi,
                project.DueDate.ToString("dd/MM/yyyy")))
            .ToList();

        var priorityHtml = priorityProjects.Any()
            ? string.Join("", priorityProjects.Select(project => $"<li><strong>{project.Title}</strong> · {project.MinistryName} · KPI {project.Kpi}% · Afati {project.DueDateLabel}</li>"))
            : "<li>Nuk ka prioritete kritike ne kete periudhe.</li>";

        var highlightHtml = highlights.Any()
            ? string.Join("", highlights.Select(project => $"<li><strong>{project.Title}</strong> · {project.MinistryName} · KPI {project.Kpi}%</li>"))
            : "<li>Nuk ka arritje te vecanta per kete periudhe.</li>";

        var html = $$"""
<!DOCTYPE html>
<html lang="sq">
<head>
    <meta charset="utf-8" />
    <title>{{title}} - {{periodLabel}}</title>
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; padding: 24px; color: #12233d; }
        h1, h2 { margin: 0 0 12px; }
        .hero { border-bottom: 3px solid #0b4f9c; padding-bottom: 16px; margin-bottom: 20px; }
        .stats { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; margin: 18px 0; }
        .stats article { border: 1px solid #d9e4f2; border-radius: 14px; padding: 12px; background: #f8fbff; }
        ul { padding-left: 18px; }
    </style>
</head>
<body>
    <div class="hero">
        <h1>{{title}}</h1>
        <p>Periudha: {{periodLabel}}</p>
    </div>
    <section>
        <h2>Snapshot KPI</h2>
        <div class="stats">
            <article><strong>{{snapshot.ActiveProjects}}</strong><div>Projekte aktive</div></article>
            <article><strong>{{snapshot.CompletedProjects}}</strong><div>Projekte te perfunduara</div></article>
            <article><strong>{{snapshot.RiskRate}}%</strong><div>Risk rate</div></article>
            <article><strong>{{snapshot.AverageProgress}}%</strong><div>Progresi mesatar</div></article>
        </div>
    </section>
    <section>
        <h2>Projektet prioritare</h2>
        <ul>{{priorityHtml}}</ul>
    </section>
    <section>
        <h2>Arritjet</h2>
        <ul>{{highlightHtml}}</ul>
    </section>
</body>
</html>
""";

        return new PortfolioPeriodicReportDto(periodType, periodLabel, snapshot, priorityProjects, highlights, html);
    }

    private static int ExpectedProgress(InnovationProject project, DateOnly today)
    {
        var totalDays = Math.Max(1, project.DueDate.DayNumber - project.StartDate.DayNumber);
        var elapsedDays = Math.Clamp(today.DayNumber - project.StartDate.DayNumber, 0, totalDays);
        return (int)Math.Round((double)elapsedDays / totalDays * 100);
    }

    private decimal CalculateRiskIndex(IReadOnlyList<InnovationProject> projects)
    {
        if (projects.Count == 0)
        {
            return 0;
        }

        var weightedScore = projects.Sum(project => GetProjectRiskLevel(project) switch
        {
            "Low" => 1,
            "Medium" => 2,
            _ => 4
        });

        return Math.Round((decimal)weightedScore / projects.Count, 2);
    }

    private static string BuildMonthlyReportHtml(
        string monthLabel,
        int activeProjects,
        int totalProjects,
        int completedProjects,
        int averageKpi,
        IReadOnlyList<ReportProjectSummaryDto> atRiskRows,
        IReadOnlyList<ReportProjectSummaryDto> highlightRows,
        IReadOnlyList<MinistryPerformanceReportRowDto> ministryRows)
    {
        var riskRows = atRiskRows.Any()
            ? string.Join("", atRiskRows.Select(project => $"<li><strong>{project.Title}</strong> · {project.MinistryName} · KPI {project.Kpi}% · Afati {project.DueDateLabel}</li>"))
            : "<li>Nuk ka projekte kritike per kete periudhe.</li>";

        var highlights = highlightRows.Any()
            ? string.Join("", highlightRows.Select(project => $"<li><strong>{project.Title}</strong> · {project.MinistryName} · KPI final {project.Kpi}%</li>"))
            : "<li>Nuk ka projekte te perfunduara ne kete periudhe.</li>";

        var ministries = string.Join("", ministryRows.Select(row =>
            $"<tr><td>{row.MinistryName}</td><td>{row.ActiveProjects}</td><td>{row.CompletedProjects}</td><td>{row.AverageKpi}%</td><td>{row.TrendIndicator}</td></tr>"));

        return $$"""
<!DOCTYPE html>
<html lang="sq">
<head>
    <meta charset="utf-8" />
    <title>Raporti Mujor - {{monthLabel}}</title>
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; padding: 24px; color: #12233d; }
        h1, h2 { margin: 0 0 12px; }
        .hero { border-bottom: 3px solid #0b4f9c; padding-bottom: 16px; margin-bottom: 20px; }
        .stats { display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); gap: 12px; margin: 18px 0; }
        .stats article { border: 1px solid #d9e4f2; border-radius: 14px; padding: 12px; background: #f8fbff; }
        table { width: 100%; border-collapse: collapse; margin-top: 12px; }
        th, td { border: 1px solid #d9e4f2; padding: 10px; text-align: left; }
        th { background: #0b4f9c; color: white; }
        ul { padding-left: 18px; }
        footer { margin-top: 24px; color: #5f6f86; font-size: 12px; }
    </style>
</head>
<body>
    <div class="hero">
        <h1>Raporti Mujor i Inovacionit Publik</h1>
        <p>Muaji: {{monthLabel}}</p>
    </div>
    <section>
        <h2>Permbledhje Ekzekutive</h2>
        <div class="stats">
            <article><strong>{{activeProjects}}</strong><div>Projekte aktive</div></article>
            <article><strong>{{totalProjects}}</strong><div>Projekte totale</div></article>
            <article><strong>{{completedProjects}}</strong><div>Projekte te perfunduara</div></article>
            <article><strong>{{averageKpi}}%</strong><div>KPI mesatar kombetar</div></article>
            <article><strong>{{atRiskRows.Count}}</strong><div>Projekte ne risk</div></article>
        </div>
    </section>
    <section>
        <h2>Performanca sipas Ministrise</h2>
        <table>
            <thead><tr><th>Ministria</th><th>Aktive</th><th>Perfunduar</th><th>KPI Mesatar</th><th>Trendi</th></tr></thead>
            <tbody>{{ministries}}</tbody>
        </table>
    </section>
    <section>
        <h2>Projektet ne Risk</h2>
        <ul>{{riskRows}}</ul>
    </section>
    <section>
        <h2>Arritjet e Muajit</h2>
        <ul>{{highlights}}</ul>
    </section>
    <footer>Gjeneruar automatikisht nga Innovation4Albania.</footer>
</body>
</html>
""";
    }
}
