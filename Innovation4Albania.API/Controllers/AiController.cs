using System.Text;
using Innovation4Albania.Application.DTOs.Workspace;
using Innovation4Albania.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Innovation4Albania.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AiController(IAiWorkspaceService aiService) : ControllerBase
{
    [HttpGet("projects/{projectId}/risk")]
    public IActionResult GetProjectRisk([FromQuery] string userId, string projectId, [FromQuery] bool forceRefresh = false)
    {
        var result = aiService.GetProjectRiskAnalysis(userId, projectId, forceRefresh);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("risk-heatmap")]
    public IActionResult GetRiskHeatmap([FromQuery] string userId) => Ok(aiService.GetRiskHeatmap(userId));

    [HttpGet("smart-alerts")]
    public IActionResult GetSmartAlerts([FromQuery] string userId, [FromQuery] bool forceRefresh = false) =>
        Ok(aiService.GetSmartAlerts(userId, forceRefresh));

    [HttpPost("projects/{projectId}/summary")]
    public IActionResult GenerateProjectSummary([FromQuery] string userId, string projectId, [FromQuery] bool forceRefresh = false)
    {
        var result = aiService.GenerateProjectSummary(userId, projectId, forceRefresh);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("pdf-history")]
    public IActionResult GetPdfHistory([FromQuery] string userId) => Ok(aiService.GetPdfExtractionHistory(userId));

    [HttpPost("pdf-extract")]
    public IActionResult ExtractPdf([FromQuery] string userId, [FromBody] PdfExtractionRequestDto request) =>
        Ok(aiService.ExtractProjectFromPdf(userId, request));

    [HttpPost("chat")]
    public IActionResult Chat([FromQuery] string userId, [FromBody] AiChatRequestDto request) =>
        Ok(aiService.SendChatMessage(userId, request));

    [HttpGet("reports/at-risk")]
    public IActionResult ExportAtRiskReport([FromQuery] string userId)
    {
        var report = aiService.GenerateBulkAtRiskReport(userId);
        return report is null
            ? NotFound()
            : Content(report.Html, "text/html; charset=utf-8", Encoding.UTF8);
    }
}
