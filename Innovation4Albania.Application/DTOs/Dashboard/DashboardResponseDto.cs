using Innovation4Albania.Application.DTOs.Auth;

namespace Innovation4Albania.Application.DTOs.Dashboard;

public sealed record DashboardResponseDto(
    SessionUserDto CurrentUser,
    OverviewCardDto Overview,
    IReadOnlyList<MinistryBoardItemDto> MinistryBoard,
    IReadOnlyList<ProjectBoardItemDto> Projects,
    IReadOnlyList<AlertDto> Alerts,
    IReadOnlyList<TimelinePointDto> Timeline,
    IReadOnlyList<WorkflowSnapshotDto> WorkflowSnapshots,
    IReadOnlyList<HistoryLogDto> HistoryLogs,
    RoleCapabilitiesDto Capabilities);
