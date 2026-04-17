namespace Innovation4Albania.Domain.Entities;

public sealed class HistoryLog
{
    public required string Id { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public required string UserName { get; init; }
    public required string ActionType { get; init; }
    public required string FieldName { get; init; }
    public required string PreviousValue { get; init; }
    public required string NewValue { get; init; }
    public required string ProjectId { get; init; }
}
