using Innovation4Albania.Application.DTOs.Auth;
using Innovation4Albania.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Innovation4Albania.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("users")]
    public IActionResult GetUsers() => Ok(dashboardService.GetUsers());

    [HttpGet("session")]
    public IActionResult GetSession([FromQuery] string userId)
    {
        var session = dashboardService.GetSession(userId);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpGet("expert-access")]
    public IActionResult GetExpertAccess([FromQuery] string userId)
    {
        var gate = dashboardService.GetExpertAccessGate(userId);
        return gate is null ? NotFound() : Ok(gate);
    }

    [HttpPost("expert-access/verify")]
    public IActionResult VerifyExpertAccess([FromBody] VerifyExpertAccessRequestDto request) =>
        Ok(dashboardService.VerifyExpertAccess(request));
}
