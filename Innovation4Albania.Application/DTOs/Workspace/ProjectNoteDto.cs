namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record ProjectNoteDto(
    string Id,
    string ProjectId,
    string ProjectTitle,
    string AuthorName,
    string Content,
    string CreatedOn,
    bool IsPrivate,
    bool CanDelete);
