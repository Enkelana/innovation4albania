namespace Innovation4Albania.Application.DTOs.Dashboard;

public sealed record WorkflowSnapshotDto(
    string ProjectTitle,
    string MinistryName,
    string StepLabel,
    string StepStatus,
    int Progress,
    DateOnly DueDate);
