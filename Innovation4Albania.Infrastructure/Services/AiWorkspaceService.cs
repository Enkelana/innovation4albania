using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Innovation4Albania.Application.DTOs.Workspace;
using Innovation4Albania.Application.Interfaces;
using Innovation4Albania.Domain.Entities;
using Innovation4Albania.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Innovation4Albania.Infrastructure.Services;

public sealed class AiWorkspaceService(
    IPlatformRepository repository,
    HttpClient httpClient,
    IConfiguration configuration) : IAiWorkspaceService
{
    private const string ClaudeApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ClaudeModel = "claude-opus-4-1";

    private readonly ConcurrentDictionary<string, AiRiskAnalysisDto> riskCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, AiProjectSummaryDto> summaryCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> previousRiskScores = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PdfExtractionHistoryDto> pdfHistory = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SmartAlertRecommendationDto> smartAlertsCache = new(StringComparer.OrdinalIgnoreCase);

    public AiRiskAnalysisDto? GetProjectRiskAnalysis(string userId, string projectId, bool forceRefresh = false)
    {
        var context = BuildProjectContext(userId, projectId);
        if (context is null)
        {
            return null;
        }

        var cacheKey = $"risk:{projectId}:{context.Hash}";
        if (!forceRefresh && riskCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var score = CalculateRiskScore(context.Factors);
        var riskLevel = GetRiskLevel(score);
        var factorDtos = BuildRiskFactorDtos(context.Factors);
        var recommendations = BuildRecommendations(context.Factors, score);
        var aiText = TryCallClaude(
            """
            Ti je nje analist rreziku per projekte inovacioni ne qeverine shqiptare.
            Analizo te dhenat dhe jep nje shpjegim te shkurter ne shqip, profesional dhe konstruktiv.
            Jep 3-4 fjali dhe mbylle me nje keshille praktike.
            """,
            $"""
            Projekti: {context.Project.Title}
            Ministria: {context.Ministry.Name}
            KPI aktual: {context.Factors.KpiPercent}%
            Trendi KPI (30 dite): {FormatSigned(context.Factors.KpiTrend)}%
            Dite te mbetura: {context.Factors.DaysRemaining}
            Hapat e workflow te perfunduar: {context.Factors.WorkflowCompletion}%
            Koha e kaluar e projektit: {context.Factors.DaysElapsedPercent}%
            Hapa me vonese: {(context.Factors.HasOverdueSteps ? "Po" : "Jo")}
            Dite pa update: {context.Factors.DaysSinceLastUpdate}
            Rezultati i rrezikut: {score}/100
            """,
            420);

        var analysis = new AiRiskAnalysisDto(
            context.Project.Id,
            score,
            riskLevel.Level,
            riskLevel.Label,
            riskLevel.Color,
            aiText.Text ?? BuildFallbackRiskExplanation(context, score),
            recommendations,
            factorDtos,
            DateTime.Now.ToString("dd MMM yyyy HH:mm"),
            aiText.UsedAi,
            aiText.Warning);

        riskCache[cacheKey] = analysis;
        TriggerPredictiveNotificationIfNeeded(context.Project, score);
        return analysis;
    }

    public IReadOnlyList<AiHeatmapItemDto> GetRiskHeatmap(string userId)
    {
        var user = repository.GetUserById(userId);
        if (user is null)
        {
            return [];
        }

        return FilterProjects(user)
            .Select(project =>
            {
                var risk = GetProjectRiskAnalysis(userId, project.Id);
                var ministryName = repository.GetMinistries().First(item => item.Id == project.MinistryId).Name;
                return risk is null
                    ? null
                    : new AiHeatmapItemDto(project.Id, project.Title, ministryName, risk.Score, risk.Label, risk.Color);
            })
            .Where(item => item is not null)
            .Cast<AiHeatmapItemDto>()
            .OrderByDescending(item => item.Score)
            .ToList();
    }

    public IReadOnlyList<SmartAlertRecommendationDto> GetSmartAlerts(string userId, bool forceRefresh = false)
    {
        var user = repository.GetUserById(userId);
        if (user is null)
        {
            return [];
        }

        var results = new List<SmartAlertRecommendationDto>();
        foreach (var project in FilterProjects(user).Select(BuildDerivedProject).Where(project => project.Status != ProjectStatus.Cancelled))
        {
            var risk = GetProjectRiskAnalysis(userId, project.Id, forceRefresh);
            if (risk is null || risk.Score < 35)
            {
                continue;
            }

            var cacheKey = $"alert:{project.Id}:{risk.Score}";
            if (!forceRefresh && smartAlertsCache.TryGetValue(cacheKey, out var cached))
            {
                results.Add(cached);
                continue;
            }

            var aiText = TryCallClaude(
                """
                Gjenero nje alert te zgjuar dhe konstruktiv per nje projekt publik.
                Kthe vetem JSON me fushat severity, title, message, recommended_actions dhe urgency_score.
                Teksti duhet te jete ne shqip.
                """,
                $"""
                Projekti: {project.Title}
                KPI: {project.Kpi}%
                Risk score: {risk.Score}
                Risk explanation: {risk.Explanation}
                """,
                320);

            var smartAlert = TryParseSmartAlert(aiText.Text, project, risk) ?? BuildFallbackSmartAlert(project, risk);
            smartAlertsCache[cacheKey] = smartAlert;
            results.Add(smartAlert);
        }

        return results.OrderByDescending(item => item.UrgencyScore).Take(8).ToList();
    }

    public AiProjectSummaryDto? GenerateProjectSummary(string userId, string projectId, bool forceRefresh = false)
    {
        var context = BuildProjectContext(userId, projectId);
        if (context is null)
        {
            return null;
        }

        var cacheKey = $"summary:{projectId}:{context.Hash}";
        if (!forceRefresh && summaryCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var summaryPrompt = $"""
        PROJEKTI: {context.Project.Title}
        MINISTRIA: {context.Ministry.Name}
        STATUSI: {context.Project.Status} | FAZA: {context.Project.ApprovalStage}
        KPI: {context.Project.Kpi}% | AFATI: {context.Project.DueDate:yyyy-MM-dd}
        DITE TE MBETURA: {context.Factors.DaysRemaining}
        PERGJEGJESI: {context.Project.OwnerName}

        WORKFLOW ({context.CompletedWorkflowSteps}/{context.TotalWorkflowSteps} hapa):
        {context.WorkflowSummary}

        KOMENTET E FUNDIT:
        {context.RecentCommentsSummary}

        AKTIVITETI (30 dite te fundit):
        {context.ActivitySummary}
        """;

        var aiText = TryCallClaude(
            """
            Ti je nje asistent ekzekutiv per Ministren e Inovacionit te Shqiperise.
            Gjenero permbledhje ekzekutive profesionale, te qarta dhe te sakta.
            Shkruaj ne shqip, ton profesional, maksimum 250 fjale.
            Strukturo me 5 pika te shkurtra.
            """,
            summaryPrompt,
            650);

        var dto = new AiProjectSummaryDto(
            context.Project.Id,
            aiText.Text ?? BuildFallbackSummary(context),
            DateTime.Now.ToString("dd MMM yyyy HH:mm"),
            aiText.UsedAi,
            aiText.Warning);

        summaryCache[cacheKey] = dto;
        return dto;
    }

    public PdfExtractionResultDto ExtractProjectFromPdf(string userId, PdfExtractionRequestDto request)
    {
        var user = repository.GetUserById(userId);
        if (user is null)
        {
            return new PdfExtractionResultDto("Pa titull", null, null, null, null, null, null, [], [], null, 0, false, "Perdoruesi nuk u gjet.");
        }

        var aiText = TryCallClaude(
            """
            Lexo kete dokument PDF dhe ekstrakto informacionin e projektit ne JSON.
            Kthe VETEM JSON me fushat:
            title, description, ministry, responsible_person, start_date, deadline, kpi_percent, objectives, workflow_steps, budget, confidence.
            workflow_steps duhet te jete liste me objekte title dhe description.
            """,
            $"""
            file_name: {request.FileName}
            pdf_base64: {request.PdfBase64}
            """,
            1400);

        var result = TryParsePdfExtraction(aiText.Text)
            ?? BuildFallbackPdfExtraction(user, request.FileName, aiText.Warning);

        pdfHistory[$"{userId}:{DateTime.UtcNow.Ticks}"] = new PdfExtractionHistoryDto(
            request.FileName,
            result.Confidence,
            DateTime.Now.ToString("dd MMM yyyy HH:mm"),
            result.UsedAi);

        return result;
    }

    public IReadOnlyList<PdfExtractionHistoryDto> GetPdfExtractionHistory(string userId) =>
        pdfHistory
            .Where(item => item.Key.StartsWith($"{userId}:", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Key)
            .Take(5)
            .Select(item => item.Value)
            .ToList();

    public AiChatResponseDto SendChatMessage(string userId, AiChatRequestDto request)
    {
        var user = repository.GetUserById(userId);
        if (user is null)
        {
            return new AiChatResponseDto("Perdoruesi nuk u gjet.", false, "Perdoruesi nuk u gjet.");
        }

        var visibleProjects = FilterProjects(user).Select(BuildDerivedProject).ToList();
        var context = new
        {
            role = user.Role.ToString(),
            ministry = user.MinistryId is null ? null : repository.GetMinistries().FirstOrDefault(item => item.Id == user.MinistryId)?.Name,
            projects = visibleProjects.Select(project => new
            {
                project.Title,
                project.Kpi,
                project.Progress,
                project.Status,
                project.DueDate,
                project.OwnerName
            }).ToList(),
            alerts = GetSmartAlerts(userId).Take(5).Select(item => new { item.ProjectTitle, item.Severity, item.UrgencyScore }).ToList()
        };

        var aiText = TryCallClaude(
            $"""
            Ti je asistenti inteligjent i platformes Innovation4Albania.
            Pergjigju vetem ne shqip.
            Roli i perdoruesit: {user.Role}
            {(IsMinistryScopedRole(user) ? $"Ministria e perdoruesit: {context.ministry}" : "Perdoruesi ka akses te gjere.")}
            Mos shpik te dhena. Nese nuk ke informacion, thuaje qarte.
            """,
            $"KONTEKSTI:\n{JsonSerializer.Serialize(context)}\n\nPYETJA:\n{request.Message}",
            700);

        return new AiChatResponseDto(
            aiText.Text ?? BuildFallbackChatResponse(request.Message, visibleProjects),
            aiText.UsedAi,
            aiText.Warning);
    }

    public BulkAiReportDto? GenerateBulkAtRiskReport(string userId)
    {
        var user = repository.GetUserById(userId);
        if (user is null || !IsDirectorLike(user))
        {
            return null;
        }

        var riskyProjects = GetRiskHeatmap(userId).Where(item => item.Score >= 45).Take(12).ToList();
        var sections = riskyProjects.Select(item =>
        {
            var summary = GenerateProjectSummary(userId, item.ProjectId)?.Summary ?? "Permbledhja nuk u gjenerua.";
            return $"<section><h2>{item.Title}</h2><p><strong>{item.MinistryName}</strong> - Rreziku: {item.Score}/100 - {item.Label}</p><div>{System.Net.WebUtility.HtmlEncode(summary).Replace("\n", "<br/>")}</div></section>";
        }).ToList();

        var html = $@"
        <html lang=""sq""><head><meta charset=""utf-8""/><title>Raport AI i Projekteve ne Risk</title>
        <style>body{{font-family:Segoe UI,sans-serif;padding:32px;line-height:1.6;color:#0f172a}}h1{{color:#0b4f9c}}section{{border:1px solid #dbe5f0;border-radius:18px;padding:18px;margin:18px 0}}</style>
        </head><body>
        <h1>Raport AI per Projektet ne Risk</h1>
        <p>Gjeneruar: {DateTime.Now:dd MMM yyyy HH:mm}</p>
        {string.Join("", sections)}
        </body></html>";

        return new BulkAiReportDto(html, riskyProjects.Count, DateTime.Now.ToString("dd MMM yyyy HH:mm"), true, null);
    }

    private ProjectAiContext? BuildProjectContext(string userId, string projectId)
    {
        var user = repository.GetUserById(userId);
        if (user is null)
        {
            return null;
        }

        var project = FilterProjects(user)
            .Where(item => item.Id == projectId)
            .Select(BuildDerivedProject)
            .FirstOrDefault();
        if (project is null)
        {
            return null;
        }

        var ministry = repository.GetMinistries().First(item => item.Id == project.MinistryId);
        var workflow = repository.GetWorkflowSteps().Where(item => item.ProjectId == project.Id).ToList();
        var notes = repository.GetNotes().Where(item => item.ProjectId == project.Id).OrderByDescending(item => item.CreatedUtc).Take(3).ToList();
        var history = repository.GetHistoryLogs().Where(item => item.ProjectId == project.Id).OrderByDescending(item => item.TimestampUtc).ToList();

        var factors = BuildRiskFactors(project, workflow, history);
        var completedSteps = workflow.Count(item => item.Progress >= 100 || item.Status.Contains("Completed", StringComparison.OrdinalIgnoreCase));
        var workflowSummary = workflow.Any()
            ? string.Join("; ", workflow.Take(6).Select(item => $"Hapi {item.StepNumber}: {item.Description} ({item.Progress}%  -  {item.Status})"))
            : "Nuk ka hapa workflow te regjistruar.";
        var commentsSummary = notes.Any()
            ? string.Join(" | ", notes.Select(item => $"{item.AuthorName}: {item.Content}"))
            : "Nuk ka komente te fundit.";
        var lastThirtyDays = DateTime.UtcNow.AddDays(-30);
        var activitySummary = history.Any(item => item.TimestampUtc >= lastThirtyDays)
            ? string.Join(" | ", history.Where(item => item.TimestampUtc >= lastThirtyDays).Take(6).Select(item => $"{item.ActionType}: {item.FieldName}"))
            : "Aktivitet i ulet ne 30 ditet e fundit.";
        var hash = $"{project.Kpi}|{project.Progress}|{project.DueDate:yyyyMMdd}|{workflow.Count}|{history.FirstOrDefault()?.TimestampUtc.Ticks ?? 0}";

        return new ProjectAiContext(project, ministry, factors, completedSteps, workflow.Count, workflowSummary, commentsSummary, activitySummary, hash);
    }

    private RiskFactors BuildRiskFactors(InnovationProject project, IReadOnlyList<WorkflowStep> workflow, IReadOnlyList<HistoryLog> history)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var totalDays = Math.Max(1, project.DueDate.DayNumber - project.StartDate.DayNumber);
        var elapsedDays = Math.Max(0, today.DayNumber - project.StartDate.DayNumber);
        var daysRemaining = project.DueDate.DayNumber - today.DayNumber;
        var workflowCompletion = workflow.Count == 0 ? project.Progress : (int)Math.Round(workflow.Average(item => item.Progress));
        var overdueSteps = workflow.Any(item => item.DueDate < today && item.Progress < 100);
        var lastUpdate = history.FirstOrDefault()?.TimestampUtc ?? DateTime.UtcNow.AddDays(-21);
        var kpiTrend = CalculateKpiTrend(project.Kpi, history);

        return new RiskFactors(
            project.Kpi,
            kpiTrend,
            daysRemaining,
            workflowCompletion,
            Math.Min(100, (int)Math.Round((double)elapsedDays / totalDays * 100)),
            overdueSteps,
            Math.Max(0, (int)(DateTime.UtcNow - lastUpdate).TotalDays));
    }

    private static int CalculateRiskScore(RiskFactors factors)
    {
        var score = 0;
        if (factors.KpiPercent < 40) score += 35;
        else if (factors.KpiPercent < 60) score += 20;
        else if (factors.KpiPercent < 75) score += 10;

        if (factors.KpiTrend < -10) score += 20;
        else if (factors.KpiTrend < -5) score += 10;

        var progressGap = factors.DaysElapsedPercent - factors.WorkflowCompletion;
        if (progressGap > 30) score += 25;
        else if (progressGap > 15) score += 15;

        if (factors.DaysRemaining < 7) score += 20;
        else if (factors.DaysRemaining < 14) score += 10;

        if (factors.HasOverdueSteps) score += 15;
        if (factors.DaysSinceLastUpdate > 14) score += 10;
        return Math.Min(score, 100);
    }

    private static (string Level, string Color, string Label) GetRiskLevel(int score) =>
        score >= 70 ? ("critical", "#EF4444", "Kritik")
        : score >= 45 ? ("high", "#F97316", "I Larte")
        : score >= 25 ? ("medium", "#F59E0B", "Mesatar")
        : ("low", "#22C55E", "I Ulet");

    private IReadOnlyList<AiRiskFactorDto> BuildRiskFactorDtos(RiskFactors factors)
    {
        var items = new List<AiRiskFactorDto>();
        void add(string key, string label, string value, int contribution)
        {
            if (contribution > 0) items.Add(new AiRiskFactorDto(key, label, value, contribution));
        }

        add("kpi", "KPI aktual", $"{factors.KpiPercent}%", factors.KpiPercent < 40 ? 35 : factors.KpiPercent < 60 ? 20 : factors.KpiPercent < 75 ? 10 : 0);
        add("trend", "Trendi i KPI", $"{FormatSigned(factors.KpiTrend)}%", factors.KpiTrend < -10 ? 20 : factors.KpiTrend < -5 ? 10 : 0);
        var progressGap = factors.DaysElapsedPercent - factors.WorkflowCompletion;
        add("progress_gap", "Mosperputhja kohe/progres", $"{progressGap} pike", progressGap > 30 ? 25 : progressGap > 15 ? 15 : 0);
        add("deadline", "Ditet e mbetura", $"{factors.DaysRemaining}", factors.DaysRemaining < 7 ? 20 : factors.DaysRemaining < 14 ? 10 : 0);
        add("overdue", "Hapa me vonese", factors.HasOverdueSteps ? "Po" : "Jo", factors.HasOverdueSteps ? 15 : 0);
        add("inactive", "Dite pa update", $"{factors.DaysSinceLastUpdate}", factors.DaysSinceLastUpdate > 14 ? 10 : 0);
        return items.OrderByDescending(item => item.Contribution).ToList();
    }

    private static IReadOnlyList<string> BuildRecommendations(RiskFactors factors, int score)
    {
        var items = new List<string>();
        if (score >= 45) items.Add("Organizoni nje takim pune per rishikimin e planit dhe pergjegjesive.");
        if (factors.HasOverdueSteps) items.Add("Rishikoni menjehere hapat e workflow-it me vonese.");
        if (factors.DaysSinceLastUpdate > 14) items.Add("Kerkoni perditesim operacional nga ekipi brenda 24 oreve.");
        if (factors.KpiPercent < 60) items.Add("Percaktoni nje plan rikuperimi per KPI-ne me objektiva javore.");
        if (factors.DaysRemaining < 14) items.Add("Perditesoni afatin ose rishperndani ngarkesen per te shmangur vonesen.");
        return items.Distinct().Take(4).ToList();
    }

    private void TriggerPredictiveNotificationIfNeeded(InnovationProject project, int score)
    {
        if (previousRiskScores.TryGetValue(project.Id, out var previous) && score - previous > 15)
        {
            AddNotification("director", project.Id, "kpi_alert", $"Rreziku i projektit {project.Title} u rrit ndjeshem", $"Rreziku i projektit {project.Title} u rrit nga {previous} ne {score} pike.");
            var ministryDirector = repository.GetUsers().FirstOrDefault(item => item.Role == UserRole.NucleusDirector && string.Equals(item.MinistryId, project.MinistryId, StringComparison.OrdinalIgnoreCase));
            if (ministryDirector is not null)
            {
                AddNotification(ministryDirector.Id, project.Id, "kpi_alert", $"Rreziku i projektit {project.Title} u rrit ndjeshem", $"Rreziku i projektit {project.Title} u rrit nga {previous} ne {score} pike.");
            }
            var expert = repository.GetUsers().FirstOrDefault(item => item.Role == UserRole.Expert && string.Equals(item.MinistryId, project.MinistryId, StringComparison.OrdinalIgnoreCase));
            if (expert is not null)
            {
                AddNotification(expert.Id, project.Id, "kpi_alert", $"Rreziku i projektit {project.Title} u rrit ndjeshem", $"Rreziku i projektit {project.Title} u rrit nga {previous} ne {score} pike.");
            }
        }
        previousRiskScores[project.Id] = score;
    }

    private void AddNotification(string recipientId, string projectId, string type, string title, string message)
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

    private static int CalculateKpiTrend(int currentKpi, IReadOnlyList<HistoryLog> history)
    {
        var recentKpiLog = history
            .Where(item => item.TimestampUtc >= DateTime.UtcNow.AddDays(-30) && item.FieldName.Contains("KPI", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.TimestampUtc)
            .FirstOrDefault();

        return recentKpiLog is null || !int.TryParse(recentKpiLog.PreviousValue, out var previousKpi)
            ? 0
            : currentKpi - previousKpi;
    }

    private IEnumerable<InnovationProject> FilterProjects(PlatformUser user)
    {
        var projects = repository.GetProjects();
        return IsMinistryScopedRole(user) && !string.IsNullOrWhiteSpace(user.MinistryId)
            ? projects.Where(project => string.Equals(project.MinistryId, user.MinistryId, StringComparison.OrdinalIgnoreCase))
            : projects;
    }

    private static bool IsDirectorLike(PlatformUser user) =>
        user.Role is UserRole.Director or UserRole.NucleusDirector;

    private static bool IsMinistryScopedRole(PlatformUser user) =>
        user.Role is UserRole.Expert or UserRole.NucleusDirector;

    private ClaudeCallResult TryCallClaude(string systemPrompt, string userMessage, int maxTokens)
    {
        var apiKey = configuration["Anthropic:ApiKey"] ?? configuration["ANTHROPIC_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ClaudeCallResult(null, false, "ANTHROPIC_API_KEY nuk eshte konfiguruar. Po perdoret analiza rezerve.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ClaudeApiUrl);
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(new
            {
                model = ClaudeModel,
                max_tokens = maxTokens,
                system = systemPrompt,
                messages = new[] { new { role = "user", content = userMessage } }
            }), Encoding.UTF8, "application/json");

            var response = httpClient.Send(request);
            if (!response.IsSuccessStatusCode)
            {
                return new ClaudeCallResult(null, false, $"Claude API nuk eshte i disponueshem ({(int)response.StatusCode}).");
            }

            using var stream = response.Content.ReadAsStream();
            using var document = JsonDocument.Parse(stream);
            var text = document.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
            return new ClaudeCallResult(text, true, null);
        }
        catch (Exception ex)
        {
            return new ClaudeCallResult(null, false, $"Sherbimi AI nuk u pergjigj: {ex.Message}");
        }
    }

    private static string BuildFallbackRiskExplanation(ProjectAiContext context, int score)
    {
        var lines = new List<string>
        {
            $"Projekti {context.Project.Title} ka rezultat rreziku {score}/100 per shkak te kombinimit te KPI-se, progresit dhe afatit."
        };
        if (context.Factors.KpiPercent < 60) lines.Add("KPI-ja aktuale eshte nen nivelin e sigurise operative.");
        if (context.Factors.HasOverdueSteps) lines.Add("Ekzistojne hapa workflow me vonese qe po ndikojne ritmin e ekzekutimit.");
        if (context.Factors.DaysElapsedPercent > context.Factors.WorkflowCompletion) lines.Add("Progresi i punes eshte prapa ritmit te kohes se konsumuar.");
        lines.Add("Rekomandohet nje rishikim i menjehershem i planit te punes dhe i pronesise se detyrave.");
        return string.Join(" ", lines);
    }

    private static string BuildFallbackSummary(ProjectAiContext context) =>
        $"""
        1. Statusi i Pergjithshem: Projekti {context.Project.Title} i ministrise {context.Ministry.Name} ndodhet ne statusin {context.Project.Status} dhe fazen {context.Project.ApprovalStage}.
        2. Progresi dhe KPI: KPI aktual eshte {context.Project.Kpi}% ndersa progresi i raportuar eshte {context.Project.Progress}%. Jane perfunduar {context.CompletedWorkflowSteps} nga {context.TotalWorkflowSteps} hapa te workflow-it.
        3. Sfidat Kryesore: Faktori kryesor i vemendjes lidhet me afatin, ritmin e progresit dhe perditesimet operative.
        4. Hapat Tjeter: Rishikoni workflow-in, konfirmoni pergjegjesit dhe dokumentoni hapat prioritare per periudhen e ardhshme.
        5. Vleresimi Perfundimtar: Projekti kerkon monitorim aktiv dhe vendimmarrje te disiplinuar.
        """;

    private static SmartAlertRecommendationDto BuildFallbackSmartAlert(InnovationProject project, AiRiskAnalysisDto risk) =>
        new(
            project.Id,
            project.Title,
            risk.Level,
            $"Vemendje per {project.Title}",
            risk.Explanation,
            Math.Min(100, risk.Score + 10),
            risk.Recommendations,
            false);

    private static SmartAlertRecommendationDto? TryParseSmartAlert(string? content, InnovationProject project, AiRiskAnalysisDto risk)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            return new SmartAlertRecommendationDto(
                project.Id,
                project.Title,
                root.TryGetProperty("severity", out var severity) ? severity.GetString() ?? risk.Level : risk.Level,
                root.TryGetProperty("title", out var title) ? title.GetString() ?? $"Alert per {project.Title}" : $"Alert per {project.Title}",
                root.TryGetProperty("message", out var message) ? message.GetString() ?? risk.Explanation : risk.Explanation,
                root.TryGetProperty("urgency_score", out var urgency) ? urgency.GetInt32() : risk.Score,
                root.TryGetProperty("recommended_actions", out var actions)
                    ? actions.EnumerateArray().Select(item => item.GetString() ?? string.Empty).Where(item => !string.IsNullOrWhiteSpace(item)).ToList()
                    : risk.Recommendations,
                true);
        }
        catch
        {
            return null;
        }
    }

    private static PdfExtractionResultDto? TryParsePdfExtraction(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var steps = root.TryGetProperty("workflow_steps", out var workflowSteps)
                ? workflowSteps.EnumerateArray()
                    .Select(item => new PdfWorkflowStepSuggestionDto(
                        item.TryGetProperty("title", out var title) ? title.GetString() ?? "Hap" : "Hap",
                        item.TryGetProperty("description", out var description) ? description.GetString() ?? string.Empty : string.Empty))
                    .ToList()
                : [];

            var objectives = root.TryGetProperty("objectives", out var objectivesElement)
                ? objectivesElement.EnumerateArray().Select(item => item.GetString() ?? string.Empty).Where(item => !string.IsNullOrWhiteSpace(item)).ToList()
                : [];

            return new PdfExtractionResultDto(
                root.TryGetProperty("title", out var titleValue) ? titleValue.GetString() ?? "Projekt i ri" : "Projekt i ri",
                root.TryGetProperty("description", out var descriptionValue) ? descriptionValue.GetString() : null,
                root.TryGetProperty("ministry", out var ministryValue) ? ministryValue.GetString() : null,
                root.TryGetProperty("responsible_person", out var ownerValue) ? ownerValue.GetString() : null,
                root.TryGetProperty("start_date", out var startDate) ? startDate.GetString() : null,
                root.TryGetProperty("deadline", out var deadline) ? deadline.GetString() : null,
                root.TryGetProperty("kpi_percent", out var kpi) && kpi.ValueKind == JsonValueKind.Number ? kpi.GetInt32() : null,
                objectives,
                steps,
                root.TryGetProperty("budget", out var budget) ? budget.GetString() : null,
                root.TryGetProperty("confidence", out var confidence) && confidence.ValueKind == JsonValueKind.Number ? confidence.GetInt32() : 0,
                true,
                null);
        }
        catch
        {
            return null;
        }
    }

    private PdfExtractionResultDto BuildFallbackPdfExtraction(PlatformUser user, string fileName, string? warning) =>
        new(
            System.IO.Path.GetFileNameWithoutExtension(fileName).Replace('-', ' '),
            "Pershkrimi duhet verifikuar manualisht.",
            user.MinistryId is null ? null : repository.GetMinistries().FirstOrDefault(item => item.Id == user.MinistryId)?.Name,
            user.FullName,
            DateTime.Today.ToString("yyyy-MM-dd"),
            DateTime.Today.AddMonths(3).ToString("yyyy-MM-dd"),
            50,
            ["Objektivi duhet verifikuar manualisht."],
            [new PdfWorkflowStepSuggestionDto("Hapi 1", "Konfirmoni te dhenat e nxjerra nga dokumenti.")],
            null,
            32,
            false,
            warning ?? "Ekstraktimi AI nuk ishte i disponueshem. Fushat u paraplotesuan me vlera rezerve.");

    private static string BuildFallbackChatResponse(string question, IReadOnlyList<InnovationProject> projects)
    {
        if (question.Contains("risk", StringComparison.OrdinalIgnoreCase))
        {
            var risky = projects.Where(project => project.Kpi < 60).Take(3).Select(project => $"{project.Title} ({project.Kpi}%)");
            return risky.Any()
                ? $"Projektet qe kerkojne vemendje jane: {string.Join(", ", risky)}."
                : "Nuk u identifikuan projekte me sinjal te forte risku ne te dhenat aktuale.";
        }

        if (question.Contains("afat", StringComparison.OrdinalIgnoreCase))
        {
            var dueSoon = projects.Where(project => project.DueDate <= DateOnly.FromDateTime(DateTime.Today).AddDays(7)).Take(5).Select(project => $"{project.Title} ({project.DueDate:dd MMM})");
            return dueSoon.Any()
                ? $"Projektet me afat te afert jane: {string.Join(", ", dueSoon)}."
                : "Nuk ka projekte me afat brenda javes sipas te dhenave aktuale.";
        }

        return "Asistenti AI nuk ishte i disponueshem ne kete moment. Ju lutem provoni perseri ose perdorni seksionin perkates ne panel.";
    }

    private InnovationProject BuildDerivedProject(InnovationProject project) =>
        ProjectMetricsCalculator.WithDerivedMetrics(
            project,
            repository.GetTasks().Where(item => item.ProjectId == project.Id),
            repository.GetWorkflowSteps().Where(item => item.ProjectId == project.Id),
            repository.GetProjectMilestones().Where(item => item.ProjectId == project.Id));

    private static string FormatSigned(int value) => value > 0 ? $"+{value}" : value.ToString();

    private sealed record RiskFactors(
        int KpiPercent,
        int KpiTrend,
        int DaysRemaining,
        int WorkflowCompletion,
        int DaysElapsedPercent,
        bool HasOverdueSteps,
        int DaysSinceLastUpdate);

    private sealed record ProjectAiContext(
        InnovationProject Project,
        Ministry Ministry,
        RiskFactors Factors,
        int CompletedWorkflowSteps,
        int TotalWorkflowSteps,
        string WorkflowSummary,
        string RecentCommentsSummary,
        string ActivitySummary,
        string Hash);

    private sealed record ClaudeCallResult(string? Text, bool UsedAi, string? Warning);
}



