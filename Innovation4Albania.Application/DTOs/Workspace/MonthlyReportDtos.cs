namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record MonthlyReportStatusDto(
    bool IsEnabled,
    string LastSentOn,
    int LastRecipientCount,
    string NextScheduledRun);

public sealed record MonthlyReportPreviewDto(
    string MonthLabel,
    string Html,
    int RecipientCount);

public sealed record UpdateMonthlyReportSettingsRequestDto(
    bool IsEnabled);
