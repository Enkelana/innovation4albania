namespace Innovation4Albania.Domain.Entities;

public sealed class ProjectTask
{
    public required string Id { get; init; }
    public required string ProjectId { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "todo";
    public string Priority { get; set; } = "medium";
    public string? AssigneeUserId { get; set; }
    public DateOnly? Deadline { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }
    public List<string> Tags { get; set; } = [];
    public int Position { get; set; }
    public required string CreatedByUserId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
