namespace Innovation4Albania.Application.DTOs.Dashboard;

public sealed record RoleCapabilitiesDto(
    bool CanEditProjects,
    bool CanManageExperts,
    bool CanConfigureAlerts,
    bool CanUploadDocuments,
    bool CanViewAuditLogs,
    bool CanExportReports);
