namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed class UpsertExpertRequestDto
{
    public string? Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string MinistryId { get; set; }
    public required string RoleTitle { get; set; }
    public string? AccessCode { get; set; }
}
