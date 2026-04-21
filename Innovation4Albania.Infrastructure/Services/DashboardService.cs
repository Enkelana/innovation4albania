using Innovation4Albania.Application.DTOs.Auth;
using Innovation4Albania.Application.DTOs.Dashboard;
using Innovation4Albania.Application.Interfaces;
using Innovation4Albania.Domain.Entities;
using Innovation4Albania.Domain.Enums;

namespace Innovation4Albania.Infrastructure.Services;

public sealed class DashboardService(IPlatformRepository repository, IKpiReportingService kpiReportingService) : IDashboardService
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
        var projects = repository.GetProjects()
            .Where(project => visibleMinistryIds.Contains(project.MinistryId))
            .Select(BuildDerivedProject)
            .ToList();
        var experts = repository.GetExperts().Where(expert => visibleMinistryIds.Contains(expert.MinistryId)).ToList();
        var alerts = repository.GetAlerts().Where(alert => visibleMinistryIds.Contains(alert.MinistryId)).ToList();
        var steps = repository.GetWorkflowSteps().Where(step => projects.Any(project => project.Id == step.ProjectId)).ToList();
        var milestones = repository.GetProjectMilestones().Where(item => projects.Any(project => project.Id == item.ProjectId)).ToList();
        var tasks = repository.GetTasks().Where(task => projects.Any(project => project.Id == task.ProjectId)).ToList();
        var overview = kpiReportingService.BuildOverview(projects, visibleMinistries.Count, experts.Count, tasks.Count, milestones);

        var ministryBoard = kpiReportingService.BuildMinistryBoard(visibleMinistries, projects, experts);

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
                kpiReportingService.GetProjectRiskLevel(project),
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

        var timeline = kpiReportingService.BuildTimeline(projects);

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

    private InnovationProject BuildDerivedProject(InnovationProject project) =>
        ProjectMetricsCalculator.WithDerivedMetrics(
            project,
            repository.GetTasks().Where(item => item.ProjectId == project.Id),
            repository.GetWorkflowSteps().Where(item => item.ProjectId == project.Id),
            repository.GetProjectMilestones().Where(item => item.ProjectId == project.Id));

}
