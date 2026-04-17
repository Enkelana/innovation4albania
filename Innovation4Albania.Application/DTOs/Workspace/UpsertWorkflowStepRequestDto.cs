namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed class UpsertWorkflowStepRequestDto
{
    public string? Id { get; set; }
    public required string ProjectId { get; set; }
    public required int StepNumber { get; set; }
    public required string Description { get; set; }
    public required string Status { get; set; }
    public required string DueDate { get; set; }
    public required string OwnerName { get; set; }
    public required int Progress { get; set; }
}
