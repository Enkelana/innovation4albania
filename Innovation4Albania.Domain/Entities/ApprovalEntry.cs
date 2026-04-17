namespace Innovation4Albania.Domain.Entities;

public sealed class ApprovalEntry
{
    public required string Id { get; init; }
    public required string ProjectId { get; init; }
    public required string StageFrom { get; init; }
    public required string StageTo { get; init; }
    public required string Action { get; init; }
    public required string ActorId { get; init; }
    public required string ActorName { get; init; }
    public string? Comment { get; init; }
    public required string DigitalSignature { get; init; }
    public required DateTime CreatedUtc { get; init; }
}
