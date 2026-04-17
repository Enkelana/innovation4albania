using Innovation4Albania.Application.DTOs.Auth;
using Innovation4Albania.Application.DTOs.Dashboard;
using Innovation4Albania.Application.Interfaces;
using Innovation4Albania.Domain.Entities;
using Innovation4Albania.Domain.Enums;

namespace Innovation4Albania.Infrastructure.Services;

public sealed class DashboardService(IPlatformRepository repository) : IDashboardService
{
    public IReadOnlyList<SessionUserDto> GetUsers() =>
        repository.GetUsers()
            .Select(MapUser)
            .ToList();

    public SessionUserDto? GetSession(string userId)
    {
        var user = repository.GetUserById(userId);
        return user is null ? null : MapUser(user);
    }

    public ExpertAccessGateDto? GetExpertAccessGate(string userId)
    {
        var user = repository.GetUserById(userId);
        if (user is null || !RequiresMinistryVerification(user) || string.IsNullOrWhiteSpace(user.MinistryId))
        {
            return null;
        }

        return new ExpertAccessGateDto(
            user.Id,
            user.FullName,
            user.MinistryId,
            repository.GetMinistries()
                .OrderBy(ministry => ministry.Name)
                .Select(ministry => new ExpertAccessMinistryDto(
                    ministry.Id,
                    ministry.Name,
                    ministry.Acronym,
                    repository.GetDemoAccessCode(ministry.Id) ?? "-"))
                .ToList());
    }

    public VerifyExpertAccessResultDto VerifyExpertAccess(VerifyExpertAccessRequestDto request)
    {
        var user = repository.GetUserById(request.UserId);
        if (user is null || !RequiresMinistryVerification(user) || string.IsNullOrWhiteSpace(user.MinistryId))
        {
            return new VerifyExpertAccessResultDto(false, "Perdoruesi nuk u gjet ose nuk ka akses te vlefshem.");
        }

        if (!string.Equals(user.MinistryId, request.MinistryId, StringComparison.OrdinalIgnoreCase))
        {
            return new VerifyExpertAccessResultDto(false, "Kjo ministri nuk eshte e lidhur me llogarine tuaj.");
        }

        if (!repository.VerifyMinistryAccessCode(request.MinistryId, request.AccessCode))
        {
            return new VerifyExpertAccessResultDto(false, "Kodi i aksesit eshte i pasakte. Provoni perseri.");
        }

        return new VerifyExpertAccessResultDto(true, "Verifikimi u krye me sukses.", request.MinistryId);
    }

