namespace Innovation4Albania.Domain.Entities;

public sealed class ProjectOkrLink
{
    public required string ProjectId { get; init; }
    public required string KeyResultId { get; init; }
    public int ContributionWeight { get; set; } = 100;
}
