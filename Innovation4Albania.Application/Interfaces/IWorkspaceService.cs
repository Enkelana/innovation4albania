using Innovation4Albania.Application.DTOs.Workspace;
using Innovation4Albania.Application.DTOs.Reporting;

namespace Innovation4Albania.Application.Interfaces;

public interface IWorkspaceService
{
    WorkspaceBootstrapDto? GetWorkspace(string userId);
    ProjectDetailDto? GetProjectDetail(string userId, string projectId);
    PortfolioKpiSnapshotDto? GetPortfolioKpiSnapshot(string userId);
    PortfolioPeriodicReportDto? GetPortfolioDailyReport(string userId);
    PortfolioPeriodicReportDto? GetPortfolioWeeklyReport(string userId);
    PortfolioMonthlyReportDto? GetPortfolioMonthlyReport(string userId);
    ImportPreviewDto PreviewImport(string userId, ImportPreviewRequestDto request);
    OperationResultDto SaveProject(string userId, UpsertProjectRequestDto request);
    OperationResultDto ApplyApprovalAction(string userId, ApprovalActionRequestDto request);
    OperationResultDto SaveExpert(string userId, UpsertExpertRequestDto request);
    OperationResultDto DeleteExpert(string userId, string expertId);
    OperationResultDto AddDocument(string userId, CreateDocumentRequestDto request);
    OperationResultDto DeleteDocument(string userId, string documentId);
    OperationResultDto SaveWorkflowStep(string userId, UpsertWorkflowStepRequestDto request);
    OperationResultDto DeleteWorkflowStep(string userId, string workflowStepId);
    OperationResultDto AddNote(string userId, CreateProjectNoteRequestDto request);
    OperationResultDto DeleteNote(string userId, string noteId);
    OperationResultDto SaveMeeting(string userId, SaveMeetingRequestDto request);
    OperationResultDto CompleteMeeting(string userId, string meetingId, CompleteMeetingRequestDto request);
    OperationResultDto DeleteMeeting(string userId, string meetingId);
    OperationResultDto SaveTask(string userId, SaveTaskRequestDto request);
    OperationResultDto DeleteTask(string userId, string taskId);
    OperationResultDto SaveTaskComment(string userId, SaveTaskCommentRequestDto request);
    OperationResultDto DeleteTaskComment(string userId, string taskCommentId);
    OperationResultDto CertifyMilestone(string userId, CertifyMilestoneRequestDto request);
    OperationResultDto SaveProjectPhoto(string userId, SaveProjectPhotoRequestDto request);
    OperationResultDto DeleteProjectPhoto(string userId, string photoId);
    OperationResultDto SaveOkr(string userId, SaveOkrRequestDto request);
    OperationResultDto LinkProjectToOkr(string userId, LinkProjectOkrRequestDto request);
    OperationResultDto MarkNotificationsAsRead(string userId, string? notificationId);
    OperationResultDto ClearReadNotifications(string userId);
    OperationResultDto ConfirmImport(string userId, ConfirmImportRequestDto request);
    OperationResultDto UpdateAlertSettings(string userId, UpdateAlertSettingsRequestDto request);
    MonthlyReportPreviewDto? GetMonthlyReportPreview(string userId);
    OperationResultDto UpdateMonthlyReportSettings(string userId, UpdateMonthlyReportSettingsRequestDto request);
    OperationResultDto SendMonthlyReportNow(string userId);
    OperationResultDto UpdateMinistryAccessCode(string userId, string ministryId, string accessCode);
    OperationResultDto RefreshSync(string userId);
}
