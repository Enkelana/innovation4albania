namespace Innovation4Albania.Domain.Entities;

public sealed class ProjectNote
{
    public required string Id { get; init; }
    public required string ProjectId { get; init; }
    public required string AuthorName { get; init; }
    public required string AuthorUserId { get; init; }
    public required string Content { get; set; }
    public required bool IsPrivate { get; set; }
    public required DateTime CreatedUtc { get; init; }
}
