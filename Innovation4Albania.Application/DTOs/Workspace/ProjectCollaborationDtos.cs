namespace Innovation4Albania.Application.DTOs.Workspace;

public sealed record MeetingDto(
    string Id,
    string ProjectId,
    string ProjectTitle,
    string Title,
    string Description,
    string MeetingUrl,
    string Platform,
    string ScheduledAtIso,
    string ScheduledAt,
    int DurationMinutes,
    string Status,
    string? Notes,
    string? RecordingUrl,
    IReadOnlyList<string> Attendees,
    bool CanJoin,
    bool CanComplete);

public sealed record TaskDto(
    string Id,
    string ProjectId,
    string ProjectTitle,
    string Title,
    string Description,
    string Status,
    string Priority,
    string? AssigneeUserId,
    string? AssigneeName,
    string? Deadline,
    decimal EstimatedHours,
    decimal ActualHours,
    IReadOnlyList<string> Tags,
    int Position,
    int CommentCount,
    bool CanDelete,
    IReadOnlyList<TaskCommentDto> Comments);

public sealed record TaskCommentDto(
    string Id,
    string TaskId,
    string AuthorName,
    string Content,
    string CreatedOn);

public sealed class SaveMeetingRequestDto
{
    public string? Id { get; set; }
    public required string ProjectId { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public string MeetingUrl { get; set; } = string.Empty;
    public string Platform { get; set; } = "google_meet";
    public required string ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public List<string> AttendeeUserIds { get; set; } = [];
}

public sealed class CompleteMeetingRequestDto
{
    public string Notes { get; set; } = string.Empty;
    public string? RecordingUrl { get; set; }
}

public sealed class SaveTaskRequestDto
{
    public string? Id { get; set; }
    public required string ProjectId { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "todo";
    public string Priority { get; set; } = "medium";
    public string? AssigneeUserId { get; set; }
    public string? Deadline { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }
    public List<string> Tags { get; set; } = [];
    public int Position { get; set; }
}

public sealed class SaveTaskCommentRequestDto
{
    public string? Id { get; set; }
    public required string TaskId { get; set; }
    public required string Content { get; set; }
}
