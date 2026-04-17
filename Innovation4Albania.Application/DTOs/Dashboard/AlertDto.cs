namespace Innovation4Albania.Application.DTOs.Dashboard;

public sealed record AlertDto(
    string Id,
    string Severity,
    string Title,
    string MinistryName,
    string ProjectTitle,
    string Message);
