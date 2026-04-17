namespace Innovation4Albania.Domain.Entities;

public sealed class ProjectDocument
{
    public required string Id { get; init; }
    public required string ProjectId { get; init; }
    public required string Name { get; init; }
    public required string FileType { get; init; }
    public required string UploadedBy { get; init; }
    public required DateOnly UploadedOn { get; init; }
}
