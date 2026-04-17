namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record WorkflowStepDto(
    string Id,
    string ProjectId,
    string ProjectTitle,
    int StepNumber,
    string Description,
    string Status,
    string DueDate,
    string OwnerName,
    int Progress);
