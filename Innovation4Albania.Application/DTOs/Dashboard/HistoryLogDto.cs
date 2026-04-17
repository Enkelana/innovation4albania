namespace Innovation4Albania.Application.DTOs.Dashboard;

public sealed record HistoryLogDto(
    string Timestamp,
    string UserName,
    string ActionType,
    string FieldName,
    string ChangeSummary,
    string ProjectId);
