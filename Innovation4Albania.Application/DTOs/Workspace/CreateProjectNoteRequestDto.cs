namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed class CreateProjectNoteRequestDto
{
    public string? Id { get; set; }
    public required string ProjectId { get; set; }
    public required string Content { get; set; }
    public bool IsPrivate { get; set; }
}
