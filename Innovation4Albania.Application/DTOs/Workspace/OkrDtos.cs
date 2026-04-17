namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record OkrDto(
    string Id,
    string MinistryId,
    string MinistryName,
    string Title,
    string Description,
    string Period,
    string OwnerName,
    int ProgressPercent,
    IReadOnlyList<KeyResultDto> KeyResults);

public sealed record KeyResultDto(
    string Id,
    string OkrId,
    string Title,
    decimal TargetValue,
    decimal CurrentValue,
    string Unit,
    int ProgressPercent,
    IReadOnlyList<ProjectOkrLinkDto> LinkedProjects);

public sealed record ProjectOkrLinkDto(
    string ProjectId,
    string ProjectTitle,
    int ContributionWeight);

public sealed class SaveOkrRequestDto
{
    public string? Id { get; set; }
    public required string MinistryId { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Period { get; set; }
    public List<SaveKeyResultRequestDto> KeyResults { get; set; } = [];
}

public sealed class SaveKeyResultRequestDto
{
    public string? Id { get; set; }
    public required string Title { get; set; }
    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = "%";
}

public sealed class LinkProjectOkrRequestDto
{
    public required string ProjectId { get; set; }
    public required string KeyResultId { get; set; }
    public int ContributionWeight { get; set; } = 100;
}
