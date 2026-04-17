namespace Innovation4Albania.Domain.Entities;

public sealed class KeyResult
{
    public required string Id { get; init; }
    public required string OkrId { get; set; }
    public required string Title { get; set; }
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public string Unit { get; set; } = "%";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
