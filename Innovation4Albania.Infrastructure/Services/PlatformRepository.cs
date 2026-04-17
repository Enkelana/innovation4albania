using Innovation4Albania.Application.Interfaces;
using Innovation4Albania.Domain.Entities;
using Innovation4Albania.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Innovation4Albania.Infrastructure.Services;

public sealed class PlatformRepository : IPlatformRepository
{
    private readonly object gate = new();
    private readonly List<Ministry> ministries;
    private readonly List<PlatformUser> users;
    private readonly List<Expert> experts;
    private readonly List<InnovationProject> projects;
    private readonly List<WorkflowStep> workflowSteps;
    private readonly List<ProjectDocument> documents;
    private readonly List<ProjectNote> notes;
    private readonly List<ProjectMeeting> meetings;
    private readonly List<ProjectTask> tasks;
    private readonly List<TaskComment> taskComments;
    private readonly List<ProjectMilestone> projectMilestones;
    private readonly List<ProjectPhoto> projectPhotos;
    private readonly List<OkrObjective> okrs;
    private readonly List<KeyResult> keyResults;
    private readonly List<ProjectOkrLink> projectOkrLinks;
    private readonly List<ApprovalEntry> approvalEntries;
    private readonly List<PlatformNotification> notifications;
    private readonly List<ImportLog> importLogs;
    private readonly List<HistoryLog> historyLogs;
    private readonly Dictionary<string, string> demoAccessCodes;
    private readonly AlertSettings alertSettings;
    private readonly SyncStatus syncStatus;
    private readonly MonthlyReportSettings monthlyReportSettings;

    public PlatformRepository()
    {
        ministries =
        [
            Ministry("MIN-01", "Ministria per Evropen dhe Punet e Jashtme", "MEPJ", "Albana Koci", "mepj@innovation4albania.al", "MIN001"),
            Ministry("MIN-02", "Ministria e Brendshme", "MB", "Ardian Meta", "mb@innovation4albania.al", "MIN002"),
            Ministry("MIN-03", "Ministria e Mbrojtjes", "MM", "Drita Pasha", "mm@innovation4albania.al", "MIN003"),
            Ministry("MIN-04", "Ministria e Financave", "MF", "Elton Shkodra", "mf@innovation4albania.al", "MIN004"),
            Ministry("MIN-05", "Ministria e Ekonomise, Kultures dhe Inovacionit", "MEKI", "Klea Bano", "meki@innovation4albania.al", "MIN005"),
            Ministry("MIN-06", "Ministria e Arsimit dhe Sportit", "MAS", "Jonida Leka", "mas@innovation4albania.al", "MIN006"),
            Ministry("MIN-07", "Ministria e Shendetesise dhe Mbrojtjes Sociale", "MSHMS", "Mirela Balla", "mshms@innovation4albania.al", "MIN007"),
            Ministry("MIN-08", "Ministria e Bujqesise dhe Zhvillimit Rural", "MBZHR", "Ergest Sina", "mbzhr@innovation4albania.al", "MIN008"),
            Ministry("MIN-09", "Ministria e Infrastruktures dhe Energjise", "MIE", "Ina Lulo", "mie@innovation4albania.al", "MIN009"),
            Ministry("MIN-10", "Ministria e Turizmit dhe Mjedisit", "MTM", "Klodiana Rruci", "mtm@innovation4albania.al", "MIN010"),
            Ministry("MIN-11", "Ministria e Drejtesise", "MD", "Sokol Mema", "md@innovation4albania.al", "MIN011"),
            Ministry("MIN-12", "Ministria e Drejtimit Vendor", "MDV", "Gerald Hoxha", "mdv@innovation4albania.al", "MIN012")
        ];

        demoAccessCodes = ministries.ToDictionary(
            ministry => ministry.Id,
            ministry => $"MIN{int.Parse(ministry.Id.Split('-').Last()):000}",
            StringComparer.OrdinalIgnoreCase);

        users =
        [
            new PlatformUser { Id = "prime-minister", FullName = "Kryeministri", Email = "kryeministri@innovation4albania.al", Role = UserRole.PrimeMinister, RoleLabel = "Pamje strategjike kombetare" },
            new PlatformUser { Id = "minister", FullName = "Ministrja e Shtetit", Email = "minister@innovation4albania.al", Role = UserRole.Minister, RoleLabel = "Pamje strategjike kombetare" },
            new PlatformUser { Id = "director", FullName = "Drejtori i Pergjithshem i Inovacionit Publik", Email = "director@innovation4albania.al", Role = UserRole.Director, RoleLabel = "Administrim i plote i platformes" },
            new PlatformUser { Id = "nuklis-director", FullName = "Drejtori i Nuklit", Email = "nuklis.director@innovation4albania.al", Role = UserRole.NucleusDirector, RoleLabel = "Administrim drejtorial per ministrine e caktuar", MinistryId = "MIN-05" },
            new PlatformUser { Id = "expert-mdig", FullName = "Elira Hasa", Email = "elira.hasa@innovation4albania.al", Role = UserRole.Expert, RoleLabel = "Eksperte e inovacionit", MinistryId = "MIN-05" }
        ];

        experts = ministries.Select((ministry, index) => new Expert
        {
            Id = $"EXP-{index + 1:00}",
            FullName = index == 13 ? "Elira Hasa" : $"Eksperti {index + 1:00}",
            Email = index == 13 ? "elira.hasa@innovation4albania.al" : $"eksperti{index + 1:00}@innovation4albania.al",
            MinistryId = ministry.Id,
            RoleTitle = "Ekspert Inovacioni"
        }).ToList();

        projects = [];
        workflowSteps = [];
        documents = [];
        notes = [];
        meetings = [];
        tasks = [];
        taskComments = [];
        projectMilestones = [];
        projectPhotos = [];
        okrs = [];
        keyResults = [];
        projectOkrLinks = [];
        approvalEntries = [];
        notifications = [];
        importLogs = [];
        historyLogs = [];

        alertSettings = new AlertSettings
        {
            CriticalKpiThreshold = 40,
            WarningKpiThreshold = 70,
            WarningDaysBeforeDeadline = 14,
            EmailRecipients = "director@innovation4albania.al;alerts@innovation4albania.al"
        };

        syncStatus = new SyncStatus
        {
            Source = "Google Sheets, Excel dhe API",
            Mode = "Rifreskim manual i fazes fillestare cdo 15 minuta",
            LastSyncUtc = DateTime.UtcNow.AddMinutes(-12),
            NextSyncUtc = DateTime.UtcNow.AddMinutes(3),
            Health = "Ne rregull"
        };

        monthlyReportSettings = new MonthlyReportSettings
        {
            IsEnabled = true,
            LastSentUtc = DateTime.UtcNow.AddDays(-17),
            LastRecipientCount = users.Count(user => user.Role is UserRole.Minister or UserRole.PrimeMinister or UserRole.Director or UserRole.NucleusDirector)
        };

        SeedPortfolio();
    }

