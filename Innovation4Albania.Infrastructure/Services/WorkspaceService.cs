using System.Text;
using System.Text.Json;
using Innovation4Albania.Application.DTOs.Workspace;
using Innovation4Albania.Application.Interfaces;
using Innovation4Albania.Domain.Entities;
using Innovation4Albania.Domain.Enums;

namespace Innovation4Albania.Infrastructure.Services;

public sealed class WorkspaceService(IPlatformRepository repository, IDashboardService dashboardService) : IWorkspaceService
{
    public WorkspaceBootstrapDto? GetWorkspace(string userId)
    {
        var user = repository.GetUserById(userId);
        var dashboard = dashboardService.GetDashboard(userId);
        if (user is null || dashboard is null)
        {
            return null;
        }

        var visibleProjects = FilterProjects(user).ToList();
        var visibleProjectIds = visibleProjects.Select(project => project.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var visibleMinistries = FilterMinistries(user).ToList();
        var ministryNames = visibleMinistries.ToDictionary(item => item.Id, item => item.Name, StringComparer.OrdinalIgnoreCase);
        var projectNames = visibleProjects.ToDictionary(item => item.Id, item => item.Title, StringComparer.OrdinalIgnoreCase);

        var experts = repository.GetExperts()
            .Where(expert => visibleMinistries.Any(ministry => ministry.Id == expert.MinistryId))
            .OrderBy(expert => expert.FullName)
            .Select(expert => new ExpertDto(
                expert.Id,
                expert.FullName,
                expert.Email,
                expert.MinistryId,
                ministryNames[expert.MinistryId],
                expert.RoleTitle,
                repository.GetDemoAccessCode(expert.MinistryId) ?? "-"))
            .ToList();

        var documents = repository.GetDocuments()
            .Where(document => visibleProjectIds.Contains(document.ProjectId))
            .OrderByDescending(document => document.UploadedOn)
            .Select(document => new DocumentDto(
                document.Id,
                document.ProjectId,
                projectNames[document.ProjectId],
                document.Name,
                document.FileType,
                document.UploadedBy,
                document.UploadedOn.ToString("yyyy-MM-dd")))
            .ToList();

        var workflowSteps = repository.GetWorkflowSteps()
            .Where(step => visibleProjectIds.Contains(step.ProjectId))
            .OrderBy(step => step.DueDate)
            .ThenBy(step => step.StepNumber)
            .Select(step => new WorkflowStepDto(
                step.Id,
                step.ProjectId,
                projectNames[step.ProjectId],
                step.StepNumber,
                step.Description,
                step.Status,
                step.DueDate.ToString("yyyy-MM-dd"),
                step.OwnerName,
                step.Progress))
            .ToList();

        var notes = repository.GetNotes()
            .Where(note => visibleProjectIds.Contains(note.ProjectId))
            .Where(note => !note.IsPrivate || IsDirectorLike(user))
            .Select(note => new ProjectNoteDto(
                note.Id,
                note.ProjectId,
                projectNames[note.ProjectId],
                note.AuthorName,
                note.Content,
                note.CreatedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                note.IsPrivate,
                IsDirectorLike(user) || string.Equals(note.AuthorUserId, user.Id, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var userNames = BuildUserNameMap();
        var meetings = repository.GetMeetings()
            .Where(item => visibleProjectIds.Contains(item.ProjectId))
            .OrderBy(item => item.ScheduledAtUtc)
            .Select(item => MapMeeting(item, projectNames, userNames, user))
            .ToList();
        var tasks = repository.GetTasks()
            .Where(item => visibleProjectIds.Contains(item.ProjectId))
            .OrderBy(item => item.Status)
            .ThenBy(item => item.Position)
            .Select(item => MapTask(item, projectNames, userNames, user))
            .ToList();
        var okrs = BuildVisibleOkrs(user, visibleMinistries, visibleProjects);

        var ministryDetails = visibleMinistries
            .Select(ministry =>
            {
                var ministryProjects = visibleProjects.Where(project => project.MinistryId == ministry.Id).ToList();
                var ministryExperts = repository.GetExperts().Where(expert => expert.MinistryId == ministry.Id).ToList();
                return new MinistryDetailDto(
                    ministry.Id,
                    ministry.Name,
                    ministry.Acronym,
                    ministry.DirectorName,
                    ministry.ContactEmail,
                    ministryExperts.Count,
                    ministryProjects.Count,
                    ministryProjects.Count == 0 ? 0 : (int)Math.Round(ministryProjects.Average(project => project.Kpi)),
                    repository.GetDemoAccessCode(ministry.Id) ?? "-",
                    ministryProjects.Select(project => new ProjectBoardMiniDto(project.Id, project.Title, project.Status.ToString(), project.Kpi, project.Progress)).ToList(),
                    ministryExperts.Select(expert => new ExpertMiniDto(expert.Id, expert.FullName, expert.Email, expert.RoleTitle)).ToList());
            })
            .ToList();

        var calendarEvents = BuildCalendarEvents(visibleProjects, workflowSteps, meetings, visibleMinistries);
        var notifications = repository.GetNotifications()
            .Where(item => string.Equals(item.RecipientId, user.Id, StringComparison.OrdinalIgnoreCase))
            .Select(MapNotification)
            .ToList();
        var importLogs = repository.GetImportLogs()
            .Where(item => IsDirectorLike(user) || string.Equals(item.ImportedByUserId, user.Id, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .Select(item => new ImportLogDto(
                item.Id,
                item.FileName,
                item.TotalRows,
                item.SuccessfulRows,
                item.FailedRows,
                item.CreatedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                item.TotalRows == 0 ? "0%" : $"{(int)Math.Round((double)item.SuccessfulRows / item.TotalRows * 100)}%"))
            .ToList();
        var settings = repository.GetAlertSettings();
        var sync = repository.GetSyncStatus();
        var monthlyReports = repository.GetMonthlyReportSettings();

        return new WorkspaceBootstrapDto(
            dashboard,
            visibleMinistries.Select(item => new MinistryOptionDto(item.Id, item.Name, item.Acronym)).ToList(),
            experts,
            documents,
            workflowSteps,
            notes,
            meetings,
            tasks,
            okrs,
            ministryDetails,
            calendarEvents,
            notifications,
            importLogs,
            new AlertSettingsDto(settings.CriticalKpiThreshold, settings.WarningKpiThreshold, settings.WarningDaysBeforeDeadline, settings.EmailRecipients),
            new SyncStatusDto(sync.Source, sync.Mode, sync.Health, sync.LastSyncUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"), sync.NextSyncUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm")),
            new MonthlyReportStatusDto(
                monthlyReports.IsEnabled,
                monthlyReports.LastSentUtc?.ToLocalTime().ToString("dd MMM yyyy HH:mm") ?? "-",
                monthlyReports.LastRecipientCount,
                NextMonthlyRun().ToString("dd MMM yyyy HH:mm")));
    }

    public ProjectDetailDto? GetProjectDetail(string userId, string projectId)
    {
        var user = repository.GetUserById(userId);
        if (user is null)
        {
            return null;
        }

        var project = FilterProjects(user).FirstOrDefault(item => item.Id == projectId);
        if (project is null)
        {
            return null;
        }

        var ministry = repository.GetMinistries().First(item => item.Id == project.MinistryId);
        var workflow = repository.GetWorkflowSteps()
            .Where(step => step.ProjectId == projectId)
            .OrderBy(step => step.StepNumber)
            .Select(step => new WorkflowStepDto(step.Id, step.ProjectId, project.Title, step.StepNumber, step.Description, step.Status, step.DueDate.ToString("yyyy-MM-dd"), step.OwnerName, step.Progress))
            .ToList();

        var documents = repository.GetDocuments()
            .Where(document => document.ProjectId == projectId)
            .OrderByDescending(document => document.UploadedOn)
            .Select(document => new DocumentDto(document.Id, document.ProjectId, project.Title, document.Name, document.FileType, document.UploadedBy, document.UploadedOn.ToString("yyyy-MM-dd")))
            .ToList();

        var notes = repository.GetNotes()
            .Where(note => note.ProjectId == projectId)
            .Where(note => !note.IsPrivate || IsDirectorLike(user))
            .Select(note => new ProjectNoteDto(note.Id, note.ProjectId, project.Title, note.AuthorName, note.Content, note.CreatedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"), note.IsPrivate, IsDirectorLike(user) || note.AuthorUserId == user.Id))
            .ToList();
        var projectNames = repository.GetProjects().ToDictionary(item => item.Id, item => item.Title, StringComparer.OrdinalIgnoreCase);
        var userNames = BuildUserNameMap();
        var meetings = repository.GetMeetings()
            .Where(item => item.ProjectId == projectId)
            .OrderBy(item => item.ScheduledAtUtc)
            .Select(item => MapMeeting(item, projectNames, userNames, user))
            .ToList();
        var tasks = repository.GetTasks()
            .Where(item => item.ProjectId == projectId)
            .OrderBy(item => item.Status)
            .ThenBy(item => item.Position)
            .Select(item => MapTask(item, projectNames, userNames, user))
            .ToList();
        var milestones = repository.GetProjectMilestones()
            .Where(item => item.ProjectId == projectId)
            .OrderBy(item => item.TargetPercent)
            .Select(item => MapMilestone(item, project, user))
            .ToList();
        var photos = repository.GetProjectPhotos()
            .Where(item => item.ProjectId == projectId)
            .OrderByDescending(item => item.UploadedAtUtc)
            .Select(item => MapProjectPhoto(item, user))
            .ToList();
        var okrLinks = BuildProjectOkrLinks(projectId, project.Title);

        var history = repository.GetHistoryLogs()
            .Where(log => log.ProjectId == projectId)
            .Take(40)
            .Select(log => new ProjectHistoryEntryDto(log.TimestampUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"), log.UserName, log.ActionType, log.FieldName, log.PreviousValue, log.NewValue))
            .ToList();

        var approvals = repository.GetApprovalEntries()
            .Where(item => item.ProjectId == projectId)
            .Select(item => new ApprovalHistoryDto(
                item.Id,
                item.StageFrom,
                item.StageTo,
                item.Action,
                item.ActorName,
                item.Comment,
                item.DigitalSignature,
                item.CreatedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm")))
            .ToList();

        return new ProjectDetailDto(
            project.Id,
            project.Title,
            project.MinistryId,
            ministry.Name,
            project.Status.ToString(),
            project.OwnerName,
            project.Kpi,
            project.Progress,
            project.Description,
            ApprovalStageShqip(project.ApprovalStage),
            project.StartDate.ToString("yyyy-MM-dd"),
            project.DueDate.ToString("yyyy-MM-dd"),
            GetRiskLevel(project),
            project.CancellationReason,
            project.RejectionReason,
            workflow,
            documents,
            notes,
            meetings,
            tasks,
            milestones,
            photos,
            okrLinks,
            history,
            approvals);
    }

    public ImportPreviewDto PreviewImport(string userId, ImportPreviewRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null || !IsDirectorLike(actor))
        {
            return new ImportPreviewDto(0, 0, 0, []);
        }

        return ParseImportPreview(request.FileContentBase64);
    }

    public OperationResultDto ApplyApprovalAction(string userId, ApprovalActionRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var project = repository.GetProjects().FirstOrDefault(item => item.Id == request.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Projekti nuk u gjet.");
        }

        var action = request.Action.Trim().ToLowerInvariant();
        var currentStage = project.ApprovalStage;
        var targetStage = currentStage;
        var targetStatus = project.Status;
        string successMessage;

        switch (action)
        {
            case "submit" when actor.Role == UserRole.Expert && currentStage == ApprovalStage.Draft:
                targetStage = ApprovalStage.UnderReview;
                successMessage = "Projekti u dergua per shqyrtim.";
                Notify("director", "project_submitted", "Projekt ne shqyrtim", $"Projekti {project.Title} u dergua per shqyrtim.", project.Id);
                break;
            case "approve" when IsDirectorLike(actor) && currentStage == ApprovalStage.UnderReview:
                targetStage = ApprovalStage.Approved;
                successMessage = "Projekti u miratua.";
                NotifyOwner(project, "project_approved", "Projekti u miratua", $"Projekti {project.Title} u miratua.");
                break;
            case "reject" when IsDirectorLike(actor) && currentStage == ApprovalStage.UnderReview:
                if (string.IsNullOrWhiteSpace(request.Comment) || request.Comment.Trim().Length < 20)
                {
                    return new OperationResultDto(false, "Arsyeja e refuzimit duhet te kete te pakten 20 karaktere.");
                }
                targetStage = ApprovalStage.Draft;
                successMessage = "Projekti u kthye ne draft.";
                NotifyOwner(project, "project_rejected", "Projekti u refuzua", request.Comment.Trim());
                break;
            case "activate" when IsDirectorLike(actor) && currentStage == ApprovalStage.Approved:
                targetStage = ApprovalStage.Active;
                targetStatus = ProjectStatus.Active;
                successMessage = "Projekti u aktivizua.";
                NotifyOwner(project, "project_activated", "Projekti u aktivizua", $"Projekti {project.Title} eshte aktiv.");
                break;
            case "complete" when IsDirectorLike(actor) && currentStage == ApprovalStage.Active:
                targetStage = ApprovalStage.Completed;
                targetStatus = ProjectStatus.Completed;
                successMessage = "Projekti u perfundua.";
                break;
            case "cancel" when IsDirectorLike(actor):
                targetStage = ApprovalStage.Cancelled;
                targetStatus = ProjectStatus.Cancelled;
                successMessage = "Projekti u anulua.";
                break;
            default:
                return new OperationResultDto(false, "Ky veprim nuk lejohet ne fazen aktuale.");
        }

        repository.SaveProject(new InnovationProject
        {
            Id = project.Id,
            Title = project.Title,
            MinistryId = project.MinistryId,
            Status = targetStatus,
            ApprovalStage = targetStage,
            Description = project.Description,
            RejectionReason = action == "reject" ? request.Comment?.Trim() : project.RejectionReason,
            StartDate = project.StartDate,
            DueDate = project.DueDate,
            Kpi = project.Kpi,
            OwnerName = project.OwnerName,
            CancellationReason = action == "cancel" ? (request.Comment?.Trim() ?? "Anuluar") : project.CancellationReason,
            Progress = project.Progress
        }, actor.FullName, false);

        repository.AddApprovalEntry(new ApprovalEntry
        {
            Id = $"APR-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            ProjectId = project.Id,
            StageFrom = ApprovalStageShqip(currentStage),
            StageTo = ApprovalStageShqip(targetStage),
            Action = action,
            ActorId = actor.Id,
            ActorName = actor.FullName,
            Comment = request.Comment?.Trim(),
            DigitalSignature = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{actor.Id}:{project.Id}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}")),
            CreatedUtc = DateTime.UtcNow
        });

        return new OperationResultDto(true, successMessage);
    }

    public OperationResultDto SaveProject(string userId, UpsertProjectRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (!IsDirectorLike(actor))
        {
            return new OperationResultDto(false, "Vetem drejtori mund te krijoje ose ndryshoje projekte.");
        }

        if (!DateOnly.TryParse(request.StartDate, out var startDate) || !DateOnly.TryParse(request.DueDate, out var dueDate))
        {
            return new OperationResultDto(false, "Datat nuk jane ne format te sakte.");
        }

        if (!Enum.TryParse<ProjectStatus>(request.Status, true, out var status))
        {
            return new OperationResultDto(false, "Status i pavlefshem.");
        }

        var isNew = string.IsNullOrWhiteSpace(request.Id);
        var existing = !isNew ? repository.GetProjects().FirstOrDefault(item => item.Id == request.Id) : null;
        var project = new InnovationProject
        {
            Id = isNew ? $"PRJ-{Guid.NewGuid():N}"[..12].ToUpperInvariant() : request.Id!,
            Title = request.Title.Trim(),
            MinistryId = request.MinistryId,
            Status = status,
            ApprovalStage = existing?.ApprovalStage ?? ApprovalStage.Draft,
            Description = existing?.Description ?? string.Empty,
            RejectionReason = existing?.RejectionReason,
            StartDate = startDate,
            DueDate = dueDate,
            Kpi = Math.Clamp(request.Kpi, 0, 100),
            OwnerName = request.OwnerName.Trim(),
            Progress = Math.Clamp(request.Progress, 0, 100),
            CancellationReason = string.IsNullOrWhiteSpace(request.CancellationReason) ? null : request.CancellationReason.Trim()
        };

        repository.SaveProject(project, actor.FullName, isNew);
        return new OperationResultDto(true, isNew ? "Projekti u krijua." : "Projekti u perditesua.");
    }

    public OperationResultDto SaveExpert(string userId, UpsertExpertRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (!IsDirectorLike(actor))
        {
            return new OperationResultDto(false, "Vetem drejtori mund te menaxhoje ekspertet.");
        }

        var isNew = string.IsNullOrWhiteSpace(request.Id);
        var expert = new Expert
        {
            Id = isNew ? $"EXP-{Guid.NewGuid():N}"[..12].ToUpperInvariant() : request.Id!,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            MinistryId = request.MinistryId,
            RoleTitle = request.RoleTitle.Trim()
        };

        repository.SaveExpert(expert, actor.FullName, isNew);
        if (!string.IsNullOrWhiteSpace(request.AccessCode))
        {
            repository.UpdateMinistryAccessCode(request.MinistryId, request.AccessCode, actor.FullName);
        }
        return new OperationResultDto(true, isNew ? "Eksperti u shtua." : "Eksperti u perditesua.");
    }

    public OperationResultDto DeleteExpert(string userId, string expertId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (!IsDirectorLike(actor))
        {
            return new OperationResultDto(false, "Vetem drejtori mund te fshije eksperte.");
        }

        return repository.DeleteExpert(expertId, actor.FullName)
            ? new OperationResultDto(true, "Eksperti u fshi.")
            : new OperationResultDto(false, "Eksperti nuk u gjet.");
    }

    public OperationResultDto AddDocument(string userId, CreateDocumentRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var project = FilterProjects(actor).FirstOrDefault(item => item.Id == request.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Nuk ke akses te ky projekt.");
        }

        if (IsMinisterLike(actor))
        {
            return new OperationResultDto(false, "Ky rol nuk mund te ngarkoje dokumente.");
        }

        var isNew = string.IsNullOrWhiteSpace(request.Id);
        var existing = !isNew ? repository.GetDocuments().FirstOrDefault(item => item.Id == request.Id) : null;
        repository.SaveDocument(new ProjectDocument
        {
            Id = isNew ? $"DOC-{Guid.NewGuid():N}"[..12].ToUpperInvariant() : request.Id!,
            ProjectId = request.ProjectId,
            Name = request.Name.Trim(),
            FileType = request.FileType.Trim().ToUpperInvariant(),
            UploadedBy = existing?.UploadedBy ?? actor.FullName,
            UploadedOn = existing?.UploadedOn ?? DateOnly.FromDateTime(DateTime.UtcNow.Date)
        }, actor.FullName, isNew);

        var projectTitle = repository.GetProjects().First(item => item.Id == request.ProjectId).Title;
        Notify("director", "document_uploaded", "Dokument i ri", $"U ngarkua dokument i ri te projekti {projectTitle}.", request.ProjectId);
        return new OperationResultDto(true, isNew ? "Dokumenti u regjistrua." : "Dokumenti u perditesua.");
    }

    public OperationResultDto DeleteDocument(string userId, string documentId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var document = repository.GetDocuments().FirstOrDefault(item => item.Id == documentId);
        if (document is null)
        {
            return new OperationResultDto(false, "Dokumenti nuk u gjet.");
        }

        var project = FilterProjects(actor).FirstOrDefault(item => item.Id == document.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Nuk ke akses te ky dokument.");
        }

        if (IsMinisterLike(actor))
        {
            return new OperationResultDto(false, "Ky rol nuk mund te fshije dokumente.");
        }

        return repository.DeleteDocument(documentId, actor.FullName)
            ? new OperationResultDto(true, "Dokumenti u fshi.")
            : new OperationResultDto(false, "Dokumenti nuk u gjet.");
    }

    public OperationResultDto SaveWorkflowStep(string userId, UpsertWorkflowStepRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var project = FilterProjects(actor).FirstOrDefault(item => item.Id == request.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Nuk ke akses te ky projekt.");
        }

        if (IsMinisterLike(actor))
        {
            return new OperationResultDto(false, "Ky rol nuk mund te ndryshoje workflow.");
        }

        if (!DateOnly.TryParse(request.DueDate, out var dueDate))
        {
            return new OperationResultDto(false, "Afati nuk eshte ne format te sakte.");
        }

        var isNew = string.IsNullOrWhiteSpace(request.Id);
        repository.SaveWorkflowStep(new WorkflowStep
        {
            Id = isNew ? $"WF-{Guid.NewGuid():N}"[..12].ToUpperInvariant() : request.Id!,
            ProjectId = request.ProjectId,
            StepNumber = request.StepNumber,
            Description = request.Description.Trim(),
            Status = request.Status.Trim(),
            DueDate = dueDate,
            OwnerName = request.OwnerName.Trim(),
            Progress = Math.Clamp(request.Progress, 0, 100)
        }, actor.FullName, isNew);

        Notify("director", "workflow_updated", "Workflow u perditesua", $"Workflow i projektit u perditesua.", request.ProjectId);
        return new OperationResultDto(true, isNew ? "Hapi i workflow-it u shtua." : "Hapi i workflow-it u perditesua.");
    }

    public OperationResultDto DeleteWorkflowStep(string userId, string workflowStepId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (IsMinisterLike(actor))
        {
            return new OperationResultDto(false, "Ky rol nuk mund te fshije hapa workflow.");
        }

        var step = repository.GetWorkflowSteps().FirstOrDefault(item => item.Id == workflowStepId);
        if (step is null)
        {
            return new OperationResultDto(false, "Hapi i workflow nuk u gjet.");
        }

        var project = FilterProjects(actor).FirstOrDefault(item => item.Id == step.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Nuk ke akses te ky hap workflow.");
        }

        return repository.DeleteWorkflowStep(workflowStepId, actor.FullName)
            ? new OperationResultDto(true, "Hapi i workflow u fshi.")
            : new OperationResultDto(false, "Hapi i workflow nuk u gjet.");
    }

    public OperationResultDto AddNote(string userId, CreateProjectNoteRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var project = FilterProjects(actor).FirstOrDefault(item => item.Id == request.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Nuk ke akses te ky projekt.");
        }

        var isNew = string.IsNullOrWhiteSpace(request.Id);
        var existing = !isNew ? repository.GetNotes().FirstOrDefault(item => item.Id == request.Id) : null;
        if (!isNew && existing is not null && !IsDirectorLike(actor) && !string.Equals(existing.AuthorUserId, actor.Id, StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResultDto(false, "Nuk ke akses per te ndryshuar kete shenim.");
        }

        repository.SaveNote(new ProjectNote
        {
            Id = isNew ? $"NOTE-{Guid.NewGuid():N}"[..12].ToUpperInvariant() : request.Id!,
            ProjectId = request.ProjectId,
            AuthorName = existing?.AuthorName ?? actor.FullName,
            AuthorUserId = existing?.AuthorUserId ?? actor.Id,
            Content = request.Content.Trim(),
            IsPrivate = request.IsPrivate && IsDirectorLike(actor),
            CreatedUtc = existing?.CreatedUtc ?? DateTime.UtcNow
        }, actor.FullName, isNew);

        NotifyProjectMembers(project, "comment_added", "Koment i ri", $"U shtua nje koment ne projektin {project.Title}.");
        return new OperationResultDto(true, isNew ? "Shenimi u shtua." : "Shenimi u perditesua.");
    }

    public OperationResultDto DeleteNote(string userId, string noteId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var note = repository.GetNotes().FirstOrDefault(item => item.Id == noteId);
        if (note is null)
        {
            return new OperationResultDto(false, "Shenimi nuk u gjet.");
        }

        if (!IsDirectorLike(actor) && !string.Equals(note.AuthorUserId, actor.Id, StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResultDto(false, "Nuk ke akses per te fshire kete shenim.");
        }

        return repository.DeleteNote(noteId, actor.FullName)
            ? new OperationResultDto(true, "Shenimi u fshi.")
            : new OperationResultDto(false, "Shenimi nuk u gjet.");
    }

    public OperationResultDto SaveMeeting(string userId, SaveMeetingRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var project = FilterProjects(actor).FirstOrDefault(item => item.Id == request.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Nuk ke akses te ky projekt.");
        }

        if (IsMinisterLike(actor))
        {
            return new OperationResultDto(false, "Ky rol nuk mund te menaxhoje takimet.");
        }

        if (!DateTime.TryParse(request.ScheduledAt, out var scheduledLocal))
        {
            return new OperationResultDto(false, "Data e takimit nuk eshte e sakte.");
        }

        var isNew = string.IsNullOrWhiteSpace(request.Id);
        var meeting = repository.SaveMeeting(new ProjectMeeting
        {
            Id = isNew ? $"MET-{Guid.NewGuid():N}"[..12].ToUpperInvariant() : request.Id!,
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            MeetingUrl = request.MeetingUrl.Trim(),
            Platform = string.IsNullOrWhiteSpace(request.Platform) ? "google_meet" : request.Platform.Trim().ToLowerInvariant(),
            ScheduledAtUtc = scheduledLocal.ToUniversalTime(),
            DurationMinutes = Math.Clamp(request.DurationMinutes, 15, 240),
            CreatedByUserId = actor.Id,
            AttendeeUserIds = request.AttendeeUserIds.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Status = isNew ? "scheduled" : repository.GetMeetings().First(item => item.Id == request.Id).Status,
            CreatedUtc = isNew ? DateTime.UtcNow : repository.GetMeetings().First(item => item.Id == request.Id).CreatedUtc
        }, actor.FullName, isNew);

        foreach (var attendeeId in meeting.AttendeeUserIds)
        {
            Notify(attendeeId, "info", "Takim i ri caktuar", $"{meeting.Title} | {meeting.ScheduledAtUtc.ToLocalTime():dd MMM yyyy HH:mm}", project.Id);
        }

        Notify("director", "info", "Takim i projektit", $"{meeting.Title} u planifikua.", project.Id);
        return new OperationResultDto(true, isNew ? "Takimi u krijua." : "Takimi u perditesua.");
    }

    public OperationResultDto CompleteMeeting(string userId, string meetingId, CompleteMeetingRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null || !IsDirectorLike(actor))
        {
            return new OperationResultDto(false, "Vetem drejtori mund ta perfundoje takimin.");
        }

        var meeting = repository.GetMeetings().FirstOrDefault(item => item.Id == meetingId);
        if (meeting is null)
        {
            return new OperationResultDto(false, "Takimi nuk u gjet.");
        }

        return repository.CompleteMeeting(meetingId, request.Notes.Trim(), request.RecordingUrl?.Trim(), actor.FullName)
            ? new OperationResultDto(true, "Takimi u shenua si i perfunduar.")
            : new OperationResultDto(false, "Takimi nuk u gjet.");
    }

    public OperationResultDto DeleteMeeting(string userId, string meetingId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (IsMinisterLike(actor))
        {
            return new OperationResultDto(false, "Ky rol nuk mund te fshije takime.");
        }

        return repository.DeleteMeeting(meetingId, actor.FullName)
            ? new OperationResultDto(true, "Takimi u fshi.")
            : new OperationResultDto(false, "Takimi nuk u gjet.");
    }

    public OperationResultDto SaveTask(string userId, SaveTaskRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var project = FilterProjects(actor).FirstOrDefault(item => item.Id == request.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Nuk ke akses te ky projekt.");
        }

        if (IsMinisterLike(actor))
        {
            return new OperationResultDto(false, "Ky rol nuk mund te menaxhoje detyrat.");
        }

        DateOnly? deadline = null;
        if (!string.IsNullOrWhiteSpace(request.Deadline))
        {
            if (!DateOnly.TryParse(request.Deadline, out var parsedDeadline))
            {
                return new OperationResultDto(false, "Afati i detyres nuk eshte i sakte.");
            }

            deadline = parsedDeadline;
        }

        var isNew = string.IsNullOrWhiteSpace(request.Id);
        repository.SaveTask(new ProjectTask
        {
            Id = isNew ? $"TSK-{Guid.NewGuid():N}"[..12].ToUpperInvariant() : request.Id!,
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = NormalizeTaskStatus(request.Status),
            Priority = NormalizeTaskPriority(request.Priority),
            AssigneeUserId = string.IsNullOrWhiteSpace(request.AssigneeUserId) ? null : request.AssigneeUserId,
            Deadline = deadline,
            EstimatedHours = Math.Max(0, request.EstimatedHours),
            ActualHours = Math.Max(0, request.ActualHours),
            Tags = request.Tags.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList(),
            Position = Math.Max(0, request.Position),
            CreatedByUserId = isNew ? actor.Id : repository.GetTasks().First(item => item.Id == request.Id).CreatedByUserId,
            CreatedUtc = isNew ? DateTime.UtcNow : repository.GetTasks().First(item => item.Id == request.Id).CreatedUtc,
            UpdatedUtc = DateTime.UtcNow
        }, actor.FullName, isNew);

        NotifyProjectMembers(project, "info", "Detyre e perditesuar", $"Detyrat e projektit {project.Title} u perditesuan.");
        return new OperationResultDto(true, isNew ? "Detyra u shtua." : "Detyra u perditesua.");
    }

    public OperationResultDto DeleteTask(string userId, string taskId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var task = repository.GetTasks().FirstOrDefault(item => item.Id == taskId);
        if (task is null)
        {
            return new OperationResultDto(false, "Detyra nuk u gjet.");
        }

        if (!IsDirectorLike(actor) && !string.Equals(task.CreatedByUserId, actor.Id, StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResultDto(false, "Nuk ke akses per ta fshire kete detyre.");
        }

        return repository.DeleteTask(taskId, actor.FullName)
            ? new OperationResultDto(true, "Detyra u fshi.")
            : new OperationResultDto(false, "Detyra nuk u gjet.");
    }

    public OperationResultDto SaveTaskComment(string userId, SaveTaskCommentRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var task = repository.GetTasks().FirstOrDefault(item => item.Id == request.TaskId);
        if (task is null)
        {
            return new OperationResultDto(false, "Detyra nuk u gjet.");
        }

        var project = FilterProjects(actor).FirstOrDefault(item => item.Id == task.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Nuk ke akses te kjo detyre.");
        }

        var isNew = string.IsNullOrWhiteSpace(request.Id);
        var existing = !isNew ? repository.GetTaskComments().FirstOrDefault(item => item.Id == request.Id) : null;
        if (!isNew && existing is not null && !IsDirectorLike(actor) && !string.Equals(existing.AuthorUserId, actor.Id, StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResultDto(false, "Nuk ke akses per te ndryshuar kete koment.");
        }

        repository.SaveTaskComment(new TaskComment
        {
            Id = isNew ? $"TCM-{Guid.NewGuid():N}"[..12].ToUpperInvariant() : request.Id!,
            TaskId = request.TaskId,
            AuthorUserId = isNew ? actor.Id : existing!.AuthorUserId,
            AuthorName = isNew ? actor.FullName : existing!.AuthorName,
            Content = request.Content.Trim(),
            CreatedUtc = isNew ? DateTime.UtcNow : existing!.CreatedUtc
        }, actor.FullName, isNew);

        return new OperationResultDto(true, isNew ? "Komenti i detyres u shtua." : "Komenti i detyres u perditesua.");
    }

    public OperationResultDto DeleteTaskComment(string userId, string taskCommentId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var comment = repository.GetTaskComments().FirstOrDefault(item => item.Id == taskCommentId);
        if (comment is null)
        {
            return new OperationResultDto(false, "Komenti i detyres nuk u gjet.");
        }

        if (!IsDirectorLike(actor) && !string.Equals(comment.AuthorUserId, actor.Id, StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResultDto(false, "Nuk ke akses per ta fshire kete koment.");
        }

        return repository.DeleteTaskComment(taskCommentId, actor.FullName)
            ? new OperationResultDto(true, "Komenti i detyres u fshi.")
            : new OperationResultDto(false, "Komenti i detyres nuk u gjet.");
    }

    public OperationResultDto CertifyMilestone(string userId, CertifyMilestoneRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (!IsDirectorLike(actor))
        {
            return new OperationResultDto(false, "Vetem drejtori mund te certifikoje piketa.");
        }

        var project = repository.GetProjects().FirstOrDefault(item => item.Id == request.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Projekti nuk u gjet.");
        }

        if (project.Kpi < request.TargetPercent)
        {
            return new OperationResultDto(false, "KPI aktual nuk e ka arritur ende kete pikete.");
        }

        var milestone = repository.GetProjectMilestones()
            .FirstOrDefault(item => string.Equals(item.ProjectId, request.ProjectId, StringComparison.OrdinalIgnoreCase) && item.TargetPercent == request.TargetPercent);

        if (milestone is null)
        {
            milestone = repository.SaveProjectMilestone(new ProjectMilestone
            {
                Id = $"MLS-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                ProjectId = request.ProjectId,
                TargetPercent = request.TargetPercent,
                CreatedUtc = DateTime.UtcNow
            }, actor.FullName, true);
        }

        var ministry = repository.GetMinistries().First(item => item.Id == project.MinistryId);
        var certificate = $"""
CERTIFIKATE ARRITJEJE
Innovation4Albania
Projekti: {project.Title}
Ministria: {ministry.Name}
Piketa: {request.TargetPercent}% e perfundimit
Data: {DateTime.Now:dd/MM/yyyy}
Certifikuar nga: {actor.FullName}
Nenshkrim dixhital: {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{actor.Id}:{project.Id}:{request.TargetPercent}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"))[..16]}...
""";

        repository.SaveProjectMilestone(new ProjectMilestone
        {
            Id = milestone.Id,
            ProjectId = milestone.ProjectId,
            TargetPercent = milestone.TargetPercent,
            AchievedAtUtc = DateTime.UtcNow,
            CertifiedByUserId = actor.Id,
            CertifiedByName = actor.FullName,
            CertificateContent = certificate,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedUtc = milestone.CreatedUtc
        }, actor.FullName, false);

        return new OperationResultDto(true, $"Piketa {request.TargetPercent}% u certifikua.");
    }

    public OperationResultDto SaveProjectPhoto(string userId, SaveProjectPhotoRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (IsMinisterLike(actor))
        {
            return new OperationResultDto(false, "Ky rol nuk mund te menaxhoje galeri.");
        }

        var project = FilterProjects(actor).FirstOrDefault(item => item.Id == request.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Nuk ke akses te ky projekt.");
        }

        var isNew = string.IsNullOrWhiteSpace(request.Id);
        var existing = !isNew ? repository.GetProjectPhotos().FirstOrDefault(item => item.Id == request.Id) : null;
        DateOnly? takenOn = null;
        if (!string.IsNullOrWhiteSpace(request.TakenOn) && DateOnly.TryParse(request.TakenOn, out var parsedTakenOn))
        {
            takenOn = parsedTakenOn;
        }

        repository.SaveProjectPhoto(new ProjectPhoto
        {
            Id = isNew ? $"PHT-{Guid.NewGuid():N}"[..12].ToUpperInvariant() : request.Id!,
            ProjectId = request.ProjectId,
            FileUrl = request.FileUrl.Trim(),
            ThumbnailUrl = request.FileUrl.Trim(),
            Caption = request.Caption?.Trim(),
            Location = request.Location?.Trim(),
            TakenOn = takenOn,
            UploadedByUserId = existing?.UploadedByUserId ?? actor.Id,
            UploadedByName = existing?.UploadedByName ?? actor.FullName,
            UploadedAtUtc = existing?.UploadedAtUtc ?? DateTime.UtcNow
        }, actor.FullName, isNew);

        return new OperationResultDto(true, isNew ? "Fotoja u shtua ne galeri." : "Fotoja u perditesua.");
    }

    public OperationResultDto DeleteProjectPhoto(string userId, string photoId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        var photo = repository.GetProjectPhotos().FirstOrDefault(item => item.Id == photoId);
        if (photo is null)
        {
            return new OperationResultDto(false, "Fotoja nuk u gjet.");
        }

        var project = FilterProjects(actor).FirstOrDefault(item => item.Id == photo.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Nuk ke akses te kjo foto.");
        }

        if (!IsDirectorLike(actor) && !string.Equals(photo.UploadedByUserId, actor.Id, StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResultDto(false, "Vetem ngarkuesi ose drejtori mund ta fshije foton.");
        }

        return repository.DeleteProjectPhoto(photoId, actor.FullName)
            ? new OperationResultDto(true, "Fotoja u fshi nga galeria.")
            : new OperationResultDto(false, "Fotoja nuk u gjet.");
    }

    public OperationResultDto SaveOkr(string userId, SaveOkrRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (!IsDirectorLike(actor))
        {
            return new OperationResultDto(false, "Vetem drejtori mund te krijoje ose perditesoje OKR.");
        }

        var ministry = repository.GetMinistries().FirstOrDefault(item => item.Id == request.MinistryId);
        if (ministry is null)
        {
            return new OperationResultDto(false, "Ministria nuk u gjet.");
        }

        if (request.KeyResults.Count == 0)
        {
            return new OperationResultDto(false, "Duhet te shtosh te pakten nje Key Result.");
        }

        var isNew = string.IsNullOrWhiteSpace(request.Id);
        var existing = !isNew ? repository.GetOkrs().FirstOrDefault(item => item.Id == request.Id) : null;
        if (!isNew && existing is null)
        {
            return new OperationResultDto(false, "OKR nuk u gjet.");
        }

        var savedOkr = repository.SaveOkr(new OkrObjective
        {
            Id = isNew ? $"OKR-{Guid.NewGuid():N}"[..12].ToUpperInvariant() : request.Id!,
            MinistryId = request.MinistryId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Period = request.Period.Trim(),
            OwnerUserId = existing?.OwnerUserId ?? actor.Id,
            CreatedUtc = existing?.CreatedUtc ?? DateTime.UtcNow
        }, actor.FullName, isNew);

        foreach (var keyResultRequest in request.KeyResults.Where(item => !string.IsNullOrWhiteSpace(item.Title)))
        {
            var existingKeyResult = !string.IsNullOrWhiteSpace(keyResultRequest.Id)
                ? repository.GetKeyResults().FirstOrDefault(item => item.Id == keyResultRequest.Id)
                : null;

            repository.SaveKeyResult(new KeyResult
            {
                Id = string.IsNullOrWhiteSpace(keyResultRequest.Id)
                    ? $"KR-{Guid.NewGuid():N}"[..12].ToUpperInvariant()
                    : keyResultRequest.Id!,
                OkrId = savedOkr.Id,
                Title = keyResultRequest.Title.Trim(),
                TargetValue = Math.Max(0, keyResultRequest.TargetValue),
                CurrentValue = existingKeyResult?.CurrentValue ?? 0,
                Unit = string.IsNullOrWhiteSpace(keyResultRequest.Unit) ? "%" : keyResultRequest.Unit.Trim(),
                CreatedUtc = existingKeyResult?.CreatedUtc ?? DateTime.UtcNow
            }, actor.FullName, existingKeyResult is null);
        }

        return new OperationResultDto(true, isNew ? "OKR u krijua." : "OKR u perditesua.");
    }

    public OperationResultDto LinkProjectToOkr(string userId, LinkProjectOkrRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (IsMinisterLike(actor))
        {
            return new OperationResultDto(false, "Ky rol nuk mund te lidhe projekte me OKR.");
        }

        var project = FilterProjects(actor).FirstOrDefault(item => item.Id == request.ProjectId);
        if (project is null)
        {
            return new OperationResultDto(false, "Nuk ke akses te ky projekt.");
        }

        var keyResult = repository.GetKeyResults().FirstOrDefault(item => item.Id == request.KeyResultId);
        if (keyResult is null)
        {
            return new OperationResultDto(false, "Key Result nuk u gjet.");
        }

        var okr = repository.GetOkrs().FirstOrDefault(item => item.Id == keyResult.OkrId);
        if (okr is null)
        {
            return new OperationResultDto(false, "OKR nuk u gjet.");
        }

        if (!string.Equals(project.MinistryId, okr.MinistryId, StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResultDto(false, "Projekti mund te lidhet vetem me OKR te ministrise se vet.");
        }

        repository.SaveProjectOkrLink(new ProjectOkrLink
        {
            ProjectId = request.ProjectId,
            KeyResultId = request.KeyResultId,
            ContributionWeight = Math.Clamp(request.ContributionWeight, 10, 100)
        }, actor.FullName);

        NotifyProjectMembers(project, "workflow_updated", "Lidhje e re me OKR", $"Projekti {project.Title} u lidh me nje Key Result te ministrise.");
        return new OperationResultDto(true, "Projekti u lidh me OKR.");
    }

    public OperationResultDto MarkNotificationsAsRead(string userId, string? notificationId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        repository.MarkNotificationsAsRead(actor.Id, notificationId);
        return new OperationResultDto(true, "Njoftimet u perditesuan.");
    }

    public OperationResultDto ClearReadNotifications(string userId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        repository.ClearReadNotifications(actor.Id);
        return new OperationResultDto(true, "Njoftimet e lexuara u pastruan.");
    }

    public OperationResultDto ConfirmImport(string userId, ConfirmImportRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null || !IsDirectorLike(actor))
        {
            return new OperationResultDto(false, "Vetem drejtori mund te importoje projekte.");
        }

        var preview = ParseImportPreview(request.FileContentBase64);
        var validRows = preview.Rows.Where(item => item.IsValid).ToList();
        if (!validRows.Any())
        {
            return new OperationResultDto(false, "Nuk ka rreshta te vlefshme per import.");
        }

        foreach (var row in validRows)
        {
            var ministry = repository.GetMinistries().First(item => string.Equals(item.Name, row.MinistryName, StringComparison.OrdinalIgnoreCase));
            repository.SaveProject(new InnovationProject
            {
                Id = $"PRJ-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                Title = row.Title,
                MinistryId = ministry.Id,
                Status = ParseImportStatus(row.Status),
                ApprovalStage = ApprovalStage.Draft,
                Description = row.Description,
                RejectionReason = null,
                StartDate = DateOnly.ParseExact(row.StartDate, "dd/MM/yyyy"),
                DueDate = DateOnly.ParseExact(row.DueDate, "dd/MM/yyyy"),
                Kpi = int.Parse(row.Kpi),
                OwnerName = row.OwnerName,
                CancellationReason = null,
                Progress = 0
            }, actor.FullName, true);
        }

        repository.AddImportLog(new ImportLog
        {
            Id = $"IMP-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            ImportedByUserId = actor.Id,
            ImportedByName = actor.FullName,
            FileName = request.FileName,
            TotalRows = preview.TotalRows,
            SuccessfulRows = validRows.Count,
            FailedRows = preview.InvalidRows,
            ErrorsJson = JsonSerializer.Serialize(preview.Rows.Where(item => !item.IsValid).Select(item => new { item.RowNumber, item.ErrorMessage })),
            CreatedUtc = DateTime.UtcNow
        });

        return new OperationResultDto(true, $"{validRows.Count} projekte u importuan me sukses!");
    }

    public OperationResultDto UpdateAlertSettings(string userId, UpdateAlertSettingsRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (!IsDirectorLike(actor))
        {
            return new OperationResultDto(false, "Vetem drejtori mund te konfiguroje alertet.");
        }

        repository.UpdateAlertSettings(new AlertSettings
        {
            CriticalKpiThreshold = Math.Clamp(request.CriticalKpiThreshold, 0, 100),
            WarningKpiThreshold = Math.Clamp(request.WarningKpiThreshold, 0, 100),
            WarningDaysBeforeDeadline = Math.Clamp(request.WarningDaysBeforeDeadline, 1, 60),
            EmailRecipients = request.EmailRecipients.Trim()
        }, actor.FullName);

        return new OperationResultDto(true, "Konfigurimi i alerteve u ruajt.");
    }

    public MonthlyReportPreviewDto? GetMonthlyReportPreview(string userId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null || !IsDirectorLike(actor))
        {
            return null;
        }

        var previousMonth = DateTime.Today.AddMonths(-1);
        var monthLabel = previousMonth.ToString("MMMM yyyy");
        var recipientCount = repository.GetUsers().Count(user => user.Role is UserRole.Minister or UserRole.PrimeMinister or UserRole.Director or UserRole.NucleusDirector);
        return new MonthlyReportPreviewDto(monthLabel, BuildMonthlyReportHtml(monthLabel, repository.GetProjects().ToList()), recipientCount);
    }

    public OperationResultDto UpdateMonthlyReportSettings(string userId, UpdateMonthlyReportSettingsRequestDto request)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null || !IsDirectorLike(actor))
        {
            return new OperationResultDto(false, "Vetem drejtori mund te ndryshoje raportet mujore.");
        }

        repository.UpdateMonthlyReportSettings(request.IsEnabled, actor.FullName);
        return new OperationResultDto(true, request.IsEnabled
            ? "Raportet mujore automatike u aktivizuan."
            : "Raportet mujore automatike u caktivizuan.");
    }

    public OperationResultDto SendMonthlyReportNow(string userId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null || !IsDirectorLike(actor))
        {
            return new OperationResultDto(false, "Vetem drejtori mund ta gjeneroje raportin mujor.");
        }

        var recipientCount = repository.GetUsers().Count(user => user.Role is UserRole.Minister or UserRole.PrimeMinister or UserRole.Director or UserRole.NucleusDirector);
        repository.RegisterMonthlyReportDispatch(DateTime.UtcNow, recipientCount, actor.FullName);
        return new OperationResultDto(true, $"Raporti mujor u gjenerua dhe u pergatit per {recipientCount} marres.");
    }

    public OperationResultDto UpdateMinistryAccessCode(string userId, string ministryId, string accessCode)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (!IsDirectorLike(actor))
        {
            return new OperationResultDto(false, "Vetem drejtori mund te menaxhoje kodet e aksesit.");
        }

        if (string.IsNullOrWhiteSpace(accessCode))
        {
            return new OperationResultDto(false, "Kodi i aksesit nuk mund te jete bosh.");
        }

        return repository.UpdateMinistryAccessCode(ministryId, accessCode, actor.FullName)
            ? new OperationResultDto(true, "Kodi i aksesit u perditesua.")
            : new OperationResultDto(false, "Ministria nuk u gjet.");
    }

    public OperationResultDto RefreshSync(string userId)
    {
        var actor = repository.GetUserById(userId);
        if (actor is null)
        {
            return new OperationResultDto(false, "Perdoruesi nuk u gjet.");
        }

        if (IsMinisterLike(actor))
        {
            return new OperationResultDto(false, "Ky rol nuk mund te ekzekutoje sync.");
        }

        repository.RefreshSyncStatus(actor.FullName);
        return new OperationResultDto(true, "Statusi i sinkronizimit u rifreskua.");
    }

    private IReadOnlyList<InnovationProject> FilterProjects(PlatformUser user)
    {
        var all = repository.GetProjects();
        return IsMinistryScopedRole(user) && !string.IsNullOrWhiteSpace(user.MinistryId)
            ? all.Where(project => project.MinistryId == user.MinistryId).ToList()
            : all.ToList();
    }

    private IReadOnlyList<Ministry> FilterMinistries(PlatformUser user)
    {
        var all = repository.GetMinistries();
        return IsMinistryScopedRole(user) && !string.IsNullOrWhiteSpace(user.MinistryId)
            ? all.Where(ministry => ministry.Id == user.MinistryId).ToList()
            : all.ToList();
    }

    private static bool IsDirectorLike(PlatformUser user) =>
        user.Role is UserRole.Director or UserRole.NucleusDirector;

    private static bool IsMinisterLike(PlatformUser user) =>
        user.Role is UserRole.Minister or UserRole.PrimeMinister;

    private static bool IsMinistryScopedRole(PlatformUser user) =>
        user.Role is UserRole.Expert or UserRole.NucleusDirector;

    private static string GetRiskLevel(InnovationProject project)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (project.Status == ProjectStatus.Cancelled || project.Kpi < 40 || project.DueDate < today)
        {
            return "High";
        }

        if (project.Kpi <= 70 || project.DueDate <= today.AddDays(14))
        {
            return "Medium";
        }

        return "Low";
    }

    private IReadOnlyList<CalendarEventDto> BuildCalendarEvents(
        IReadOnlyList<InnovationProject> projects,
        IReadOnlyList<WorkflowStepDto> workflowSteps,
        IReadOnlyList<MeetingDto> meetings,
        IReadOnlyList<Ministry> ministries)
    {
        var ministryLookup = ministries.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var projectLookup = projects.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var items = new List<CalendarEventDto>();

        items.AddRange(projects.Select(project =>
        {
            var ministry = ministryLookup[project.MinistryId];
            return new CalendarEventDto(
                project.Id,
                "project",
                project.Title,
                ministry.Id,
                ministry.Name,
                ministry.Acronym,
                project.Status.ToString(),
                ApprovalStageShqip(project.ApprovalStage),
                project.DueDate.ToString("yyyy-MM-dd"),
                CalendarColor(project),
                false,
                project.Id,
                project.OwnerName,
                project.Kpi,
                project.Progress);
        }));

        items.AddRange(workflowSteps.Select(step =>
        {
            var project = projectLookup[step.ProjectId];
            var ministry = ministryLookup[project.MinistryId];
            return new CalendarEventDto(
                step.Id,
                "workflow",
                $"Hapi {step.StepNumber}: {step.Description}",
                ministry.Id,
                ministry.Name,
                ministry.Acronym,
                step.Status,
                ApprovalStageShqip(project.ApprovalStage),
                step.DueDate,
                "#1D4ED8",
                true,
                project.Id,
                step.OwnerName,
                project.Kpi,
                step.Progress);
        }));

        items.AddRange(meetings.Select(meeting =>
        {
            var project = projectLookup[meeting.ProjectId];
            var ministry = ministryLookup[project.MinistryId];
            return new CalendarEventDto(
                meeting.Id,
                "meeting",
                $"Kamer takimi: {meeting.Title}",
                ministry.Id,
                ministry.Name,
                ministry.Acronym,
                meeting.Status,
                ApprovalStageShqip(project.ApprovalStage),
                DateOnly.FromDateTime(DateTime.Parse(meeting.ScheduledAtIso)).ToString("yyyy-MM-dd"),
                "#7C3AED",
                false,
                project.Id,
                project.OwnerName,
                project.Kpi,
                project.Progress);
        }));

        return items.OrderBy(item => item.DueDate).ToList();
    }

    private Dictionary<string, string> BuildUserNameMap()
    {
        var map = repository.GetUsers().ToDictionary(item => item.Id, item => item.FullName, StringComparer.OrdinalIgnoreCase);
        foreach (var expert in repository.GetExperts())
        {
            map.TryAdd(expert.Id, expert.FullName);
        }

        return map;
    }

    private MeetingDto MapMeeting(ProjectMeeting item, IReadOnlyDictionary<string, string> projectNames, IReadOnlyDictionary<string, string> userNames, PlatformUser actor)
    {
        var now = DateTime.UtcNow;
        var start = item.ScheduledAtUtc;
        var end = item.ScheduledAtUtc.AddMinutes(item.DurationMinutes);
        return new MeetingDto(
            item.Id,
            item.ProjectId,
            projectNames[item.ProjectId],
            item.Title,
            item.Description,
            item.MeetingUrl,
            item.Platform,
            item.ScheduledAtUtc.ToString("o"),
            item.ScheduledAtUtc.ToLocalTime().ToString("dddd, dd MMMM yyyy '·' HH:mm"),
            item.DurationMinutes,
            item.Status,
            item.Notes,
            item.RecordingUrl,
            item.AttendeeUserIds.Select(id => userNames.TryGetValue(id, out var name) ? name : id).ToList(),
            !string.IsNullOrWhiteSpace(item.MeetingUrl) && start <= now.AddMinutes(15) && end >= now.AddMinutes(-15) && item.Status == "scheduled",
            IsDirectorLike(actor) && item.Status == "scheduled" && end < now);
    }

    private TaskDto MapTask(ProjectTask item, IReadOnlyDictionary<string, string> projectNames, IReadOnlyDictionary<string, string> userNames, PlatformUser actor)
    {
        var comments = repository.GetTaskComments()
            .Where(comment => comment.TaskId == item.Id)
            .Select(comment => new TaskCommentDto(comment.Id, comment.TaskId, comment.AuthorName, comment.Content, comment.CreatedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm")))
            .ToList();

        return new TaskDto(
            item.Id,
            item.ProjectId,
            projectNames[item.ProjectId],
            item.Title,
            item.Description,
            item.Status,
            item.Priority,
            item.AssigneeUserId,
            item.AssigneeUserId is not null && userNames.TryGetValue(item.AssigneeUserId, out var assigneeName) ? assigneeName : null,
            item.Deadline?.ToString("yyyy-MM-dd"),
            item.EstimatedHours,
            item.ActualHours,
            item.Tags,
            item.Position,
            comments.Count,
            IsDirectorLike(actor) || string.Equals(item.CreatedByUserId, actor.Id, StringComparison.OrdinalIgnoreCase),
            comments);
    }

    private ProjectMilestoneDto MapMilestone(ProjectMilestone item, InnovationProject project, PlatformUser actor)
    {
        var achieved = item.AchievedAtUtc.HasValue;
        var canCertify = IsDirectorLike(actor) && !achieved && project.Kpi >= item.TargetPercent;
        return new ProjectMilestoneDto(
            item.Id,
            item.TargetPercent,
            achieved,
            achieved ? "Arritur" : project.Kpi >= item.TargetPercent ? "Gati per certifikim" : "Ne pritje",
            item.AchievedAtUtc?.ToLocalTime().ToString("dd MMM yyyy"),
            item.CertifiedByName,
            item.Notes,
            item.CertificateContent,
            canCertify);
    }

    private ProjectPhotoDto MapProjectPhoto(ProjectPhoto item, PlatformUser actor) =>
        new(
            item.Id,
            item.ProjectId,
            item.FileUrl,
            item.ThumbnailUrl ?? item.FileUrl,
            item.Caption ?? "Pa pershkrim",
            item.Location ?? "-",
            item.TakenOn?.ToString("yyyy-MM-dd"),
            item.UploadedByName,
            item.UploadedAtUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
            IsDirectorLike(actor) || string.Equals(item.UploadedByUserId, actor.Id, StringComparison.OrdinalIgnoreCase));

    private IReadOnlyList<OkrDto> BuildVisibleOkrs(PlatformUser actor, IReadOnlyList<Ministry> visibleMinistries, IReadOnlyList<InnovationProject> visibleProjects)
    {
        var visibleMinistryIds = visibleMinistries.Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ministryNames = visibleMinistries.ToDictionary(item => item.Id, item => item.Name, StringComparer.OrdinalIgnoreCase);
        var projectNames = visibleProjects.ToDictionary(item => item.Id, item => item.Title, StringComparer.OrdinalIgnoreCase);
        var usersById = repository.GetUsers().ToDictionary(item => item.Id, item => item.FullName, StringComparer.OrdinalIgnoreCase);
        var keyResultsByOkr = repository.GetKeyResults()
            .GroupBy(item => item.OkrId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var linksByKeyResult = repository.GetProjectOkrLinks()
            .GroupBy(item => item.KeyResultId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        return repository.GetOkrs()
            .Where(item => visibleMinistryIds.Contains(item.MinistryId))
            .OrderBy(item => item.Period)
            .ThenBy(item => item.Title)
            .Select(okr =>
            {
                var keyResultDtos = (keyResultsByOkr.TryGetValue(okr.Id, out var krList) ? krList : [])
                    .Select(keyResult =>
                    {
                        var linkedProjects = (linksByKeyResult.TryGetValue(keyResult.Id, out var linkList) ? linkList : [])
                            .Where(link => projectNames.ContainsKey(link.ProjectId))
                            .Select(link => new ProjectOkrLinkDto(link.ProjectId, projectNames[link.ProjectId], link.ContributionWeight))
                            .ToList();

                        var recalculatedCurrent = CalculateCurrentValueForKeyResult(keyResult, linkedProjects, visibleProjects);
                        var progressPercent = keyResult.TargetValue <= 0
                            ? 0
                            : (int)Math.Round(Math.Clamp(recalculatedCurrent / keyResult.TargetValue * 100m, 0m, 100m));

                        return new KeyResultDto(
                            keyResult.Id,
                            keyResult.OkrId,
                            keyResult.Title,
                            keyResult.TargetValue,
                            recalculatedCurrent,
                            keyResult.Unit,
                            progressPercent,
                            linkedProjects);
                    })
                    .ToList();

                var overallProgress = keyResultDtos.Count == 0 ? 0 : (int)Math.Round(keyResultDtos.Average(item => item.ProgressPercent));
                return new OkrDto(
                    okr.Id,
                    okr.MinistryId,
                    ministryNames.TryGetValue(okr.MinistryId, out var ministryName) ? ministryName : okr.MinistryId,
                    okr.Title,
                    okr.Description,
                    okr.Period,
                    usersById.TryGetValue(okr.OwnerUserId, out var ownerName) ? ownerName : actor.FullName,
                    overallProgress,
                    keyResultDtos);
            })
            .ToList();
    }

    private IReadOnlyList<ProjectOkrLinkDto> BuildProjectOkrLinks(string projectId, string projectTitle) =>
        repository.GetProjectOkrLinks()
            .Where(item => string.Equals(item.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
            .Select(item => new ProjectOkrLinkDto(projectId, BuildLinkedProjectLabel(item.KeyResultId, projectTitle), item.ContributionWeight))
            .ToList();

    private string BuildLinkedProjectLabel(string keyResultId, string fallbackProjectTitle)
    {
        var keyResult = repository.GetKeyResults().FirstOrDefault(item => item.Id == keyResultId);
        var okr = keyResult is null ? null : repository.GetOkrs().FirstOrDefault(item => item.Id == keyResult.OkrId);
        if (keyResult is null)
        {
            return fallbackProjectTitle;
        }

        return okr is null ? keyResult.Title : $"{okr.Title} | {keyResult.Title}";
    }

    private decimal CalculateCurrentValueForKeyResult(KeyResult keyResult, IReadOnlyList<ProjectOkrLinkDto> linkedProjects, IReadOnlyList<InnovationProject> visibleProjects)
    {
        if (linkedProjects.Count == 0)
        {
            return keyResult.CurrentValue;
        }

        var projectsById = visibleProjects.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        decimal totalWeight = 0;
        decimal weightedKpi = 0;
        foreach (var link in linkedProjects)
        {
            if (!projectsById.TryGetValue(link.ProjectId, out var project))
            {
                continue;
            }

            totalWeight += link.ContributionWeight;
            weightedKpi += project.Kpi * link.ContributionWeight;
        }

        if (totalWeight <= 0)
        {
            return keyResult.CurrentValue;
        }

        return Math.Round(weightedKpi / totalWeight, 1);
    }

    private static string NormalizeTaskStatus(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            "in_progress" => "in_progress",
            "review" => "review",
            "done" => "done",
            _ => "todo"
        };

    private static string NormalizeTaskPriority(string priority) =>
        priority.Trim().ToLowerInvariant() switch
        {
            "low" => "low",
            "high" => "high",
            "urgent" => "urgent",
            _ => "medium"
        };

    private static NotificationDto MapNotification(PlatformNotification item)
    {
        var diff = DateTime.UtcNow - item.CreatedUtc;
        var relative = diff.TotalMinutes < 1 ? "Tani"
            : diff.TotalHours < 1 ? $"{(int)diff.TotalMinutes} minuta me pare"
            : diff.TotalDays < 1 ? $"{(int)diff.TotalHours} ore me pare"
            : diff.TotalDays < 2 ? "Dje"
            : $"{(int)diff.TotalDays} dite me pare";

        return new NotificationDto(item.Id, item.Type, item.Title, item.Message, item.ProjectId, item.IsRead, item.CreatedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"), relative);
    }

    private ImportPreviewDto ParseImportPreview(string fileContentBase64)
    {
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(fileContentBase64));
        var lines = decoded.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            return new ImportPreviewDto(0, 0, 0, []);
        }

        var rows = new List<ImportPreviewRowDto>();
        for (var i = 1; i < lines.Length; i++)
        {
            var columns = lines[i].Split(',');
            var title = columns.ElementAtOrDefault(0)?.Trim() ?? "";
            var ministryName = columns.ElementAtOrDefault(1)?.Trim() ?? "";
            var description = columns.ElementAtOrDefault(2)?.Trim() ?? "";
            var startDate = columns.ElementAtOrDefault(3)?.Trim() ?? "";
            var dueDate = columns.ElementAtOrDefault(4)?.Trim() ?? "";
            var kpi = columns.ElementAtOrDefault(5)?.Trim() ?? "";
            var owner = columns.ElementAtOrDefault(6)?.Trim() ?? "";
            var status = columns.ElementAtOrDefault(7)?.Trim() ?? "draft";

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(title)) errors.Add("Titulli mungon");
            if (!repository.GetMinistries().Any(item => string.Equals(item.Name, ministryName, StringComparison.OrdinalIgnoreCase))) errors.Add("Ministria nuk u gjet");
            if (!DateOnly.TryParseExact(startDate, "dd/MM/yyyy", out _)) errors.Add("Data e fillimit e pasakte");
            if (!DateOnly.TryParseExact(dueDate, "dd/MM/yyyy", out _)) errors.Add("Afati i pasakte");
            if (!int.TryParse(kpi, out var kpiValue) || kpiValue is < 0 or > 100) errors.Add("KPI duhet 0-100");
            if (string.IsNullOrWhiteSpace(owner)) errors.Add("Pergjegjesi mungon");

            rows.Add(new ImportPreviewRowDto(i, errors.Count == 0, string.Join("; ", errors), title, ministryName, description, startDate, dueDate, kpi, owner, status));
        }

        return new ImportPreviewDto(rows.Count, rows.Count(item => item.IsValid), rows.Count(item => !item.IsValid), rows);
    }

    private void Notify(string recipientId, string type, string title, string message, string? projectId)
    {
        repository.AddNotification(new PlatformNotification
        {
            Id = $"NOT-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            RecipientId = recipientId,
            Type = type,
            Title = title,
            Message = message,
            ProjectId = projectId,
            IsRead = false,
            CreatedUtc = DateTime.UtcNow
        });
    }

    private void NotifyOwner(InnovationProject project, string type, string title, string message)
    {
        var expert = repository.GetUsers().FirstOrDefault(item => item.Role == UserRole.Expert && string.Equals(item.MinistryId, project.MinistryId, StringComparison.OrdinalIgnoreCase));
        if (expert is not null)
        {
            Notify(expert.Id, type, title, message, project.Id);
        }

        var nucleusDirector = repository.GetUsers().FirstOrDefault(item => item.Role == UserRole.NucleusDirector && string.Equals(item.MinistryId, project.MinistryId, StringComparison.OrdinalIgnoreCase));
        if (nucleusDirector is not null)
        {
            Notify(nucleusDirector.Id, type, title, message, project.Id);
        }
    }

    private void NotifyProjectMembers(InnovationProject project, string type, string title, string message)
    {
        Notify("director", type, title, message, project.Id);
        NotifyOwner(project, type, title, message);
    }

    private static ProjectStatus ParseImportStatus(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            "active" => ProjectStatus.Active,
            "in_progress" => ProjectStatus.InProcess,
            "completed" => ProjectStatus.Completed,
            "cancelled" => ProjectStatus.Cancelled,
            _ => ProjectStatus.InProcess
        };

    private static string CalendarColor(InnovationProject project)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (project.Kpi < 40 || project.DueDate < today)
        {
            return "#EF4444";
        }

        if (project.Kpi < 60)
        {
            return "#F97316";
        }

        return "#22C55E";
    }

    private static string ApprovalStageShqip(ApprovalStage stage) =>
        stage switch
        {
            ApprovalStage.Draft => "Draft",
            ApprovalStage.UnderReview => "Ne Shqyrtim",
            ApprovalStage.Approved => "Miratuar",
            ApprovalStage.Active => "Aktiv",
            ApprovalStage.Completed => "Perfunduar",
            ApprovalStage.Cancelled => "Anuluar",
            _ => stage.ToString()
        };

    private static DateTime NextMonthlyRun()
    {
        var now = DateTime.Now;
        return new DateTime(now.Year, now.Month, 1, 8, 0, 0).AddMonths(1);
    }

    private string BuildMonthlyReportHtml(string monthLabel, IReadOnlyList<InnovationProject> projects)
    {
        var ministries = repository.GetMinistries().ToDictionary(item => item.Id, item => item.Name, StringComparer.OrdinalIgnoreCase);
        var activeProjects = projects.Count(item => item.Status is ProjectStatus.Active or ProjectStatus.InProcess);
        var completedProjects = projects.Count(item => item.Status == ProjectStatus.Completed);
        var averageKpi = projects.Count == 0 ? 0 : (int)Math.Round(projects.Average(item => item.Kpi));
        var riskProjects = projects
            .Where(item => item.Kpi < 60 || item.DueDate < DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(item => item.Kpi)
            .ThenBy(item => item.DueDate)
            .Take(10)
            .ToList();
        var highlights = projects
            .Where(item => item.Status == ProjectStatus.Completed)
            .OrderByDescending(item => item.Kpi)
            .Take(5)
            .ToList();

        var ministryRows = repository.GetMinistries()
            .Select(ministry =>
            {
                var related = projects.Where(project => project.MinistryId == ministry.Id).ToList();
                var avg = related.Count == 0 ? 0 : (int)Math.Round(related.Average(project => project.Kpi));
                return $"<tr><td>{ministry.Name}</td><td>{related.Count(project => project.Status is ProjectStatus.Active or ProjectStatus.InProcess)}</td><td>{related.Count(project => project.Status == ProjectStatus.Completed)}</td><td>{avg}%</td><td>{(avg >= 70 ? "↑" : avg >= 50 ? "→" : "↓")}</td></tr>";
            });

        var riskRows = riskProjects.Any()
            ? string.Join("", riskProjects.Select(project => $"<li><strong>{project.Title}</strong> · {ministries[project.MinistryId]} · KPI {project.Kpi}% · Afati {project.DueDate:dd/MM/yyyy}</li>"))
            : "<li>Nuk ka projekte kritike per kete periudhe.</li>";

        var highlightRows = highlights.Any()
            ? string.Join("", highlights.Select(project => $"<li><strong>{project.Title}</strong> · {ministries[project.MinistryId]} · KPI final {project.Kpi}%</li>"))
            : "<li>Nuk ka projekte te perfunduara ne kete periudhe.</li>";

        return $$"""
<!DOCTYPE html>
<html lang="sq">
<head>
    <meta charset="utf-8" />
    <title>Raporti Mujor - {{monthLabel}}</title>
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; padding: 24px; color: #12233d; }
        h1, h2 { margin: 0 0 12px; }
        .hero { border-bottom: 3px solid #0b4f9c; padding-bottom: 16px; margin-bottom: 20px; }
        .stats { display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); gap: 12px; margin: 18px 0; }
        .stats article { border: 1px solid #d9e4f2; border-radius: 14px; padding: 12px; background: #f8fbff; }
        table { width: 100%; border-collapse: collapse; margin-top: 12px; }
        th, td { border: 1px solid #d9e4f2; padding: 10px; text-align: left; }
        th { background: #0b4f9c; color: white; }
        ul { padding-left: 18px; }
        footer { margin-top: 24px; color: #5f6f86; font-size: 12px; }
    </style>
</head>
<body>
    <div class="hero">
        <h1>Raporti Mujor i Inovacionit Publik</h1>
        <p>Muaji: {{monthLabel}}</p>
    </div>
    <section>
        <h2>Permbledhje Ekzekutive</h2>
        <div class="stats">
            <article><strong>{{activeProjects}}</strong><div>Projekte aktive</div></article>
            <article><strong>{{projects.Count}}</strong><div>Projekte totale</div></article>
            <article><strong>{{completedProjects}}</strong><div>Projekte te perfunduara</div></article>
            <article><strong>{{averageKpi}}%</strong><div>KPI mesatar kombetar</div></article>
            <article><strong>{{riskProjects.Count}}</strong><div>Projekte ne risk</div></article>
        </div>
    </section>
    <section>
        <h2>Performanca sipas Ministrise</h2>
        <table>
            <thead><tr><th>Ministria</th><th>Aktive</th><th>Perfunduar</th><th>KPI Mesatar</th><th>Trendi</th></tr></thead>
            <tbody>{{string.Join("", ministryRows)}}</tbody>
        </table>
    </section>
    <section>
        <h2>Projektet ne Risk</h2>
        <ul>{{riskRows}}</ul>
    </section>
    <section>
        <h2>Arritjet e Muajit</h2>
        <ul>{{highlightRows}}</ul>
    </section>
    <footer>Gjeneruar automatikisht nga Innovation4Albania · {{DateTime.Now:dd/MM/yyyy HH:mm}}</footer>
</body>
</html>
""";
    }
}
