namespace Innovation4Albania.Domain.Entities;

public sealed class Ministry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Acronym { get; init; }
    public required string DirectorName { get; init; }
    public required string ContactEmail { get; init; }
    public required string AccessCodeHash { get; init; }
}
