namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed class CreateDocumentRequestDto
{
    public string? Id { get; set; }
    public required string ProjectId { get; set; }
    public required string Name { get; set; }
    public required string FileType { get; set; }
}
