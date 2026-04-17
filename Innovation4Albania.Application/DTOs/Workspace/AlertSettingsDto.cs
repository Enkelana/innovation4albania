namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record AlertSettingsDto(
    int CriticalKpiThreshold,
    int WarningKpiThreshold,
    int WarningDaysBeforeDeadline,
    string EmailRecipients);
