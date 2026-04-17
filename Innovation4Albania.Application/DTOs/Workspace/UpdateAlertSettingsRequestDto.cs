namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed class UpdateAlertSettingsRequestDto
{
    public required int CriticalKpiThreshold { get; set; }
    public required int WarningKpiThreshold { get; set; }
    public required int WarningDaysBeforeDeadline { get; set; }
    public required string EmailRecipients { get; set; }
}
