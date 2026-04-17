namespace Innovation4Albania.Domain.Entities;

public sealed class Expert
{
    public required string Id { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string MinistryId { get; init; }
    public required string RoleTitle { get; init; }
}
