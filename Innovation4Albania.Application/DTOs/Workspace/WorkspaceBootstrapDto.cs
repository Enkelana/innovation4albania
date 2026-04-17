using Innovation4Albania.Application.DTOs.Dashboard;

namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record WorkspaceBootstrapDto(
    DashboardResponseDto Dashboard,
    IReadOnlyList<MinistryOptionDto> Ministries,
    IReadOnlyList<ExpertDto> Experts,
    IReadOnlyList<DocumentDto> Documents,
    IReadOnlyList<WorkflowStepDto> WorkflowSteps,
    IReadOnlyList<ProjectNoteDto> Notes,
    IReadOnlyList<MeetingDto> Meetings,
    IReadOnlyList<TaskDto> Tasks,
    IReadOnlyList<OkrDto> Okrs,
    IReadOnlyList<MinistryDetailDto> MinistryDetails,
    IReadOnlyList<CalendarEventDto> CalendarEvents,
    IReadOnlyList<NotificationDto> Notifications,
    IReadOnlyList<ImportLogDto> ImportLogs,
    AlertSettingsDto AlertSettings,
    SyncStatusDto SyncStatus,
    MonthlyReportStatusDto MonthlyReportStatus);
