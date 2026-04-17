using Innovation4Albania.Domain.Enums;

namespace Innovation4Albania.Domain.Entities;

public sealed class PlatformAlert
{
    public required string Id { get; init; }
    public required string ProjectId { get; init; }
    public required string MinistryId { get; init; }
    public required AlertSeverity Severity { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
}
