using Innovation4Albania.Domain.Enums;

namespace Innovation4Albania.Domain.Entities;

public sealed class InnovationProject
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string MinistryId { get; init; }
    public required ProjectStatus Status { get; set; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly DueDate { get; set; }
    public required int Kpi { get; set; }
    public required string OwnerName { get; set; }
    public string Description { get; set; } = string.Empty;
    public required ApprovalStage ApprovalStage { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
    public required int Progress { get; set; }
}
