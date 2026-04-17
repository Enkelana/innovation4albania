using Innovation4Albania.Application.DTOs.Auth;
using Innovation4Albania.Application.DTOs.Dashboard;

namespace Innovation4Albania.Application.Interfaces;

public interface IDashboardService
{
    IReadOnlyList<SessionUserDto> GetUsers();
    SessionUserDto? GetSession(string userId);
    ExpertAccessGateDto? GetExpertAccessGate(string userId);
    VerifyExpertAccessResultDto VerifyExpertAccess(VerifyExpertAccessRequestDto request);
    DashboardResponseDto? GetDashboard(string userId);
}
