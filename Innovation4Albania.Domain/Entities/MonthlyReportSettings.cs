namespace Innovation4Albania.Domain.Entities;

public sealed class MonthlyReportSettings
{
    public required bool IsEnabled { get; set; }
    public DateTime? LastSentUtc { get; set; }
    public required int LastRecipientCount { get; set; }
}
