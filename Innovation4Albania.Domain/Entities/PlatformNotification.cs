namespace Innovation4Albania.Domain.Entities;

public sealed class PlatformNotification
{
    public required string Id { get; init; }
    public required string RecipientId { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? ProjectId { get; init; }
    public required bool IsRead { get; set; }
    public required DateTime CreatedUtc { get; init; }
}
