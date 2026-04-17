namespace Innovation4Albania.Application.DTOs.Auth;

public sealed record SessionUserDto(
    string Id,
    string FullName,
    string Email,
    string Role,
    string RoleLabel,
    string? MinistryId);
