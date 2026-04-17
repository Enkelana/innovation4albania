namespace Innovation4Albania.Domain.Entities;

public sealed class ProjectMilestone
{
    public required string Id { get; init; }
    public required string ProjectId { get; set; }
    public int TargetPercent { get; set; }
    public DateTime? AchievedAtUtc { get; set; }
    public string? CertifiedByUserId { get; set; }
    public string? CertifiedByName { get; set; }
    public string? CertificateContent { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
