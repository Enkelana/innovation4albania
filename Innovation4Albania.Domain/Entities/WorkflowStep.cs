namespace Innovation4Albania.Domain.Entities;

public sealed class WorkflowStep
{
    public required string Id { get; init; }
    public required string ProjectId { get; init; }
    public required int StepNumber { get; init; }
    public required string Description { get; init; }
    public required string Status { get; set; }
    public required DateOnly DueDate { get; set; }
    public required string OwnerName { get; set; }
    public required int Progress { get; set; }
}
