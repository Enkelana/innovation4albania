namespace Innovation4Albania.Domain.Entities;

public sealed class ImportLog
{
    public required string Id { get; init; }
    public required string ImportedByUserId { get; init; }
    public required string ImportedByName { get; init; }
    public required string FileName { get; init; }
    public required int TotalRows { get; init; }
    public required int SuccessfulRows { get; init; }
    public required int FailedRows { get; init; }
    public required string ErrorsJson { get; init; }
    public required DateTime CreatedUtc { get; init; }
}
