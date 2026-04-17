namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record ExpertDto(
    string Id,
    string FullName,
    string Email,
    string MinistryId,
    string MinistryName,
    string RoleTitle,
    string DemoAccessCode);
