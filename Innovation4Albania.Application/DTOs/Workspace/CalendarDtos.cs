namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record CalendarEventDto(
    string Id,
    string EventType,
    string Title,
    string MinistryId,
    string MinistryName,
    string MinistryAcronym,
    string Status,
    string ApprovalStage,
    string DueDate,
    string Color,
    bool IsWorkflow,
    string ProjectId,
    string OwnerName,
    int Kpi,
    int Progress);

public sealed record NotificationDto(
    string Id,
    string Type,
    string Title,
    string Message,
    string? ProjectId,
    bool IsRead,
    string CreatedOn,
    string RelativeTime);

public sealed record ImportLogDto(
    string Id,
    string FileName,
    int TotalRows,
    int SuccessfulRows,
    int FailedRows,
    string CreatedOn,
    string SuccessRate);

public sealed record ApprovalHistoryDto(
    string Id,
    string StageFrom,
    string StageTo,
    string Action,
    string ActorName,
    string? Comment,
    string DigitalSignature,
    string CreatedOn);

public sealed record ImportPreviewRowDto(
    int RowNumber,
    bool IsValid,
    string ErrorMessage,
    string Title,
    string MinistryName,
    string Description,
    string StartDate,
    string DueDate,
    string Kpi,
    string OwnerName,
    string Status);

public sealed record ImportPreviewDto(
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<ImportPreviewRowDto> Rows);

public sealed class ApprovalActionRequestDto
{
    public required string ProjectId { get; set; }
    public required string Action { get; set; }
    public string? Comment { get; set; }
}

public sealed class ImportPreviewRequestDto
{
    public required string FileName { get; set; }
    public required string FileContentBase64 { get; set; }
}

public sealed class ConfirmImportRequestDto
{
    public required string FileName { get; set; }
    public required string FileContentBase64 { get; set; }
    public bool SendEmailNotifications { get; set; }
}
