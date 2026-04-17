namespace Innovation4Albania.Domain.Entities;

public sealed class OkrObjective
{
    public required string Id { get; init; }
    public required string MinistryId { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Period { get; set; }
    public required string OwnerUserId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
