namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record DocumentDto(
    string Id,
    string ProjectId,
    string ProjectTitle,
    string Name,
    string FileType,
    string UploadedBy,
    string UploadedOn);
