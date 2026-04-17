using Innovation4Albania.Domain.Enums;

namespace Innovation4Albania.Domain.Entities;

public sealed class PlatformUser
{
    public required string Id { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required UserRole Role { get; init; }
    public string? MinistryId { get; init; }
    public required string RoleLabel { get; init; }
}
