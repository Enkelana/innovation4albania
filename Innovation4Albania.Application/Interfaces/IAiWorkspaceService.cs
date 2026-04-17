using Innovation4Albania.Application.DTOs.Workspace;

namespace Innovation4Albania.Application.Interfaces;

public interface IAiWorkspaceService
{
    AiRiskAnalysisDto? GetProjectRiskAnalysis(string userId, string projectId, bool forceRefresh = false);
    IReadOnlyList<AiHeatmapItemDto> GetRiskHeatmap(string userId);
    IReadOnlyList<SmartAlertRecommendationDto> GetSmartAlerts(string userId, bool forceRefresh = false);
    AiProjectSummaryDto? GenerateProjectSummary(string userId, string projectId, bool forceRefresh = false);
    PdfExtractionResultDto ExtractProjectFromPdf(string userId, PdfExtractionRequestDto request);
    IReadOnlyList<PdfExtractionHistoryDto> GetPdfExtractionHistory(string userId);
    AiChatResponseDto SendChatMessage(string userId, AiChatRequestDto request);
    BulkAiReportDto? GenerateBulkAtRiskReport(string userId);
}
