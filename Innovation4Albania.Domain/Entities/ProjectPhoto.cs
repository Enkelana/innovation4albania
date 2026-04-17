namespace Innovation4Albania.Domain.Entities;

public sealed class ProjectPhoto
{
    public required string Id { get; init; }
    public required string ProjectId { get; set; }
    public required string FileUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Caption { get; set; }
    public string? Location { get; set; }
    public DateOnly? TakenOn { get; set; }
    public required string UploadedByUserId { get; set; }
    public required string UploadedByName { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
