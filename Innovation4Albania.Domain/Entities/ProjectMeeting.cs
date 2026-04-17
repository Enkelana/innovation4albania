namespace Innovation4Albania.Domain.Entities;

public sealed class ProjectMeeting
{
    public required string Id { get; init; }
    public required string ProjectId { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public string MeetingUrl { get; set; } = string.Empty;
    public string Platform { get; set; } = "google_meet";
    public required DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public required string CreatedByUserId { get; set; }
    public List<string> AttendeeUserIds { get; set; } = [];
    public string Status { get; set; } = "scheduled";
    public string? Notes { get; set; }
    public string? RecordingUrl { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
