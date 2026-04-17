namespace Innovation4Albania.Domain.Entities;

public sealed class AlertSettings
{
    public required int CriticalKpiThreshold { get; set; }
    public required int WarningKpiThreshold { get; set; }
    public required int WarningDaysBeforeDeadline { get; set; }
    public required string EmailRecipients { get; set; }
}
