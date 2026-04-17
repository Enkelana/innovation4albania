using Innovation4Albania.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Innovation4Albania.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetDashboard([FromQuery] string userId)
    {
        var dashboard = dashboardService.GetDashboard(userId);
        return dashboard is null ? NotFound() : Ok(dashboard);
    }
}
