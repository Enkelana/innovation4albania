namespace Innovation4Albania.Application.DTOs.Dashboard;

public sealed record TimelinePointDto(
    string MonthLabel,
    int OpenedProjects,
    int ClosedProjects);
