using System.Text;
using System.IO.Compression;
using Innovation4Albania.Application.DTOs.Workspace;
using Innovation4Albania.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Innovation4Albania.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WorkspaceController(IWorkspaceService workspaceService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetWorkspace([FromQuery] string userId)
    {
        var workspace = workspaceService.GetWorkspace(userId);
        return workspace is null ? NotFound() : Ok(workspace);
    }

    [HttpGet("projects/{projectId}")]
    public IActionResult GetProjectDetail([FromQuery] string userId, string projectId)
    {
        var project = workspaceService.GetProjectDetail(userId, projectId);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost("projects/approval")]
    public IActionResult ApplyApproval([FromQuery] string userId, [FromBody] ApprovalActionRequestDto request) =>
        Ok(workspaceService.ApplyApprovalAction(userId, request));

    [HttpPost("projects")]
    public IActionResult SaveProject([FromQuery] string userId, [FromBody] UpsertProjectRequestDto request) =>
        Ok(workspaceService.SaveProject(userId, request));

    [HttpPost("experts")]
    public IActionResult SaveExpert([FromQuery] string userId, [FromBody] UpsertExpertRequestDto request) =>
        Ok(workspaceService.SaveExpert(userId, request));

    [HttpDelete("experts/{expertId}")]
    public IActionResult DeleteExpert([FromQuery] string userId, string expertId) =>
        Ok(workspaceService.DeleteExpert(userId, expertId));

    [HttpPost("documents")]
    public IActionResult AddDocument([FromQuery] string userId, [FromBody] CreateDocumentRequestDto request) =>
        Ok(workspaceService.AddDocument(userId, request));

    [HttpDelete("documents/{documentId}")]
    public IActionResult DeleteDocument([FromQuery] string userId, string documentId) =>
        Ok(workspaceService.DeleteDocument(userId, documentId));

    [HttpPost("workflow")]
    public IActionResult SaveWorkflow([FromQuery] string userId, [FromBody] UpsertWorkflowStepRequestDto request) =>
        Ok(workspaceService.SaveWorkflowStep(userId, request));

    [HttpDelete("workflow/{workflowStepId}")]
    public IActionResult DeleteWorkflow([FromQuery] string userId, string workflowStepId) =>
        Ok(workspaceService.DeleteWorkflowStep(userId, workflowStepId));

    [HttpPost("notes")]
    public IActionResult AddNote([FromQuery] string userId, [FromBody] CreateProjectNoteRequestDto request) =>
        Ok(workspaceService.AddNote(userId, request));

    [HttpDelete("notes/{noteId}")]
    public IActionResult DeleteNote([FromQuery] string userId, string noteId) =>
        Ok(workspaceService.DeleteNote(userId, noteId));

    [HttpPost("meetings")]
    public IActionResult SaveMeeting([FromQuery] string userId, [FromBody] SaveMeetingRequestDto request) =>
        Ok(workspaceService.SaveMeeting(userId, request));

    [HttpPost("meetings/{meetingId}/complete")]
    public IActionResult CompleteMeeting([FromQuery] string userId, string meetingId, [FromBody] CompleteMeetingRequestDto request) =>
        Ok(workspaceService.CompleteMeeting(userId, meetingId, request));

    [HttpDelete("meetings/{meetingId}")]
    public IActionResult DeleteMeeting([FromQuery] string userId, string meetingId) =>
        Ok(workspaceService.DeleteMeeting(userId, meetingId));

    [HttpPost("tasks")]
    public IActionResult SaveTask([FromQuery] string userId, [FromBody] SaveTaskRequestDto request) =>
        Ok(workspaceService.SaveTask(userId, request));

    [HttpDelete("tasks/{taskId}")]
    public IActionResult DeleteTask([FromQuery] string userId, string taskId) =>
        Ok(workspaceService.DeleteTask(userId, taskId));

    [HttpPost("task-comments")]
    public IActionResult SaveTaskComment([FromQuery] string userId, [FromBody] SaveTaskCommentRequestDto request) =>
        Ok(workspaceService.SaveTaskComment(userId, request));

    [HttpDelete("task-comments/{taskCommentId}")]
    public IActionResult DeleteTaskComment([FromQuery] string userId, string taskCommentId) =>
        Ok(workspaceService.DeleteTaskComment(userId, taskCommentId));

    [HttpPost("milestones/certify")]
    public IActionResult CertifyMilestone([FromQuery] string userId, [FromBody] CertifyMilestoneRequestDto request) =>
        Ok(workspaceService.CertifyMilestone(userId, request));

    [HttpPost("photos")]
    public IActionResult SaveProjectPhoto([FromQuery] string userId, [FromBody] SaveProjectPhotoRequestDto request) =>
        Ok(workspaceService.SaveProjectPhoto(userId, request));

    [HttpDelete("photos/{photoId}")]
    public IActionResult DeleteProjectPhoto([FromQuery] string userId, string photoId) =>
        Ok(workspaceService.DeleteProjectPhoto(userId, photoId));

    [HttpGet("photos/project/{projectId}/zip")]
    public IActionResult DownloadProjectPhotosZip([FromQuery] string userId, string projectId)
    {
        var project = workspaceService.GetProjectDetail(userId, projectId);
        if (project is null)
        {
            return NotFound();
        }

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            var index = 1;
            foreach (var photo in project.Photos)
            {
                var safeBaseName = string.IsNullOrWhiteSpace(photo.Caption)
                    ? $"foto-{index:00}"
                    : string.Concat(photo.Caption.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')).Trim('-');

                if (photo.FileUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    var commaIndex = photo.FileUrl.IndexOf(',');
                    if (commaIndex > -1)
                    {
                        var metadata = photo.FileUrl[..commaIndex];
                        var extension = metadata.Contains("image/png", StringComparison.OrdinalIgnoreCase) ? "png"
                            : metadata.Contains("image/webp", StringComparison.OrdinalIgnoreCase) ? "webp"
                            : "jpg";
                        var bytes = Convert.FromBase64String(photo.FileUrl[(commaIndex + 1)..]);
                        var entry = archive.CreateEntry($"{safeBaseName}-{index:00}.{extension}");
                        using var entryStream = entry.Open();
                        entryStream.Write(bytes, 0, bytes.Length);
                    }
                }
                else
                {
                    var entry = archive.CreateEntry($"{safeBaseName}-{index:00}.url.txt");
                    using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                    writer.WriteLine(photo.FileUrl);
                }

                index++;
            }
        }

        stream.Position = 0;
        var fileName = $"{project.Title.Replace(' ', '-')}-galeria.zip";
        return File(stream.ToArray(), "application/zip", fileName);
    }

    [HttpPost("okrs")]
    public IActionResult SaveOkr([FromQuery] string userId, [FromBody] SaveOkrRequestDto request) =>
        Ok(workspaceService.SaveOkr(userId, request));

    [HttpPost("okrs/link")]
    public IActionResult LinkProjectOkr([FromQuery] string userId, [FromBody] LinkProjectOkrRequestDto request) =>
        Ok(workspaceService.LinkProjectToOkr(userId, request));

    [HttpPut("alerts")]
    public IActionResult UpdateAlerts([FromQuery] string userId, [FromBody] UpdateAlertSettingsRequestDto request) =>
        Ok(workspaceService.UpdateAlertSettings(userId, request));

    [HttpGet("reports/monthly/preview")]
    public IActionResult GetMonthlyReportPreview([FromQuery] string userId)
    {
        var preview = workspaceService.GetMonthlyReportPreview(userId);
        return preview is null ? NotFound() : Ok(preview);
    }

    [HttpGet("reports/monthly/export")]
    public IActionResult ExportMonthlyReport([FromQuery] string userId)
    {
        var preview = workspaceService.GetMonthlyReportPreview(userId);
        return preview is null
            ? NotFound()
            : Content(preview.Html, "text/html; charset=utf-8", Encoding.UTF8);
    }

    [HttpPut("reports/monthly/settings")]
    public IActionResult UpdateMonthlyReportSettings([FromQuery] string userId, [FromBody] UpdateMonthlyReportSettingsRequestDto request) =>
        Ok(workspaceService.UpdateMonthlyReportSettings(userId, request));

    [HttpPost("reports/monthly/send")]
    public IActionResult SendMonthlyReport([FromQuery] string userId) =>
        Ok(workspaceService.SendMonthlyReportNow(userId));

    [HttpPut("ministries/{ministryId}/access-code")]
    public IActionResult UpdateMinistryAccessCode([FromQuery] string userId, string ministryId, [FromBody] UpdateMinistryAccessCodeRequest request) =>
        Ok(workspaceService.UpdateMinistryAccessCode(userId, ministryId, request.AccessCode));

    [HttpPost("notifications/read")]
    public IActionResult MarkNotificationsAsRead([FromQuery] string userId, [FromBody] MarkNotificationsReadRequest request) =>
        Ok(workspaceService.MarkNotificationsAsRead(userId, request.NotificationId));

    [HttpDelete("notifications/read")]
    public IActionResult ClearReadNotifications([FromQuery] string userId) =>
        Ok(workspaceService.ClearReadNotifications(userId));

    [HttpPost("sync")]
    public IActionResult RefreshSync([FromQuery] string userId) =>
        Ok(workspaceService.RefreshSync(userId));

    [HttpPost("import/preview")]
    public IActionResult PreviewImport([FromQuery] string userId, [FromBody] ImportPreviewRequestDto request) =>
        Ok(workspaceService.PreviewImport(userId, request));

    [HttpPost("import/confirm")]
    public IActionResult ConfirmImport([FromQuery] string userId, [FromBody] ConfirmImportRequestDto request) =>
        Ok(workspaceService.ConfirmImport(userId, request));

    [HttpGet("export/projects.csv")]
    public IActionResult ExportProjectsCsv([FromQuery] string userId)
    {
        var workspace = workspaceService.GetWorkspace(userId);
        if (workspace is null)
        {
            return NotFound();
        }

        var rows = new StringBuilder();
        rows.AppendLine("ID,Titulli,Ministria,Statusi,Pergjegjesi,KPI,Progresi,DataFillimit,Afati");
        foreach (var project in workspace.Dashboard.Projects)
        {
            rows.AppendLine(string.Join(",", [
                EscapeCsv(project.ProjectId),
                EscapeCsv(project.Title),
                EscapeCsv(project.MinistryName),
                EscapeCsv(project.Status),
                EscapeCsv(project.OwnerName),
                project.Kpi.ToString(),
                project.Progress.ToString(),
                EscapeCsv(project.StartDate.ToString("yyyy-MM-dd")),
                EscapeCsv(project.DueDate.ToString("yyyy-MM-dd"))
            ]));
        }

        return File(Encoding.UTF8.GetBytes(rows.ToString()), "text/csv", "projektet-innovation4albania.csv");
    }

    [HttpGet("export/projects-excel")]
    public IActionResult ExportProjectsExcel([FromQuery] string userId)
    {
        var workspace = workspaceService.GetWorkspace(userId);
        if (workspace is null)
        {
            return NotFound();
        }

        var xml = BuildExcelWorkbook(workspace);
        return File(Encoding.UTF8.GetBytes(xml), "application/vnd.ms-excel", "projektet-innovation4albania.xls");
    }

    [HttpGet("export/view-excel")]
    public IActionResult ExportViewExcel([FromQuery] string userId, [FromQuery] string view, [FromQuery] string? projectId, [FromQuery] string? ministryId)
    {
        var workspace = workspaceService.GetWorkspace(userId);
        if (workspace is null)
        {
            return NotFound();
        }

        var projectDetail = !string.IsNullOrWhiteSpace(projectId)
            ? workspaceService.GetProjectDetail(userId, projectId)
            : null;

        var xml = BuildExcelWorkbookForView(workspace, view, projectDetail, ministryId);
        return File(Encoding.UTF8.GetBytes(xml), "application/vnd.ms-excel", $"innovation4albania-{view}.xls");
    }

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static string BuildExcelWorkbook(WorkspaceBootstrapDto workspace)
    {
        var sb = new StringBuilder();
        sb.Append("""
<?xml version="1.0"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:o="urn:schemas-microsoft-com:office:office"
 xmlns:x="urn:schemas-microsoft-com:office:excel"
 xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
<Worksheet ss:Name="Lista">
<Table>
<Row><Cell><Data ss:Type="String">ID</Data></Cell><Cell><Data ss:Type="String">Titulli</Data></Cell><Cell><Data ss:Type="String">Ministria</Data></Cell><Cell><Data ss:Type="String">Statusi</Data></Cell><Cell><Data ss:Type="String">Pergjegjesi</Data></Cell><Cell><Data ss:Type="Number">KPI</Data></Cell><Cell><Data ss:Type="Number">Progresi</Data></Cell></Row>
""");
        foreach (var project in workspace.Dashboard.Projects)
        {
            sb.Append($"<Row><Cell><Data ss:Type=\"String\">{Xml(project.ProjectId)}</Data></Cell><Cell><Data ss:Type=\"String\">{Xml(project.Title)}</Data></Cell><Cell><Data ss:Type=\"String\">{Xml(project.MinistryName)}</Data></Cell><Cell><Data ss:Type=\"String\">{Xml(project.Status)}</Data></Cell><Cell><Data ss:Type=\"String\">{Xml(project.OwnerName)}</Data></Cell><Cell><Data ss:Type=\"Number\">{project.Kpi}</Data></Cell><Cell><Data ss:Type=\"Number\">{project.Progress}</Data></Cell></Row>");
        }

        sb.Append("""
</Table>
</Worksheet>
<Worksheet ss:Name="Permbledhje">
<Table>
<Row><Cell><Data ss:Type="String">Metrika</Data></Cell><Cell><Data ss:Type="String">Vlera</Data></Cell></Row>
""");

        var overview = workspace.Dashboard.Overview;
        var summaryRows = new Dictionary<string, string>
        {
            ["Ministri aktive"] = overview.ActiveMinistries.ToString(),
            ["Projekte totale"] = overview.TotalProjects.ToString(),
            ["Eksperte"] = overview.TotalExperts.ToString(),
            ["Projekte ne risk"] = overview.RiskProjects.ToString(),
            ["KPI mesatar"] = $"{overview.AverageKpi}%",
            ["Afate ne 14 dite"] = overview.UpcomingDeadlines.ToString()
        };

        foreach (var row in summaryRows)
        {
            sb.Append($"<Row><Cell><Data ss:Type=\"String\">{Xml(row.Key)}</Data></Cell><Cell><Data ss:Type=\"String\">{Xml(row.Value)}</Data></Cell></Row>");
        }

        sb.Append("""
</Table>
</Worksheet>
</Workbook>
""");
        return sb.ToString();
    }

    private static string BuildExcelWorkbookForView(WorkspaceBootstrapDto workspace, string view, ProjectDetailDto? projectDetail, string? ministryId)
    {
        var safeView = string.IsNullOrWhiteSpace(view) ? "overview" : view.Trim().ToLowerInvariant();
        return safeView switch
        {
            "projects" => BuildProjectsWorkbook(workspace),
            "charts" => BuildChartsWorkbook(workspace),
            "project-detail" => BuildProjectDetailWorkbook(projectDetail),
            "ministries" => BuildMinistriesWorkbook(workspace, ministryId),
            "experts" => BuildExpertsWorkbook(workspace),
            "documents" => BuildDocumentsWorkbook(workspace),
            "tasks" => BuildTasksWorkbook(workspace),
            "okrs" => BuildOkrsWorkbook(workspace),
            "workflow" => BuildWorkflowWorkbook(workspace, projectDetail),
            "calendar" => BuildCalendarWorkbook(workspace),
            "alerts" => BuildAlertsWorkbook(workspace),
            "sync" => BuildSyncWorkbook(workspace),
            "notifications" => BuildNotificationsWorkbook(workspace),
            "import" => BuildImportWorkbook(workspace),
            "logs" => BuildLogsWorkbook(workspace),
            _ => BuildOverviewWorkbook(workspace)
        };
    }

    private static string BuildOverviewWorkbook(WorkspaceBootstrapDto workspace)
    {
        var overview = workspace.Dashboard.Overview;
        return WrapWorkbook(
            ("Permbledhje", [
                ["Metrika", "Vlera"],
                ["Ministri aktive", overview.ActiveMinistries.ToString()],
                ["Projekte totale", overview.TotalProjects.ToString()],
                ["Eksperte", overview.TotalExperts.ToString()],
                ["Projekte ne risk", overview.RiskProjects.ToString()],
                ["KPI mesatar", $"{overview.AverageKpi}%"],
                ["Afate ne 14 dite", overview.UpcomingDeadlines.ToString()]
            ]),
            ("Ministrite", workspace.Dashboard.MinistryBoard.Select(item => new[]
            {
                item.MinistryName, item.Acronym, item.TotalProjects.ToString(), item.Experts.ToString(), $"{item.AverageKpi}%"
            }).Prepend(["Ministria", "Shkurtimi", "Projektet", "Ekspertet", "KPI mesatar"]).ToList())
        );
    }

    private static string BuildProjectsWorkbook(WorkspaceBootstrapDto workspace) => BuildExcelWorkbook(workspace);

    private static string BuildChartsWorkbook(WorkspaceBootstrapDto workspace)
    {
        var overview = workspace.Dashboard.Overview;
        var metricRows = new List<string[]>
        {
            new[] { "Metrika", "Vlera" },
            new[] { "KPI mesatar", $"{overview.AverageKpi}%" },
            new[] { "Projekte ne risk", overview.RiskProjects.ToString() },
            new[] { "Afate ne 14 dite", overview.UpcomingDeadlines.ToString() },
            new[] { "Ministri aktive", overview.ActiveMinistries.ToString() }
        };
        var statusRows = workspace.Dashboard.Projects
            .GroupBy(item => item.Status)
            .Select(group => new[] { group.Key, group.Count().ToString() })
            .Prepend(new[] { "Statusi", "Numri" })
            .ToList();
        var ministryRows = workspace.Dashboard.MinistryBoard
            .Select(item => new[]
            {
                item.MinistryName, item.Acronym, $"{item.AverageKpi}%", item.TotalProjects.ToString(), item.HealthStatus
            })
            .Prepend(new[] { "Ministria", "Shkurtimi", "KPI mesatar", "Projektet", "Gjendja" })
            .ToList();

        return WrapWorkbook(
            ("Metrikat", metricRows),
            ("Statuset", statusRows),
            ("Performanca e ministrive", ministryRows)
        );
    }

    private static string BuildProjectDetailWorkbook(ProjectDetailDto? detail)
    {
        if (detail is null)
        {
            return WrapWorkbook(("Detajet", [["Mesazh", "Nuk u gjet projekti"]]));
        }

        return WrapWorkbook(
            ("Projekti", [
                ["Fusha", "Vlera"],
                ["Titulli", detail.Title],
                ["Ministria", detail.MinistryName],
                ["Statusi", detail.Status],
                ["Pergjegjesi", detail.OwnerName],
                ["KPI", detail.Kpi.ToString()],
                ["Progresi", detail.Progress.ToString()],
                ["Fillimi", detail.StartDate],
                ["Afati", detail.DueDate]
            ]),
            ("Workflow", detail.Workflow.Select(step => new[]
            {
                step.StepNumber.ToString(), step.Description, step.Status, step.OwnerName, step.Progress.ToString(), step.DueDate
            }).Prepend(["Hapi", "Pershkrimi", "Statusi", "Pergjegjesi", "Progresi", "Afati"]).ToList()),
            ("Dokumente", detail.Documents.Select(document => new[]
            {
                document.Name, document.FileType, document.UploadedBy, document.UploadedOn
            }).Prepend(["Emri", "Lloji", "Ngarkuar nga", "Data"]).ToList()),
            ("Komente", detail.Notes.Select(note => new[]
            {
                note.AuthorName, note.IsPrivate ? "Po" : "Jo", note.Content, note.CreatedOn
            }).Prepend(["Autori", "Privat", "Permbajtja", "Data"]).ToList()),
            ("Historik", detail.History.Select(log => new[]
            {
                log.Timestamp, log.UserName, log.ActionType, log.FieldName, log.PreviousValue, log.NewValue
            }).Prepend(["Koha", "Perdoruesi", "Veprimi", "Fusha", "Nga", "Ne"]).ToList())
        );
    }

    private static string BuildMinistriesWorkbook(WorkspaceBootstrapDto workspace, string? ministryId)
    {
        var ministries = string.IsNullOrWhiteSpace(ministryId)
            ? workspace.MinistryDetails
            : workspace.MinistryDetails.Where(item => item.MinistryId == ministryId).ToList();

        return WrapWorkbook(
            ("Ministrite", ministries.Select(item => new[]
            {
                item.MinistryName, item.Acronym, item.DirectorName, item.ContactEmail, item.ProjectsCount.ToString(), item.ExpertsCount.ToString(), item.DemoAccessCode
            }).Prepend(["Ministria", "Shkurtimi", "Drejtuesi", "Kontakti", "Projektet", "Ekspertet", "Kodi demo"]).ToList())
        );
    }

    private static string BuildExpertsWorkbook(WorkspaceBootstrapDto workspace) =>
        WrapWorkbook(("Anetaret", workspace.Experts.Select(item => new[]
        {
            item.FullName, item.Email, item.MinistryName, item.RoleTitle, item.DemoAccessCode
        }).Prepend(["Emri", "Email", "Ministria", "Roli", "Kodi demo"]).ToList()));

    private static string BuildDocumentsWorkbook(WorkspaceBootstrapDto workspace) =>
        WrapWorkbook(("Dokumentet", workspace.Documents.Select(item => new[]
        {
            item.ProjectTitle, item.Name, item.FileType, item.UploadedBy, item.UploadedOn
        }).Prepend(["Projekti", "Dokumenti", "Lloji", "Ngarkuar nga", "Data"]).ToList()));

    private static string BuildTasksWorkbook(WorkspaceBootstrapDto workspace) =>
        WrapWorkbook(("Detyrat", workspace.Tasks.Select(item => new[]
        {
            item.ProjectTitle, item.Title, item.Status, item.Priority, item.AssigneeName ?? "-", item.Deadline ?? "-", item.EstimatedHours.ToString(), item.ActualHours.ToString()
        }).Prepend(["Projekti", "Titulli", "Statusi", "Prioriteti", "Pergjegjesi", "Afati", "Ore te planifikuara", "Ore reale"]).ToList()));

    private static string BuildOkrsWorkbook(WorkspaceBootstrapDto workspace)
    {
        var objectiveRows = workspace.Okrs.Select(item => new[]
        {
            item.MinistryName, item.Title, item.Period, item.OwnerName, $"{item.ProgressPercent}%"
        }).Prepend(["Ministria", "Objektivi", "Periudha", "Pronari", "Progresi"]).ToList();

        var keyResultRows = workspace.Okrs
            .SelectMany(item => item.KeyResults.Select(keyResult => new[]
            {
                item.MinistryName,
                item.Title,
                keyResult.Title,
                $"{keyResult.CurrentValue}/{keyResult.TargetValue} {keyResult.Unit}".Trim(),
                $"{keyResult.ProgressPercent}%",
                string.Join(" | ", keyResult.LinkedProjects.Select(link => $"{link.ProjectTitle} ({link.ContributionWeight}%)"))
            }))
            .Prepend(["Ministria", "Objektivi", "Key Result", "Vlera", "Progresi", "Projektet e lidhura"])
            .ToList();

        return WrapWorkbook(
            ("Objektivat", objectiveRows),
            ("Key Results", keyResultRows));
    }

    private static string BuildWorkflowWorkbook(WorkspaceBootstrapDto workspace, ProjectDetailDto? detail)
    {
        var source = detail?.Workflow ?? workspace.WorkflowSteps;
        var rows = source.Select(item => new[]
        {
            item.ProjectTitle, item.StepNumber.ToString(), item.Description, item.Status, item.OwnerName, item.Progress.ToString(), item.DueDate
        }).Prepend(["Projekti", "Hapi", "Pershkrimi", "Statusi", "Pergjegjesi", "Progresi", "Afati"]).ToList();
        return WrapWorkbook(("Workflow", rows));
    }

    private static string BuildCalendarWorkbook(WorkspaceBootstrapDto workspace) =>
        WrapWorkbook(("Kalendar", workspace.CalendarEvents.Select(item => new[]
        {
            item.Title, item.MinistryName, item.MinistryAcronym, item.Status, item.ApprovalStage, item.DueDate, item.OwnerName, item.IsWorkflow ? "Workflow" : "Projekt"
        }).Prepend(["Titulli", "Ministria", "Shkurtimi", "Statusi", "Faza", "Afati", "Pergjegjesi", "Lloji"]).ToList()));

    private static string BuildAlertsWorkbook(WorkspaceBootstrapDto workspace) =>
        WrapWorkbook(("Alertet", workspace.Dashboard.Alerts.Select(item => new[]
        {
            item.Title, item.MinistryName, item.ProjectTitle, item.Severity, item.Message
        }).Prepend(["Titulli", "Ministria", "Projekti", "Niveli", "Mesazhi"]).ToList()));

    private static string BuildSyncWorkbook(WorkspaceBootstrapDto workspace) =>
        WrapWorkbook(("Sinkronizimi", [
            ["Fusha", "Vlera"],
            ["Burimi", workspace.SyncStatus.Source],
            ["Menyra", workspace.SyncStatus.Mode],
            ["Gjendja", workspace.SyncStatus.Health],
            ["Sinkronizimi i fundit", workspace.SyncStatus.LastSync],
            ["Sinkronizimi i ardhshem", workspace.SyncStatus.NextSync]
        ]));

    private static string BuildNotificationsWorkbook(WorkspaceBootstrapDto workspace) =>
        WrapWorkbook(("Njoftimet", workspace.Notifications.Select(item => new[]
        {
            item.Title, item.Message, item.Type, item.IsRead ? "Lexuar" : "Palexuar", item.CreatedOn, item.RelativeTime
        }).Prepend(["Titulli", "Mesazhi", "Lloji", "Gjendja", "Data", "Koha relative"]).ToList()));

    private static string BuildImportWorkbook(WorkspaceBootstrapDto workspace) =>
        WrapWorkbook(("Historiku i importit", workspace.ImportLogs.Select(item => new[]
        {
            item.FileName, item.TotalRows.ToString(), item.SuccessfulRows.ToString(), item.FailedRows.ToString(), item.SuccessRate, item.CreatedOn
        }).Prepend(["Skedari", "Rreshta gjithsej", "Te suksesshem", "Me gabime", "Shkalla e suksesit", "Data"]).ToList()));

    private static string BuildLogsWorkbook(WorkspaceBootstrapDto workspace) =>
        WrapWorkbook(("Historiku", workspace.Dashboard.HistoryLogs.Select(item => new[]
        {
            item.Timestamp, item.UserName, item.ActionType, item.FieldName, item.ChangeSummary, item.ProjectId
        }).Prepend(["Koha", "Perdoruesi", "Veprimi", "Fusha", "Permbledhja", "Projekti"]).ToList()));

    private static string WrapWorkbook(params (string Name, IReadOnlyList<string[]> Rows)[] sheets)
    {
        var sb = new StringBuilder();
        sb.Append("""
<?xml version="1.0"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:o="urn:schemas-microsoft-com:office:office"
 xmlns:x="urn:schemas-microsoft-com:office:excel"
 xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
""");

        foreach (var sheet in sheets)
        {
            sb.Append($"<Worksheet ss:Name=\"{Xml(sheet.Name)}\"><Table>");
            foreach (var row in sheet.Rows)
            {
                sb.Append("<Row>");
                foreach (var cell in row)
                {
                    sb.Append($"<Cell><Data ss:Type=\"String\">{Xml(cell)}</Data></Cell>");
                }
                sb.Append("</Row>");
            }
            sb.Append("</Table></Worksheet>");
        }

        sb.Append("</Workbook>");
        return sb.ToString();
    }

    private static string Xml(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
}

public sealed class UpdateMinistryAccessCodeRequest
{
    public required string AccessCode { get; set; }
}

public sealed class MarkNotificationsReadRequest
{
    public string? NotificationId { get; set; }
}