    public IReadOnlyList<PlatformUser> GetUsers() => users;

    public PlatformUser? GetUserById(string userId) =>
        users.FirstOrDefault(user => user.Id.Equals(userId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Ministry> GetMinistries() => ministries;

    public bool VerifyMinistryAccessCode(string ministryId, string accessCode)
    {
        var ministry = ministries.FirstOrDefault(item => item.Id.Equals(ministryId, StringComparison.OrdinalIgnoreCase));
        return ministry is not null && string.Equals(ministry.AccessCodeHash, HashAccessCode(accessCode), StringComparison.Ordinal);
    }

    public string? GetDemoAccessCode(string ministryId) =>
        demoAccessCodes.TryGetValue(ministryId, out var code) ? code : null;

    public IReadOnlyList<Expert> GetExperts() => experts;

    public IReadOnlyList<InnovationProject> GetProjects() => projects;

    public IReadOnlyList<WorkflowStep> GetWorkflowSteps() => workflowSteps;

    public IReadOnlyList<ProjectDocument> GetDocuments() => documents;

    public IReadOnlyList<ProjectNote> GetNotes() => notes.OrderByDescending(note => note.CreatedUtc).ToList();

    public IReadOnlyList<ProjectMeeting> GetMeetings() => meetings.OrderBy(item => item.ScheduledAtUtc).ToList();

    public IReadOnlyList<ProjectTask> GetTasks() => tasks.OrderBy(item => item.Status).ThenBy(item => item.Position).ToList();

    public IReadOnlyList<TaskComment> GetTaskComments() => taskComments.OrderBy(item => item.CreatedUtc).ToList();

    public IReadOnlyList<ProjectMilestone> GetProjectMilestones() => projectMilestones.OrderBy(item => item.TargetPercent).ToList();

    public IReadOnlyList<ProjectPhoto> GetProjectPhotos() => projectPhotos.OrderByDescending(item => item.UploadedAtUtc).ToList();

    public IReadOnlyList<OkrObjective> GetOkrs() => okrs.OrderBy(item => item.Period).ThenBy(item => item.Title).ToList();

    public IReadOnlyList<KeyResult> GetKeyResults() => keyResults.OrderBy(item => item.Title).ToList();

    public IReadOnlyList<ProjectOkrLink> GetProjectOkrLinks() => projectOkrLinks.OrderByDescending(item => item.ContributionWeight).ToList();

    public IReadOnlyList<ApprovalEntry> GetApprovalEntries() =>
        approvalEntries.OrderByDescending(item => item.CreatedUtc).ToList();

    public IReadOnlyList<PlatformNotification> GetNotifications() =>
        notifications.OrderByDescending(item => item.CreatedUtc).ToList();

    public IReadOnlyList<ImportLog> GetImportLogs() =>
        importLogs.OrderByDescending(item => item.CreatedUtc).ToList();

    public IReadOnlyList<PlatformAlert> GetAlerts()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return projects
            .Select(project =>
            {
                var severity = ResolveSeverity(project, today);
                if (severity is null)
                {
                    return null;
                }

                return new PlatformAlert
                {
                    Id = $"ALT-{project.Id}",
                    ProjectId = project.Id,
                    MinistryId = project.MinistryId,
                    Severity = severity.Value,
                    Title = severity == AlertSeverity.Critical ? "Alert kritik" : "Alert kujdes",
                    Message = severity == AlertSeverity.Critical
                        ? "KPI nen prag kritik ose afat i kaluar. Kerkohet nderhyrje e menjehershme."
                        : "Afat i afert ose KPI ne vezhgim. Monitoro progresin."
                };
            })
            .Where(alert => alert is not null)
            .Cast<PlatformAlert>()
            .OrderByDescending(alert => alert.Severity)
            .ToList();
    }

    public IReadOnlyList<HistoryLog> GetHistoryLogs() =>
        historyLogs.OrderByDescending(log => log.TimestampUtc).ToList();

    public AlertSettings GetAlertSettings() => alertSettings;

    public SyncStatus GetSyncStatus() => syncStatus;

    public MonthlyReportSettings GetMonthlyReportSettings() => monthlyReportSettings;

    public InnovationProject SaveProject(InnovationProject project, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                projects.Add(project);
                AppendLog(actorName, "Projekt i krijuar", "Projekt", "-", project.Title, project.Id);
                return project;
            }

            var existing = projects.First(item => item.Id == project.Id);
            AppendDiffLogsForProject(existing, project, actorName);
            var updated = new InnovationProject
            {
                Id = existing.Id,
                Title = project.Title,
                MinistryId = project.MinistryId,
                Status = project.Status,
                ApprovalStage = project.ApprovalStage,
                Description = project.Description,
                RejectionReason = project.RejectionReason,
                StartDate = project.StartDate,
                DueDate = project.DueDate,
                Kpi = project.Kpi,
                OwnerName = project.OwnerName,
                CancellationReason = project.CancellationReason,
                Progress = project.Progress
            };
            var index = projects.FindIndex(item => item.Id == project.Id);
            projects[index] = updated;
            return updated;
        }
    }

    public Expert SaveExpert(Expert expert, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                experts.Add(expert);
                AppendLog(actorName, "Ekspert i krijuar", "Ekspert", "-", expert.FullName, expert.Id);
                return expert;
            }

            var existing = experts.First(item => item.Id == expert.Id);
            if (!string.Equals(existing.MinistryId, expert.MinistryId, StringComparison.OrdinalIgnoreCase))
            {
                AppendLog(actorName, "Eksperti u ricaktua", "Ministria", existing.MinistryId, expert.MinistryId, expert.Id);
            }

            existing = new Expert
            {
                Id = existing.Id,
                FullName = expert.FullName,
                Email = expert.Email,
                MinistryId = expert.MinistryId,
                RoleTitle = expert.RoleTitle
            };

            var index = experts.FindIndex(item => item.Id == expert.Id);
            experts[index] = existing;
            return existing;
        }
    }

    public bool DeleteExpert(string expertId, string actorName)
    {
        lock (gate)
        {
            var expert = experts.FirstOrDefault(item => item.Id == expertId);
            if (expert is null)
            {
                return false;
            }

            experts.Remove(expert);
            AppendLog(actorName, "Eksperti u fshi", "Ekspert", expert.FullName, "-", expertId);
            return true;
        }
    }

    public bool UpdateMinistryAccessCode(string ministryId, string accessCode, string actorName)
    {
        lock (gate)
        {
            var ministry = ministries.FirstOrDefault(item => item.Id == ministryId);
            if (ministry is null)
            {
                return false;
            }

            var previousDemo = demoAccessCodes.TryGetValue(ministryId, out var code) ? code : "-";
            demoAccessCodes[ministryId] = accessCode.Trim();

            var index = ministries.FindIndex(item => item.Id == ministryId);
            ministries[index] = new Ministry
            {
                Id = ministry.Id,
                Name = ministry.Name,
                Acronym = ministry.Acronym,
                DirectorName = ministry.DirectorName,
                ContactEmail = ministry.ContactEmail,
                AccessCodeHash = HashAccessCode(accessCode)
            };

            AppendLog(actorName, "Kodi i aksesit u perditesua", "Kodi i aksesit", previousDemo, accessCode.Trim(), ministryId);
            return true;
        }
    }

    public ProjectDocument SaveDocument(ProjectDocument document, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                documents.Add(document);
                AppendLog(actorName, "Dokument i ngarkuar", "Dokument", "-", document.Name, document.ProjectId);
                return document;
            }

            var existing = documents.First(item => item.Id == document.Id);
            var updated = new ProjectDocument
            {
                Id = existing.Id,
                ProjectId = existing.ProjectId,
                Name = document.Name,
                FileType = document.FileType,
                UploadedBy = existing.UploadedBy,
                UploadedOn = existing.UploadedOn
            };
            var index = documents.FindIndex(item => item.Id == document.Id);
            documents[index] = updated;
            AppendLog(actorName, "Dokumenti u perditesua", "Dokument", existing.Name, updated.Name, updated.ProjectId);
            return updated;
        }
    }

    public bool DeleteDocument(string documentId, string actorName)
    {
        lock (gate)
        {
            var document = documents.FirstOrDefault(item => item.Id == documentId);
            if (document is null)
            {
                return false;
            }

            documents.Remove(document);
            AppendLog(actorName, "Dokument i fshire", "Dokument", document.Name, "-", document.ProjectId);
            return true;
        }
    }

    public WorkflowStep SaveWorkflowStep(WorkflowStep step, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                workflowSteps.Add(step);
                AppendLog(actorName, "Hap i rrjedhes se punes i krijuar", "Rrjedha e punes", "-", step.Description, step.ProjectId);
                return step;
            }

            var existing = workflowSteps.First(item => item.Id == step.Id);
            if (!string.Equals(existing.Status, step.Status, StringComparison.OrdinalIgnoreCase))
            {
                AppendLog(actorName, "Rrjedha e punes u perditesua", "Statusi", existing.Status, step.Status, step.ProjectId);
            }
            var updated = new WorkflowStep
            {
                Id = existing.Id,
                ProjectId = existing.ProjectId,
                StepNumber = step.StepNumber,
                Description = step.Description,
                Status = step.Status,
                DueDate = step.DueDate,
                OwnerName = step.OwnerName,
                Progress = step.Progress
            };
            var index = workflowSteps.FindIndex(item => item.Id == step.Id);
            workflowSteps[index] = updated;
            return updated;
        }
    }

    public bool DeleteWorkflowStep(string workflowStepId, string actorName)
    {
        lock (gate)
        {
            var step = workflowSteps.FirstOrDefault(item => item.Id == workflowStepId);
            if (step is null)
            {
                return false;
            }

            workflowSteps.Remove(step);
            AppendLog(actorName, "Hapi i workflow u fshi", "Rrjedha e punes", step.Description, "-", step.ProjectId);
            return true;
        }
    }

    public ProjectNote SaveNote(ProjectNote note, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                notes.Add(note);
                AppendLog(actorName, note.IsPrivate ? "Shenim privat i shtuar" : "Shenim i shtuar", "Koment", "-", note.Content, note.ProjectId);
                return note;
            }

            var existing = notes.First(item => item.Id == note.Id);
            var updated = new ProjectNote
            {
                Id = existing.Id,
                ProjectId = existing.ProjectId,
                AuthorName = existing.AuthorName,
                AuthorUserId = existing.AuthorUserId,
                Content = note.Content,
                IsPrivate = note.IsPrivate,
                CreatedUtc = existing.CreatedUtc
            };
            var index = notes.FindIndex(item => item.Id == note.Id);
            notes[index] = updated;
            AppendLog(actorName, "Shenimi u perditesua", "Koment", existing.Content, updated.Content, updated.ProjectId);
            return updated;
        }
    }

    public bool DeleteNote(string noteId, string actorName)
    {
        lock (gate)
        {
            var note = notes.FirstOrDefault(item => item.Id == noteId);
            if (note is null)
            {
                return false;
            }

            notes.Remove(note);
            AppendLog(actorName, "Shenim i fshire", "Koment", note.Content, "-", note.ProjectId);
            return true;
        }
    }

    public ProjectMeeting SaveMeeting(ProjectMeeting meeting, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                meetings.Add(meeting);
                AppendLog(actorName, "Takim i shtuar", "Takim", "-", meeting.Title, meeting.ProjectId);
                return meeting;
            }

            var existing = meetings.First(item => item.Id == meeting.Id);
            existing.Title = meeting.Title;
            existing.Description = meeting.Description;
            existing.MeetingUrl = meeting.MeetingUrl;
            existing.Platform = meeting.Platform;
            existing.ScheduledAtUtc = meeting.ScheduledAtUtc;
            existing.DurationMinutes = meeting.DurationMinutes;
            existing.AttendeeUserIds = meeting.AttendeeUserIds;
            AppendLog(actorName, "Takim i perditesuar", "Takim", existing.Title, meeting.Title, existing.ProjectId);
            return existing;
        }
    }

    public bool CompleteMeeting(string meetingId, string notesValue, string? recordingUrl, string actorName)
    {
        lock (gate)
        {
            var meeting = meetings.FirstOrDefault(item => item.Id == meetingId);
            if (meeting is null)
            {
                return false;
            }

            meeting.Status = "completed";
            meeting.Notes = notesValue;
            meeting.RecordingUrl = recordingUrl;
            AppendLog(actorName, "Takim i perfunduar", "Takim", "scheduled", "completed", meeting.ProjectId);
            return true;
        }
    }

    public bool DeleteMeeting(string meetingId, string actorName)
    {
        lock (gate)
        {
            var meeting = meetings.FirstOrDefault(item => item.Id == meetingId);
            if (meeting is null)
            {
                return false;
            }

            meetings.Remove(meeting);
            AppendLog(actorName, "Takim i fshire", "Takim", meeting.Title, "-", meeting.ProjectId);
            return true;
        }
    }

    public ProjectTask SaveTask(ProjectTask task, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                tasks.Add(task);
                AppendLog(actorName, "Detyre e shtuar", "Detyre", "-", task.Title, task.ProjectId);
                return task;
            }

            var existing = tasks.First(item => item.Id == task.Id);
            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Status = task.Status;
            existing.Priority = task.Priority;
            existing.AssigneeUserId = task.AssigneeUserId;
            existing.Deadline = task.Deadline;
            existing.EstimatedHours = task.EstimatedHours;
            existing.ActualHours = task.ActualHours;
            existing.Tags = task.Tags;
            existing.Position = task.Position;
            existing.UpdatedUtc = DateTime.UtcNow;
            AppendLog(actorName, "Detyra u perditesua", "Detyre", existing.Title, task.Title, existing.ProjectId);
            return existing;
        }
    }

    public bool DeleteTask(string taskId, string actorName)
    {
        lock (gate)
        {
            var task = tasks.FirstOrDefault(item => item.Id == taskId);
            if (task is null)
            {
                return false;
            }

            tasks.Remove(task);
            taskComments.RemoveAll(item => item.TaskId == taskId);
            AppendLog(actorName, "Detyre e fshire", "Detyre", task.Title, "-", task.ProjectId);
            return true;
        }
    }

    public TaskComment SaveTaskComment(TaskComment comment, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                taskComments.Add(comment);
                var task = tasks.FirstOrDefault(item => item.Id == comment.TaskId);
                AppendLog(actorName, "Koment detyre i shtuar", "Detyre", "-", comment.Content, task?.ProjectId ?? "-");
                return comment;
            }

            var existing = taskComments.First(item => item.Id == comment.Id);
            existing.Content = comment.Content;
            var taskForEdit = tasks.FirstOrDefault(item => item.Id == existing.TaskId);
            AppendLog(actorName, "Koment detyre i perditesuar", "Detyre", existing.Content, comment.Content, taskForEdit?.ProjectId ?? "-");
            return existing;
        }
    }

    public bool DeleteTaskComment(string taskCommentId, string actorName)
    {
        lock (gate)
        {
            var comment = taskComments.FirstOrDefault(item => item.Id == taskCommentId);
            if (comment is null)
            {
                return false;
            }

            taskComments.Remove(comment);
            var task = tasks.FirstOrDefault(item => item.Id == comment.TaskId);
            AppendLog(actorName, "Koment detyre i fshire", "Detyre", comment.Content, "-", task?.ProjectId ?? "-");
            return true;
        }
    }

    public ProjectMilestone SaveProjectMilestone(ProjectMilestone milestone, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                projectMilestones.Add(milestone);
                AppendLog(actorName, "Piketa u krijua", "Piketa", "-", $"{milestone.TargetPercent}%", milestone.ProjectId);
                return milestone;
            }

            var existing = projectMilestones.First(item => item.Id == milestone.Id);
            existing.AchievedAtUtc = milestone.AchievedAtUtc;
            existing.CertifiedByUserId = milestone.CertifiedByUserId;
            existing.CertifiedByName = milestone.CertifiedByName;
            existing.CertificateContent = milestone.CertificateContent;
            existing.Notes = milestone.Notes;
            AppendLog(actorName, "Piketa u certifikua", "Piketa", "-", $"{milestone.TargetPercent}%", milestone.ProjectId);
            return existing;
        }
    }

    public ProjectPhoto SaveProjectPhoto(ProjectPhoto photo, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                projectPhotos.Insert(0, photo);
                AppendLog(actorName, "Foto e projektit u shtua", "Galeria", "-", photo.Caption ?? photo.FileUrl, photo.ProjectId);
                return photo;
            }

            var existing = projectPhotos.First(item => item.Id == photo.Id);
            existing.FileUrl = photo.FileUrl;
            existing.ThumbnailUrl = photo.ThumbnailUrl;
            existing.Caption = photo.Caption;
            existing.Location = photo.Location;
            existing.TakenOn = photo.TakenOn;
            AppendLog(actorName, "Foto e projektit u perditesua", "Galeria", "-", photo.Caption ?? photo.FileUrl, photo.ProjectId);
            return existing;
        }
    }

    public bool DeleteProjectPhoto(string photoId, string actorName)
    {
        lock (gate)
        {
            var photo = projectPhotos.FirstOrDefault(item => item.Id == photoId);
            if (photo is null)
            {
                return false;
            }

            projectPhotos.Remove(photo);
            AppendLog(actorName, "Foto e projektit u fshi", "Galeria", photo.Caption ?? photo.FileUrl, "-", photo.ProjectId);
            return true;
        }
    }

    public OkrObjective SaveOkr(OkrObjective okr, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                okrs.Add(okr);
                AppendLog(actorName, "OKR u krijua", "OKR", "-", okr.Title, okr.MinistryId);
                return okr;
            }

            var existing = okrs.First(item => item.Id == okr.Id);
            var previousTitle = existing.Title;
            existing.MinistryId = okr.MinistryId;
            existing.Title = okr.Title;
            existing.Description = okr.Description;
            existing.Period = okr.Period;
            existing.OwnerUserId = okr.OwnerUserId;
            AppendLog(actorName, "OKR u perditesua", "OKR", previousTitle, okr.Title, okr.MinistryId);
            return existing;
        }
    }

    public KeyResult SaveKeyResult(KeyResult keyResult, string actorName, bool isNew)
    {
        lock (gate)
        {
            if (isNew)
            {
                keyResults.Add(keyResult);
                AppendLog(actorName, "Key Result u shtua", "Key Result", "-", keyResult.Title, keyResult.OkrId);
                return keyResult;
            }

            var existing = keyResults.First(item => item.Id == keyResult.Id);
            var previousTitle = existing.Title;
            existing.OkrId = keyResult.OkrId;
            existing.Title = keyResult.Title;
            existing.TargetValue = keyResult.TargetValue;
            existing.CurrentValue = keyResult.CurrentValue;
            existing.Unit = keyResult.Unit;
            AppendLog(actorName, "Key Result u perditesua", "Key Result", previousTitle, keyResult.Title, keyResult.OkrId);
            return existing;
        }
    }

    public ProjectOkrLink SaveProjectOkrLink(ProjectOkrLink link, string actorName)
    {
        lock (gate)
        {
            var existing = projectOkrLinks.FirstOrDefault(item =>
                string.Equals(item.ProjectId, link.ProjectId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.KeyResultId, link.KeyResultId, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                projectOkrLinks.Add(link);
                AppendLog(actorName, "Projekti u lidh me OKR", "OKR", "-", $"{link.KeyResultId} ({link.ContributionWeight}%)", link.ProjectId);
                return link;
            }

            var previous = existing.ContributionWeight;
            existing.ContributionWeight = link.ContributionWeight;
            AppendLog(actorName, "Lidhja me OKR u perditesua", "Kontributi", $"{previous}%", $"{link.ContributionWeight}%", link.ProjectId);
            return existing;
        }
    }

    public ApprovalEntry AddApprovalEntry(ApprovalEntry entry)
    {
        lock (gate)
        {
            approvalEntries.Insert(0, entry);
            return entry;
        }
    }

    public void AddNotification(PlatformNotification notification)
    {
        lock (gate)
        {
            notifications.Insert(0, notification);
        }
    }

    public int MarkNotificationsAsRead(string recipientId, string? notificationId = null)
    {
        lock (gate)
        {
            var matching = notifications.Where(item =>
                string.Equals(item.RecipientId, recipientId, StringComparison.OrdinalIgnoreCase) &&
                (notificationId is null || string.Equals(item.Id, notificationId, StringComparison.OrdinalIgnoreCase)))
                .Where(item => !item.IsRead)
                .ToList();

            matching.ForEach(item => item.IsRead = true);
            return matching.Count;
        }
    }

    public void ClearReadNotifications(string recipientId)
    {
        lock (gate)
        {
            notifications.RemoveAll(item =>
                string.Equals(item.RecipientId, recipientId, StringComparison.OrdinalIgnoreCase) &&
                item.IsRead);
        }
    }

    public ImportLog AddImportLog(ImportLog log)
    {
        lock (gate)
        {
            importLogs.Insert(0, log);
            return log;
        }
    }

    public AlertSettings UpdateAlertSettings(AlertSettings settings, string actorName)
    {
        lock (gate)
        {
            AppendLog(actorName, "Konfigurimi i alerteve u perditesua", "Pragu i alertit", $"{alertSettings.CriticalKpiThreshold}/{alertSettings.WarningKpiThreshold}", $"{settings.CriticalKpiThreshold}/{settings.WarningKpiThreshold}", "alerts");
            alertSettings.CriticalKpiThreshold = settings.CriticalKpiThreshold;
            alertSettings.WarningKpiThreshold = settings.WarningKpiThreshold;
            alertSettings.WarningDaysBeforeDeadline = settings.WarningDaysBeforeDeadline;
            alertSettings.EmailRecipients = settings.EmailRecipients;
            return alertSettings;
        }
    }

    public SyncStatus RefreshSyncStatus(string actorName)
    {
        lock (gate)
        {
            var previous = syncStatus.LastSyncUtc;
            syncStatus.LastSyncUtc = DateTime.UtcNow;
            syncStatus.NextSyncUtc = DateTime.UtcNow.AddMinutes(15);
            syncStatus.Health = "Ne rregull";
            AppendLog(actorName, "Sinkronizimi u rifreskua", "Sinkronizimi i fundit", previous.ToString("u"), syncStatus.LastSyncUtc.ToString("u"), "sync");
            return syncStatus;
        }
    }

    public MonthlyReportSettings UpdateMonthlyReportSettings(bool isEnabled, string actorName)
    {
        lock (gate)
        {
            if (monthlyReportSettings.IsEnabled != isEnabled)
            {
                AppendLog(actorName, "Raportet mujore u perditesuan", "Raportet mujore", monthlyReportSettings.IsEnabled ? "Aktive" : "Pasive", isEnabled ? "Aktive" : "Pasive", "monthly-report");
            }

            monthlyReportSettings.IsEnabled = isEnabled;
            return monthlyReportSettings;
        }
    }

    public MonthlyReportSettings RegisterMonthlyReportDispatch(DateTime sentUtc, int recipientCount, string actorName)
    {
        lock (gate)
        {
            monthlyReportSettings.LastSentUtc = sentUtc;
            monthlyReportSettings.LastRecipientCount = recipientCount;
            AppendLog(actorName, "Raporti mujor u gjenerua", "Raporti mujor", "-", $"{recipientCount} marres", "monthly-report");
            return monthlyReportSettings;
        }
    }

    private void SeedPortfolio()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        for (var i = 0; i < ministries.Count; i++)
        {
            var ministry = ministries[i];
            var primaryStatus = (i % 4) switch
            {
                0 => ProjectStatus.Active,
                1 => ProjectStatus.InProcess,
                2 => ProjectStatus.Completed,
                _ => ProjectStatus.Cancelled
            };

            var secondaryStatus = (i % 3) switch
            {
                0 => ProjectStatus.Active,
                1 => ProjectStatus.InProcess,
                _ => ProjectStatus.Completed
            };

            var firstProject = new InnovationProject
            {
                Id = $"PRJ-{(i * 2) + 1:000}",
                Title = $"{ministry.Acronym} Laboratori i Ridesenjimit te Sherbimit",
                MinistryId = ministry.Id,
                Status = primaryStatus,
                ApprovalStage = primaryStatus == ProjectStatus.Completed ? ApprovalStage.Completed : primaryStatus == ProjectStatus.Cancelled ? ApprovalStage.Cancelled : i % 5 == 0 ? ApprovalStage.UnderReview : ApprovalStage.Active,
                StartDate = today.AddDays(-(110 + i * 4)),
                DueDate = today.AddDays((i % 6) * 7 - 9),
                Kpi = Math.Clamp(38 + (i * 5), 28, 92),
                OwnerName = experts[i].FullName,
                Description = "Projekt pilot per ridesenjimin e sherbimeve publike.",
                RejectionReason = i % 7 == 0 ? "Nevojitet sqarim shtese mbi qellimin dhe KPI-te." : null,
                CancellationReason = primaryStatus == ProjectStatus.Cancelled ? "Rialokim buxheti dhe ndryshim prioritetesh." : null,
                Progress = Math.Clamp(25 + (i * 4), 12, 100)
            };

            var secondProject = new InnovationProject
            {
                Id = $"PRJ-{(i * 2) + 2:000}",
                Title = $"{ministry.Acronym} Programi i Sinkronizimit te te Dhenave",
                MinistryId = ministry.Id,
                Status = secondaryStatus,
                ApprovalStage = secondaryStatus == ProjectStatus.Completed ? ApprovalStage.Completed : ApprovalStage.Active,
                StartDate = today.AddDays(-(60 + i * 3)),
                DueDate = today.AddDays(8 + (i % 5) * 6),
                Kpi = Math.Clamp(52 + (i * 3), 35, 96),
                OwnerName = experts[i].FullName,
                Description = "Projekt per harmonizimin e te dhenave dhe raportimit operacional.",
                Progress = Math.Clamp(40 + (i * 3), 18, 100)
            };

            projects.Add(firstProject);
            projects.Add(secondProject);

            workflowSteps.Add(new WorkflowStep
            {
                Id = $"WF-{firstProject.Id}-1",
                ProjectId = firstProject.Id,
                StepNumber = 1,
                Description = "Validim me drejtuesit e nukles",
                Status = firstProject.Status == ProjectStatus.Completed ? "Perfunduar" : "Ne proces",
                DueDate = firstProject.DueDate.AddDays(-10),
                OwnerName = firstProject.OwnerName,
                Progress = Math.Clamp(firstProject.Progress - 10, 5, 100)
            });

            workflowSteps.Add(new WorkflowStep
            {
                Id = $"WF-{secondProject.Id}-1",
                ProjectId = secondProject.Id,
                StepNumber = 1,
                Description = "Harmonizim me Google Sheets dhe Excel",
                Status = secondProject.Status == ProjectStatus.Completed ? "Perfunduar" : "Ne pritje shqyrtimi",
                DueDate = secondProject.DueDate.AddDays(-6),
                OwnerName = secondProject.OwnerName,
                Progress = Math.Clamp(secondProject.Progress - 5, 10, 100)
            });

            documents.Add(new ProjectDocument
            {
                Id = $"DOC-{firstProject.Id}",
                ProjectId = firstProject.Id,
                Name = $"{firstProject.Title} - Permbledhje.pdf",
                FileType = "PDF",
                UploadedBy = firstProject.OwnerName,
                UploadedOn = today.AddDays(-(i + 2))
            });

            notes.Add(new ProjectNote
            {
                Id = $"NOTE-{firstProject.Id}",
                ProjectId = firstProject.Id,
                AuthorName = firstProject.OwnerName,
                AuthorUserId = ministry.Id == "MIN-05" ? "expert-mdig" : $"expert-{i:00}",
                Content = "Nevojitet koordinim me ekipin e ministrise per afatin e fazes se ardhshme.",
                IsPrivate = false,
                CreatedUtc = DateTime.UtcNow.AddHours(-(i + 1))
            });

            meetings.Add(new ProjectMeeting
            {
                Id = $"MET-{firstProject.Id}",
                ProjectId = firstProject.Id,
                Title = $"Takim koordinues per {firstProject.Title}",
                Description = "Koordinim javor me ekipin e projektit dhe drejtuesin funksional.",
                MeetingUrl = "https://meet.google.com/demo-innovation4albania",
                Platform = "google_meet",
                ScheduledAtUtc = DateTime.UtcNow.AddDays((i % 5) + 1).AddHours(9 + (i % 3)),
                DurationMinutes = 60,
                CreatedByUserId = "director",
                AttendeeUserIds = ministry.Id == "MIN-05" ? ["director", "expert-mdig"] : ["director"],
                Status = "scheduled",
                CreatedUtc = DateTime.UtcNow.AddDays(-3)
            });

            tasks.Add(new ProjectTask
            {
                Id = $"TSK-{firstProject.Id}-1",
                ProjectId = firstProject.Id,
                Title = "Pergatit raportin e fazes se ardhshme",
                Description = "Permbledhje e progresit, riskut dhe nevojave per vendimmarrje.",
                Status = i % 4 == 0 ? "done" : i % 4 == 1 ? "review" : i % 4 == 2 ? "in_progress" : "todo",
                Priority = i % 5 == 0 ? "urgent" : i % 2 == 0 ? "high" : "medium",
                AssigneeUserId = ministry.Id == "MIN-05" ? "expert-mdig" : "director",
                Deadline = firstProject.DueDate.AddDays(-5),
                EstimatedHours = 6,
                ActualHours = i % 4 == 0 ? 5.5m : 0,
                Tags = ["raportim", ministry.Acronym.ToLowerInvariant()],
                Position = 1,
                CreatedByUserId = "director",
                CreatedUtc = DateTime.UtcNow.AddDays(-6),
                UpdatedUtc = DateTime.UtcNow.AddDays(-1)
            });

            tasks.Add(new ProjectTask
            {
                Id = $"TSK-{firstProject.Id}-2",
                ProjectId = firstProject.Id,
                Title = "Perditeso planin e veprimit",
                Description = "Reflekto ndryshimet e afateve dhe te KPI-ve ne planin operativ.",
                Status = "todo",
                Priority = "medium",
                AssigneeUserId = "director",
                Deadline = firstProject.DueDate.AddDays(-2),
                EstimatedHours = 3,
                ActualHours = 0,
                Tags = ["planifikim"],
                Position = 2,
                CreatedByUserId = "director",
                CreatedUtc = DateTime.UtcNow.AddDays(-4),
                UpdatedUtc = DateTime.UtcNow.AddDays(-2)
            });

            taskComments.Add(new TaskComment
            {
                Id = $"TCM-{firstProject.Id}-1",
                TaskId = $"TSK-{firstProject.Id}-1",
                AuthorUserId = "director",
                AuthorName = "Drejtori i Inovacionit Publik",
                Content = "Sigurohu qe raporti te perfshije riskun dhe afatet kritike.",
                CreatedUtc = DateTime.UtcNow.AddHours(-(i + 4))
            });

            foreach (var percent in new[] { 25, 50, 75, 100 })
            {
                projectMilestones.Add(new ProjectMilestone
                {
                    Id = $"MLS-{firstProject.Id}-{percent}",
                    ProjectId = firstProject.Id,
                    TargetPercent = percent,
                    AchievedAtUtc = firstProject.Kpi >= percent && percent <= 50 ? DateTime.UtcNow.AddDays(-(20 - i)) : null,
                    CertifiedByUserId = firstProject.Kpi >= percent && percent <= 50 ? "director" : null,
                    CertifiedByName = firstProject.Kpi >= percent && percent <= 50 ? "Drejtori i Inovacionit Publik" : null,
                    CertificateContent = firstProject.Kpi >= percent && percent <= 50 ? $"CERTIFIKATE ARRITJEJE\nProjekti: {firstProject.Title}\nMinistria: {ministry.Name}\nPiketa: {percent}%\nCertifikuar nga: Drejtori i Inovacionit Publik" : null,
                    Notes = firstProject.Kpi >= percent && percent <= 50 ? "Piketa u verifikua sipas progresit te raportuar." : null,
                    CreatedUtc = DateTime.UtcNow.AddDays(-30)
                });
            }

            projectPhotos.Add(new ProjectPhoto
            {
                Id = $"PHT-{firstProject.Id}-1",
                ProjectId = firstProject.Id,
                FileUrl = $"https://placehold.co/1200x800/0b4f9c/ffffff?text={Uri.EscapeDataString(firstProject.Title)}",
                ThumbnailUrl = $"https://placehold.co/400x300/0b4f9c/ffffff?text={Uri.EscapeDataString(ministry.Acronym)}",
                Caption = "Pamje nga aktiviteti i fundit i projektit",
                Location = ministry.Name,
                TakenOn = today.AddDays(-(i + 6)),
                UploadedByUserId = "director",
                UploadedByName = "Drejtori i Inovacionit Publik",
                UploadedAtUtc = DateTime.UtcNow.AddDays(-(i + 1))
            });

            historyLogs.Add(new HistoryLog
            {
                Id = $"LOG-{firstProject.Id}",
                TimestampUtc = DateTime.UtcNow.AddHours(-(i + 2)),
                UserName = firstProject.OwnerName,
                ActionType = "Statusi u perditesua",
                FieldName = "Statusi",
                PreviousValue = "Planifikim",
                NewValue = StatusiShqip(firstProject.Status),
                ProjectId = firstProject.Id
            });

            approvalEntries.Add(new ApprovalEntry
            {
                Id = $"APR-{firstProject.Id}",
                ProjectId = firstProject.Id,
                StageFrom = "draft",
                StageTo = ApprovalStageShqip(firstProject.ApprovalStage),
                Action = firstProject.ApprovalStage == ApprovalStage.UnderReview ? "submit" : "activate",
                ActorId = ministry.Id == "MIN-05" ? "expert-mdig" : $"seed-user-{i:00}",
                ActorName = firstProject.OwnerName,
                Comment = firstProject.RejectionReason,
                DigitalSignature = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{firstProject.OwnerName}:{firstProject.Id}:{DateTime.UtcNow.Ticks}")),
                CreatedUtc = DateTime.UtcNow.AddHours(-(i + 3))
            });
        }

        for (var i = 0; i < ministries.Count; i++)
        {
            var ministry = ministries[i];
            var objective = new OkrObjective
            {
                Id = $"OKR-{i + 1:00}",
                MinistryId = ministry.Id,
                Title = $"Objektivi strategjik {ministry.Acronym} {DateTime.UtcNow.Year}",
                Description = "Rritja e kapacitetit te inovacionit, permiresimi i sherbimeve dhe raportimi me KPI te matshme.",
                Period = i % 2 == 0 ? $"Q2 {DateTime.UtcNow.Year}" : $"{DateTime.UtcNow.Year}",
                OwnerUserId = "director",
                CreatedUtc = DateTime.UtcNow.AddDays(-(20 + i))
            };
            okrs.Add(objective);

            var ministryProjects = projects.Where(item => item.MinistryId == ministry.Id).Take(2).ToList();
            for (var krIndex = 0; krIndex < 2; krIndex++)
            {
                var keyResult = new KeyResult
                {
                    Id = $"KR-{i + 1:00}-{krIndex + 1}",
                    OkrId = objective.Id,
                    Title = krIndex == 0
                        ? "Rrit KPI-ne mesatare te projekteve te ministrise"
                        : "Siguro progres te qendrueshem ne projektet prioritare",
                    TargetValue = 100,
                    CurrentValue = 0,
                    Unit = "%",
                    CreatedUtc = DateTime.UtcNow.AddDays(-(18 + i + krIndex))
                };
                keyResults.Add(keyResult);

                if (ministryProjects.Count > krIndex)
                {
                    projectOkrLinks.Add(new ProjectOkrLink
                    {
                        ProjectId = ministryProjects[krIndex].Id,
                        KeyResultId = keyResult.Id,
                        ContributionWeight = krIndex == 0 ? 100 : 70
                    });
                }
            }
        }

        notifications.Add(new PlatformNotification
        {
            Id = "NOT-001",
            RecipientId = "director",
            Type = "project_submitted",
            Title = "Projekt i ri ne shqyrtim",
            Message = "Nje projekt pret miratim nga drejtori.",
            ProjectId = projects.First().Id,
            IsRead = false,
            CreatedUtc = DateTime.UtcNow.AddMinutes(-25)
        });

        notifications.Add(new PlatformNotification
        {
            Id = "NOT-002",
            RecipientId = "nuklis-director",
            Type = "project_watch",
            Title = "Projektet e ministrise jane gati per ndjekje",
            Message = "Dashboard-i i drejtorise eshte gati me projektet e ministrise tuaj.",
            ProjectId = projects.FirstOrDefault(item => item.MinistryId == "MIN-05")?.Id,
            IsRead = false,
            CreatedUtc = DateTime.UtcNow.AddMinutes(-18)
        });

    }

    private void AppendDiffLogsForProject(InnovationProject existing, InnovationProject updated, string actorName)
    {
        if (!string.Equals(existing.Title, updated.Title, StringComparison.Ordinal))
        {
            AppendLog(actorName, "Projekti u perditesua", "Titulli", existing.Title, updated.Title, existing.Id);
        }

        if (existing.Status != updated.Status)
        {
            AppendLog(actorName, "Projekti u perditesua", "Statusi", StatusiShqip(existing.Status), StatusiShqip(updated.Status), existing.Id);
        }

        if (existing.Kpi != updated.Kpi)
        {
            AppendLog(actorName, "Projekti u perditesua", "KPI", $"{existing.Kpi}%", $"{updated.Kpi}%", existing.Id);
        }

        if (existing.DueDate != updated.DueDate)
        {
            AppendLog(actorName, "Projekti u perditesua", "Afati", existing.DueDate.ToString("yyyy-MM-dd"), updated.DueDate.ToString("yyyy-MM-dd"), existing.Id);
        }

        if (existing.Progress != updated.Progress)
        {
            AppendLog(actorName, "Projekti u perditesua", "Progresi", $"{existing.Progress}%", $"{updated.Progress}%", existing.Id);
        }
    }

    private void AppendLog(string actorName, string actionType, string fieldName, string previousValue, string newValue, string projectId)
    {
        historyLogs.Insert(0, new HistoryLog
        {
            Id = $"LOG-{Guid.NewGuid():N}",
            TimestampUtc = DateTime.UtcNow,
            UserName = actorName,
            ActionType = actionType,
            FieldName = fieldName,
            PreviousValue = previousValue,
            NewValue = newValue,
            ProjectId = projectId
        });
    }

    private AlertSeverity? ResolveSeverity(InnovationProject project, DateOnly today)
    {
        if (project.Status == ProjectStatus.Cancelled || project.Kpi < alertSettings.CriticalKpiThreshold || project.DueDate < today)
        {
            return AlertSeverity.Critical;
        }

        if (project.Kpi <= alertSettings.WarningKpiThreshold || project.DueDate <= today.AddDays(alertSettings.WarningDaysBeforeDeadline))
        {
            return AlertSeverity.Warning;
        }

        return null;
    }

    private static string StatusiShqip(ProjectStatus status) =>
        status switch
        {
            ProjectStatus.Active => "Aktiv",
            ProjectStatus.InProcess => "Ne proces",
            ProjectStatus.Completed => "Perfunduar",
            ProjectStatus.Cancelled => "Anuluar",
            _ => status.ToString()
        };

    private static string ApprovalStageShqip(ApprovalStage stage) =>
        stage switch
        {
            ApprovalStage.Draft => "Draft",
            ApprovalStage.UnderReview => "Ne shqyrtim",
            ApprovalStage.Approved => "Miratuar",
            ApprovalStage.Active => "Aktiv",
            ApprovalStage.Completed => "Perfunduar",
            ApprovalStage.Cancelled => "Anuluar",
            _ => stage.ToString()
        };

    private static string HashAccessCode(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return Convert.ToHexString(bytes);
    }

    private static Ministry Ministry(string id, string name, string acronym, string directorName, string contactEmail, string accessCode) =>
        new()
        {
            Id = id,
            Name = name,
            Acronym = acronym,
            DirectorName = directorName,
            ContactEmail = contactEmail,
            AccessCodeHash = HashAccessCode(accessCode)
        };
}
