namespace Innovation4Albania.Domain.Entities;

public sealed class TaskComment
{
    public required string Id { get; init; }
    public required string TaskId { get; set; }
    public required string AuthorUserId { get; set; }
    public required string AuthorName { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
