namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed class UpsertProjectRequestDto
{
    public string? Id { get; set; }
    public required string Title { get; set; }
    public required string MinistryId { get; set; }
    public required string Status { get; set; }
    public required string StartDate { get; set; }
    public required string DueDate { get; set; }
    public required int Kpi { get; set; }
    public required string OwnerName { get; set; }
    public required int Progress { get; set; }
    public string? CancellationReason { get; set; }
}
