namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record ProjectDetailDto(
    string ProjectId,
    string Title,
    string MinistryId,
    string MinistryName,
    string Status,
    string OwnerName,
    int Kpi,
    int Progress,
    string Description,
    string ApprovalStage,
    string StartDate,
    string DueDate,
    string RiskLevel,
    string? CancellationReason,
    string? RejectionReason,
    IReadOnlyList<WorkflowStepDto> Workflow,
    IReadOnlyList<DocumentDto> Documents,
    IReadOnlyList<ProjectNoteDto> Notes,
    IReadOnlyList<MeetingDto> Meetings,
    IReadOnlyList<TaskDto> Tasks,
    IReadOnlyList<ProjectMilestoneDto> Milestones,
    IReadOnlyList<ProjectPhotoDto> Photos,
    IReadOnlyList<ProjectOkrLinkDto> OkrLinks,
    IReadOnlyList<ProjectHistoryEntryDto> History,
    IReadOnlyList<ApprovalHistoryDto> ApprovalHistory);

public sealed record ProjectHistoryEntryDto(
    string Timestamp,
    string UserName,
    string ActionType,
    string FieldName,
    string PreviousValue,
    string NewValue);

public sealed record MinistryDetailDto(
    string MinistryId,
    string MinistryName,
    string Acronym,
    string DirectorName,
    string ContactEmail,
    int ExpertsCount,
    int ProjectsCount,
    int AverageKpi,
    string DemoAccessCode,
    IReadOnlyList<ProjectBoardMiniDto> Projects,
    IReadOnlyList<ExpertMiniDto> Experts);

public sealed record ProjectBoardMiniDto(
    string ProjectId,
    string Title,
    string Status,
    int Kpi,
    int Progress);

public sealed record ExpertMiniDto(
    string ExpertId,
    string FullName,
    string Email,
    string RoleTitle);
