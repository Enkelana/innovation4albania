namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record ProjectMilestoneDto(
    string Id,
    int TargetPercent,
    bool IsAchieved,
    string StatusLabel,
    string? AchievedOn,
    string? CertifiedBy,
    string? Notes,
    string? CertificateContent,
    bool CanCertify);

public sealed class CertifyMilestoneRequestDto
{
    public required string ProjectId { get; set; }
    public int TargetPercent { get; set; }
    public string? Notes { get; set; }
}

public sealed record ProjectPhotoDto(
    string Id,
    string ProjectId,
    string FileUrl,
    string ThumbnailUrl,
    string Caption,
    string Location,
    string? TakenOn,
    string UploadedBy,
    string UploadedOn,
    bool CanDelete);

public sealed class SaveProjectPhotoRequestDto
{
    public string? Id { get; set; }
    public required string ProjectId { get; set; }
    public required string FileUrl { get; set; }
    public string? Caption { get; set; }
    public string? Location { get; set; }
    public string? TakenOn { get; set; }
}
