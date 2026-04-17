namespace Innovation4Albania.Application.DTOs.Auth;

public sealed record ExpertAccessMinistryDto(
    string Id,
    string Name,
    string Acronym,
    string DemoCode);

public sealed record ExpertAccessGateDto(
    string UserId,
    string UserFullName,
    string AssignedMinistryId,
    IReadOnlyList<ExpertAccessMinistryDto> Ministries);

public sealed record VerifyExpertAccessRequestDto(
    string UserId,
    string MinistryId,
    string AccessCode);

public sealed record VerifyExpertAccessResultDto(
    bool Success,
    string Message,
    string? VerifiedMinistryId = null);