    public DashboardResponseDto? GetDashboard(string userId)
    {
        var user = repository.GetUserById(userId);
        if (user is null)
        {
            return null;
        }

        var visibleMinistries = FilterMinistries(user);
        var visibleMinistryIds = visibleMinistries.Select(ministry => ministry.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var projects = repository.GetProjects().Where(project => visibleMinistryIds.Contains(project.MinistryId)).ToList();
        var experts = repository.GetExperts().Where(expert => visibleMinistryIds.Contains(expert.MinistryId)).ToList();
        var alerts = repository.GetAlerts().Where(alert => visibleMinistryIds.Contains(alert.MinistryId)).ToList();
        var steps = repository.GetWorkflowSteps().Where(step => projects.Any(project => project.Id == step.ProjectId)).ToList();
        var milestones = repository.GetProjectMilestones().Where(item => projects.Any(project => project.Id == item.ProjectId)).ToList();
        var tasks = repository.GetTasks().Where(task => projects.Any(project => project.Id == task.ProjectId)).ToList();
        var activeProjectsCount = projects.Count(project => project.Status is ProjectStatus.Active or ProjectStatus.InProcess);
        var completedProjectsCount = projects.Count(project => project.Status == ProjectStatus.Completed);
        var riskProjectsCount = projects.Count(project => GetRiskLevel(project) is "High");
        var atRiskDeadlinesCount = projects.Count(project =>
            project.Status is ProjectStatus.Active or ProjectStatus.InProcess &&
            project.Progress < 50 &&
            project.DueDate >= DateOnly.FromDateTime(DateTime.UtcNow.Date) &&
            project.DueDate <= DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(14));
        var achievedMilestones = milestones.Count(item => item.AchievedAtUtc.HasValue);

        var overview = new OverviewCardDto(
            ActiveMinistries: visibleMinistries.Count,
            TotalProjects: projects.Count,
            TotalExperts: experts.Count,
            RiskProjects: riskProjectsCount,
            AverageKpi: projects.Count == 0 ? 0 : (int)Math.Round(projects.Average(project => project.Kpi)),
            UpcomingDeadlines: projects.Count(project =>
                project.DueDate >= DateOnly.FromDateTime(DateTime.UtcNow.Date) &&
                project.DueDate <= DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(14)),
            PendingApprovals: projects.Count(project => project.ApprovalStage == ApprovalStage.UnderReview),
            CompletedProjects: completedProjectsCount,
            RiskRate: activeProjectsCount == 0 ? 0 : (int)Math.Round((double)riskProjectsCount / activeProjectsCount * 100),
            AtRiskDeadlines: atRiskDeadlinesCount,
            AverageProgress: projects.Count == 0 ? 0 : (int)Math.Round(projects.Average(project => project.Progress)),
            MilestoneCompletionRate: milestones.Count == 0 ? 0 : (int)Math.Round((double)achievedMilestones / milestones.Count * 100),
            AverageTasksPerExpert: experts.Count == 0 ? 0 : (int)Math.Round((double)tasks.Count / experts.Count));

        var ministryBoard = visibleMinistries
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
                    GetHealthStatus(ministryProjects));
            })
            .OrderBy(item => item.MinistryName)
            .ToList();

        var ministriesById = visibleMinistries.ToDictionary(ministry => ministry.Id, ministry => ministry.Name, StringComparer.OrdinalIgnoreCase);
        var projectsById = projects.ToDictionary(project => project.Id, StringComparer.OrdinalIgnoreCase);

        var projectItems = projects
            .OrderBy(project => project.DueDate)
            .Select(project => new ProjectBoardItemDto(
                project.Id,
                project.Title,
                ministriesById[project.MinistryId],
                project.Status.ToString(),
                project.ApprovalStage.ToString(),
                project.OwnerName,
                project.Kpi,
                project.Progress,
                project.StartDate,
                project.DueDate,
                GetRiskLevel(project),
                project.CancellationReason))
            .ToList();

        var alertItems = alerts
            .Select(alert => new AlertDto(
                alert.Id,
                alert.Severity.ToString(),
                alert.Title,
                ministriesById[alert.MinistryId],
                projectsById.TryGetValue(alert.ProjectId, out var project) ? project.Title : "Projekt",
                alert.Message))
            .OrderByDescending(alert => alert.Severity)
            .Take(8)
            .ToList();

        var workflowSnapshots = steps
            .OrderBy(step => step.DueDate)
            .Take(8)
            .Select(step =>
            {
                var project = projectsById[step.ProjectId];
                return new WorkflowSnapshotDto(
                    project.Title,
                    ministriesById[project.MinistryId],
                    $"Hapi {step.StepNumber}: {step.Description}",
                    step.Status,
                    step.Progress,
                    step.DueDate);
            })
            .ToList();

        var timeline = BuildTimeline(projects);

        var logs = IsDirectorLike(user)
            ? repository.GetHistoryLogs()
                .Where(log => projectsById.ContainsKey(log.ProjectId))
                .Take(12)
                .Select(log => new HistoryLogDto(
                    log.TimestampUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                    log.UserName,
                    log.ActionType,
                    log.FieldName,
                    $"{log.PreviousValue} -> {log.NewValue}",
                    log.ProjectId))
                .ToList()
            : [];

        var capabilities = new RoleCapabilitiesDto(
            CanEditProjects: IsDirectorLike(user),
            CanManageExperts: IsDirectorLike(user),
            CanConfigureAlerts: IsDirectorLike(user),
            CanUploadDocuments: IsDirectorLike(user) || user.Role == UserRole.Expert,
            CanViewAuditLogs: IsDirectorLike(user),
            CanExportReports: true);

        return new DashboardResponseDto(
            MapUser(user),
            overview,
            ministryBoard,
            projectItems,
            alertItems,
            timeline,
            workflowSnapshots,
            logs,
            capabilities);
    }

    private IReadOnlyList<Ministry> FilterMinistries(PlatformUser user)
    {
        var ministries = repository.GetMinistries();
        return IsMinistryScopedRole(user) && !string.IsNullOrWhiteSpace(user.MinistryId)
            ? ministries.Where(ministry => ministry.Id == user.MinistryId).ToList()
            : ministries;
    }

    private static SessionUserDto MapUser(PlatformUser user) =>
        new(user.Id, user.FullName, user.Email, user.Role.ToString(), user.RoleLabel, user.MinistryId);

    private static bool RequiresMinistryVerification(PlatformUser user) =>
        user.Role is UserRole.Expert or UserRole.NucleusDirector;

    private static bool IsDirectorLike(PlatformUser user) =>
        user.Role is UserRole.Director or UserRole.NucleusDirector;

    private static bool IsMinistryScopedRole(PlatformUser user) =>
        user.Role is UserRole.Expert or UserRole.NucleusDirector;

    private static string GetRiskLevel(InnovationProject project)
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

    private static string GetHealthStatus(IEnumerable<InnovationProject> projects)
    {
        var projectList = projects.ToList();
        if (projectList.Count == 0)
        {
            return "No data";
        }

        var averageKpi = projectList.Average(project => project.Kpi);
        if (averageKpi < 50 || projectList.Any(project => GetRiskLevel(project) == "High"))
        {
            return "Needs attention";
        }

        if (averageKpi < 70)
        {
            return "Watchlist";
        }

        return "On track";
    }

    private static IReadOnlyList<TimelinePointDto> BuildTimeline(IReadOnlyList<InnovationProject> projects)
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
}
