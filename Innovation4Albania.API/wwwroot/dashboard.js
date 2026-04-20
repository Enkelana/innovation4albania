let workspaceState = null;
let currentUserId = null;
let currentProjectDetail = null;
let workspacePollId = null;
let onboardingTimeoutId = null;
let selectedCalendarMonth = 0;
let smartAlertsState = [];
let riskHeatmapState = [];
let riskHeatmapFilter = "critical";
let pdfExtractionHistoryState = [];
let aiChatHistory = [];
let importPreviewState = null;

const muajt2026 = [
    "Janar", "Shkurt", "Mars", "Prill", "Maj", "Qershor",
    "Korrik", "Gusht", "Shtator", "Tetor", "Nentor", "Dhjetor"
];

const perkthimStatusi = { Active: "Aktiv", InProcess: "Ne proces", Completed: "Perfunduar", Cancelled: "Anuluar" };
const perkthimRisku = { High: "Risk i larte", Medium: "Risk mesatar", Low: "Risk i ulet" };
const perkthimGjendjeje = { "On track": "Ne rruge te mbare", Watchlist: "Ne vezhgim", "Needs attention": "Kerkon vemendje", "No data": "Pa te dhena" };
const perkthimStatusiWorkflow = { Completed: "Perfunduar", "In progress": "Ne proces", "Pending review": "Ne pritje shqyrtimi", Pending: "Ne pritje" };
const perkthimStatusiDetyrave = { todo: "Per t'u bere", in_progress: "Ne progres", review: "Ne shqyrtim", done: "Perfunduar" };
const perkthimPrioritetitDetyrave = { low: "E ulet", medium: "Mesatare", high: "E larte", urgent: "Urgjente" };

function statusiShqip(vlera) { return perkthimStatusi[vlera] ?? vlera; }
function riskuShqip(vlera) { return perkthimRisku[vlera] ?? vlera; }
function gjendjaShqip(vlera) { return perkthimGjendjeje[vlera] ?? vlera; }
function statusiWorkflowShqip(vlera) { return perkthimStatusiWorkflow[vlera] ?? vlera; }
function statusiDetyresShqip(vlera) { return perkthimStatusiDetyrave[vlera] ?? vlera; }
function prioritetiDetyresShqip(vlera) { return perkthimPrioritetitDetyrave[vlera] ?? vlera; }
function klasaGjendjes(vlera) { return vlera === "On track" ? "success" : vlera === "Watchlist" ? "warning" : "critical"; }

function parseLooseDate(value) {
    if (!value) {
        return null;
    }
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
}

function isMinisterLike(role) {
    return role === "Minister" || role === "PrimeMinister";
}

function isDirectorLike(role) {
    return role === "Director" || role === "NucleusDirector";
}

function isExpertLike(role) {
    return role === "Expert";
}

function isMinistryScopedRole(role) {
    return role === "Expert" || role === "NucleusDirector";
}

const pamjetSipasRolit = {
    Minister: ["overview", "projects", "charts", "ministries"],
    PrimeMinister: ["overview", "projects", "charts", "ministries"],
    Director: ["overview", "projects", "charts", "okrs", "project-detail", "ministries", "experts", "documents", "tasks", "workflow", "calendar", "import", "alerts", "sync", "notifications", "logs"],
    NucleusDirector: ["overview", "projects", "charts", "okrs", "project-detail", "ministries", "experts", "documents", "tasks", "workflow", "calendar", "import", "alerts", "sync", "notifications", "logs"],
    Expert: ["overview", "projects", "charts", "okrs", "project-detail", "ministries", "documents", "workflow", "calendar", "alerts", "notifications"]
};

const seksionetSipasPamjes = {
    overview: ["statsGrid", "expertProjectsOverviewSection", "macroCharts", "overviewInsightsRow", "directorPrioritySection", "analyticsSection", "overviewBoard", "overviewAlertsProgressRow", "overviewMinistryProgressSection", "ministerOverviewMinistryProgressSection"],
    projects: ["directorPrioritySection", "projects", "projectDetailSection"],
    charts: ["statsGrid", "directorPrioritySection", "macroCharts", "macroRankingSection", "overviewInsightsRow", "ministryProgressSection", "okrObjectivesOverviewSection", "analyticsSection"],
    okrs: ["directorPrioritySection", "okrsSection"],
    "project-detail": ["projects", "projectDetailSection"],
    ministries: ["directorPrioritySection", "ministriesSection"],
    experts: ["directorPrioritySection", "expertsSection"],
    documents: ["directorPrioritySection", "documentsSection"],
    tasks: ["directorPrioritySection", "tasksSection"],
    workflow: ["directorPrioritySection", "workflowSection", "notesSection"],
    calendar: ["directorPrioritySection", "calendarSection"],
    notifications: ["directorPrioritySection", "notificationsSection"],
    import: ["importSection"],
    alerts: ["overviewInsightsRow", "overviewBoard", "alertsConfigSection"],
    sync: ["alertsConfigSection"],
    logs: ["logs"]
};

function pamjaAktive(role) {
    const path = window.location.pathname.toLowerCase();
    const inferred =
        path.endsWith("/director/calendar") || path.endsWith("/expert/calendar") || path.endsWith("/minister/calendar") ? "calendar" :
        path.endsWith("/director/settings") ? "sync" :
        path.endsWith("/notifications") ? "notifications" :
        path.endsWith("/director/import") ? "import" :
        path.endsWith("/director/tasks") ? "tasks" :
        path.endsWith("/director/okrs") || path.endsWith("/expert/okrs") ? "okrs" :
        new URLSearchParams(window.location.search).get("view") || "overview";
    const view = inferred;
    const teLejuara = pamjetSipasRolit[role] ?? ["overview"];
    return teLejuara.includes(view) ? view : teLejuara[0];
}

function currentView() {
    const role = workspaceState?.dashboard?.currentUser?.role ?? "Director";
    return pamjaAktive(role);
}

function rrugaBazePanelit(role) {
    return isExpertLike(role) ? "/expert/dashboard" : "/dashboard.html";
}

function rrugaPamjes(role, view) {
    if (view === "calendar") {
        return isExpertLike(role) ? "/expert/calendar" : isMinisterLike(role) ? "/minister/calendar" : "/director/calendar";
    }
    if (view === "import") {
        return "/director/import";
    }
    if (view === "tasks" && isDirectorLike(role)) {
        return "/director/tasks";
    }
    if (view === "okrs") {
        return isExpertLike(role) ? "/expert/okrs" : "/director/okrs";
    }
    if (view === "notifications") {
        return isExpertLike(role) ? "/expert/dashboard?view=notifications" : "/notifications";
    }
    if (view === "sync" && isDirectorLike(role)) {
        return "/director/settings";
    }
    return `${rrugaBazePanelit(role)}?view=${view}`;
}

function formatDate(value) {
    return new Intl.DateTimeFormat("sq-AL", {
        day: "2-digit",
        month: "short",
        year: "numeric"
    }).format(new Date(value));
}

function getAiAlertSettings() {
    try {
        return JSON.parse(localStorage.getItem("i4a-ai-alerts") || "{\"enabled\":true,\"frequency\":\"daily\"}");
    } catch {
        return { enabled: true, frequency: "daily" };
    }
}

function setAiAlertSettings(settings) {
    localStorage.setItem("i4a-ai-alerts", JSON.stringify(settings));
}

function calculateLocalRisk(project, detail = null) {
    const startDate = new Date(project.startDate || detail?.startDate || new Date());
    const dueDate = new Date(project.dueDate || detail?.dueDate || new Date());
    const today = new Date();
    const totalDays = Math.max(1, Math.ceil((dueDate - startDate) / 86400000));
    const elapsedDays = Math.max(0, Math.ceil((today - startDate) / 86400000));
    const daysRemaining = Math.ceil((dueDate - today) / 86400000);
    const workflowCompletion = detail?.workflow?.length
        ? Math.round(detail.workflow.reduce((sum, item) => sum + item.progress, 0) / detail.workflow.length)
        : project.progress;
    const hasOverdueSteps = !!detail?.workflow?.some((item) => new Date(item.dueDate) < today && item.progress < 100);
    const daysSinceLastUpdate = detail?.history?.length
        ? Math.max(0, Math.floor((today - new Date(detail.history[0].timestamp)) / 86400000))
        : 7;
    const score = Math.min(100,
        (project.kpi < 40 ? 35 : project.kpi < 60 ? 20 : project.kpi < 75 ? 10 : 0) +
        (elapsedDays / totalDays * 100 - workflowCompletion > 30 ? 25 : elapsedDays / totalDays * 100 - workflowCompletion > 15 ? 15 : 0) +
        (daysRemaining < 7 ? 20 : daysRemaining < 14 ? 10 : 0) +
        (hasOverdueSteps ? 15 : 0) +
        (daysSinceLastUpdate > 14 ? 10 : 0));
    return score >= 70 ? { score, label: "Kritik", color: "#EF4444" }
        : score >= 45 ? { score, label: "I Lartë", color: "#F97316" }
        : score >= 25 ? { score, label: "Mesatar", color: "#F59E0B" }
        : { score, label: "I Ulët", color: "#22C55E" };
}

function riskBadge(project, detail = null) {
    const risk = calculateLocalRisk(project, detail);
    return `<span class="pill ai-badge" style="background:${risk.color};color:#fff;">Rrezik: ${risk.score} · ${risk.label}</span>`;
}

async function loadAiRiskPanel(projectId, forceRefresh = false) {
    // Ky panel ngarkon dhe shfaq analizen AI per projektin e zgjedhur.
    const host = document.getElementById("projectAiRiskPanel");
    if (!host || !projectId) {
        return;
    }
    host.innerHTML = '<div class="muted-row">Duke analizuar riskun...</div>';
    try {
        const result = await fetchJson(`/api/ai/projects/${encodeURIComponent(projectId)}/risk?userId=${encodeURIComponent(currentUserId)}&forceRefresh=${forceRefresh}`);
        host.innerHTML = `
            <div class="history-card-top">
                <div>
                    <strong><span class="pill ai-badge" style="background:${result.color};color:#fff;">${result.score}/100 · ${result.label}</span></strong>
                    <p>Analiza AI e riskut për projektin aktual</p>
                </div>
                <div class="history-side-meta">
            <span>${result.usedAi ? "✨ AI" : "Analizë rezervë"}</span>
                    <small>${result.analyzedOn}</small>
                </div>
            </div>
            ${result.warningMessage ? `<div class="feedback-response-box"><strong>Njoftim</strong><p>${result.warningMessage}</p></div>` : ""}
            <p class="task-card-summary">${result.explanation}</p>
            <div class="history-diff-grid">
                ${result.factors.map((factor) => `<div class="history-diff-box"><span>${factor.label}</span><strong>${factor.value}</strong><small>Ndikimi: +${factor.contribution}</small></div>`).join("")}
            </div>
            <div class="meeting-note-box"><strong>Rekomandime</strong><p>${result.recommendations.join("<br>")}</p></div>
            <div class="task-button-row">
                <button class="ghost-button" type="button" id="refreshAiRiskButton">Rifito Analizën</button>
                <button class="ghost-button" type="button" id="reportAiIssueButton">Raporto problem me AI</button>
            </div>
        `;
        document.getElementById("refreshAiRiskButton")?.addEventListener("click", () => loadAiRiskPanel(projectId, true));
        document.getElementById("reportAiIssueButton")?.addEventListener("click", () => showFeedback("Problemi me AI u regjistrua për verifikim.", false));
    } catch {
        host.innerHTML = '<div class="empty-state">Analiza AI nuk u ngarkua dot.</div>';
    }
}

async function renderRiskHeatmap() {
    // Kjo nderton heatmap-in e riskut dhe filtron projektet sipas nivelit.
    const panel = document.getElementById("riskHeatmapPanel");
    const grid = document.getElementById("riskHeatmapGrid");
    const summary = document.getElementById("riskHeatmapSummary");
    if (!panel || !grid || !summary || !workspaceState) {
        return;
    }
    panel.style.display = "";
    summary.innerHTML = "";
    grid.innerHTML = '<div class="muted-row">Duke ngarkuar heatmap-in...</div>';
    try {
        riskHeatmapState = await fetchJson(`/api/ai/risk-heatmap?userId=${encodeURIComponent(currentUserId)}`);
        const bucketFor = (item) => {
            const normalizedLabel = (item.label ?? "").toString().trim().toLowerCase();
            if (normalizedLabel === "kritik") return "critical";
            if (normalizedLabel === "i larte" || normalizedLabel === "i lartë") return "high";
            if (normalizedLabel === "mesatar") return "medium";
            if (normalizedLabel === "i ulet" || normalizedLabel === "i ulët") return "low";

            if (item.score >= 70) return "critical";
            if (item.score >= 45) return "high";
            if (item.score >= 25) return "medium";
            return "low";
        };
        const filterMeta = {
            critical: { title: "Kritik", subtitle: "Rastet qe duan vendim te menjehershem" },
            high: { title: "I larte", subtitle: "Sinjale te forta per ndjekje" },
            medium: { title: "Mesatar", subtitle: "Projektet qe duhen monitoruar" },
            low: { title: "I ulet", subtitle: "Projektet jashte zones se riskut" }
        };
        const counts = {
            critical: riskHeatmapState.filter((item) => bucketFor(item) === "critical").length,
            high: riskHeatmapState.filter((item) => bucketFor(item) === "high").length,
            medium: riskHeatmapState.filter((item) => bucketFor(item) === "medium").length,
            low: riskHeatmapState.filter((item) => bucketFor(item) === "low").length
        };
        const sortedRisk = [...riskHeatmapState].sort((left, right) => right.score - left.score);
        if (!filterMeta[riskHeatmapFilter]) {
            riskHeatmapFilter = "critical";
        }
        const visibleItems = sortedRisk.filter((item) => bucketFor(item) === riskHeatmapFilter);
        summary.innerHTML = `
            ${Object.entries(filterMeta).map(([key, meta]) => `
                <button class="mini-stat-card compact risk-filter-card risk-filter-card-${key} ${riskHeatmapFilter === key ? "active" : ""}" type="button" data-risk-filter="${key}">
                    <span>${meta.title}</span>
                    <strong>${counts[key]}</strong>
                    <small>${meta.subtitle}</small>
                </button>
            `).join("")}
        `;
        grid.innerHTML = `
            <div class="risk-heatmap-toolbar risk-heatmap-toolbar-table">
                <div class="muted-row">Po shfaqen ${visibleItems.length} projekte ne kategorine <strong>${filterMeta[riskHeatmapFilter].title}</strong>.</div>
            </div>
            ${visibleItems.length ? `
                <div class="table-wrap risk-heatmap-table-wrap">
                    <table class="data-table risk-heatmap-table">
                        <thead>
                            <tr>
                                <th>Projekti</th>
                                <th>Ministria</th>
                                <th>Risk score</th>
                                <th>Niveli</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${visibleItems.map((item) => `
                                <tr class="risk-heatmap-row" data-risk-project="${item.projectId}">
                                    <td><strong>${item.title}</strong></td>
                                    <td>${item.ministryName}</td>
                                    <td><span class="pill ai-badge" style="background:${item.color};color:#fff;">${item.score}/100</span></td>
                                    <td><span class="pill ${bucketFor(item) === "critical" ? "critical" : bucketFor(item) === "high" ? "warning" : bucketFor(item) === "medium" ? "neutral" : "success"}">${item.label}</span></td>
                                </tr>
                            `).join("")}
                        </tbody>
                    </table>
                </div>
            ` : '<div class="empty-state">Nuk ka të dhëna risku.</div>'}
        `;
        summary.querySelectorAll("[data-risk-filter]").forEach((button) => {
            button.addEventListener("click", async () => {
                riskHeatmapFilter = button.getAttribute("data-risk-filter");
                await renderRiskHeatmap();
            });
        });
        grid.querySelectorAll("[data-risk-project]").forEach((row) => {
            row.addEventListener("click", async () => {
                const projectId = row.getAttribute("data-risk-project");
                updateViewUrl("project-detail", { projectId });
                await loadProjectDetail(projectId);
                activateDetailTab("risk-ai");
                aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
            });
        });
    } catch {
        summary.innerHTML = "";
        grid.innerHTML = '<div class="empty-state">Heatmap-i AI nuk u ngarkua dot.</div>';
    }
}

async function renderSmartAlerts() {
    if (!workspaceState) {
        return;
    }
    try {
        smartAlertsState = await fetchJson(`/api/ai/smart-alerts?userId=${encodeURIComponent(currentUserId)}`);
        const summary = document.getElementById("smartAlertsFeedbackSummary");
        if (summary) {
            summary.innerHTML = `
                    <span class="pill ai-badge">✨ AI</span>
                <span class="pill neutral">${smartAlertsState.length} alerte inteligjente</span>
                <span class="pill warning">${smartAlertsState.filter((item) => item.severity === "critical" || item.severity === "high").length} prioritare</span>
            `;
        }
    } catch {
        const summary = document.getElementById("smartAlertsFeedbackSummary");
        if (summary) {
            summary.innerHTML = '<div class="empty-state">Nuk ka alerte inteligjente.</div>';
        }
    }
}

async function openAiSummaryModal(forceRefresh = false) {
    if (!currentProjectDetail) {
        showFeedback("Zgjidhni më parë një projekt.", true);
        return;
    }
    const modal = document.createElement("div");
    modal.className = "qr-modal-overlay";
    modal.innerHTML = `
        <div class="qr-modal-card">
            <div class="list-card-header">
                <div>
                    <span class="section-kicker">Përmbledhje Ekzekutive · AI</span>
                    <h2>${currentProjectDetail.title}</h2>
                </div>
                <button class="ghost-button" type="button" data-close>Mbyll</button>
            </div>
            <div id="aiSummaryContent" class="history-card"><div class="muted-row">Duke analizuar projektin...</div></div>
            <div class="task-button-row">
                <button class="ghost-button" type="button" data-copy>Kopjo</button>
                <button class="ghost-button" type="button" data-regenerate>Rigjenero</button>
                <button class="ghost-button" type="button" data-report>Raporto problem me AI</button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
    const close = () => modal.remove();
    modal.querySelector("[data-close]")?.addEventListener("click", close);
    modal.addEventListener("click", (event) => { if (event.target === modal) close(); });

    const load = async (refresh) => {
        const content = modal.querySelector("#aiSummaryContent");
        content.innerHTML = '<div class="muted-row">Duke analizuar projektin...</div>';
        try {
            const result = await postJson(`/api/ai/projects/${encodeURIComponent(currentProjectDetail.projectId)}/summary?userId=${encodeURIComponent(currentUserId)}&forceRefresh=${refresh}`, {});
            content.innerHTML = `
                <div class="history-card-top"><div><strong>✨ AI</strong><p>Bazuar në të dhënat e platformës</p></div><div class="history-side-meta"><small>${result.generatedOn}</small></div></div>
                ${result.warningMessage ? `<div class="feedback-response-box"><strong>Njoftim</strong><p>${result.warningMessage}</p></div>` : ""}
                <div class="task-card-summary">${String(result.summary).replace(/\n/g, "<br>")}</div>
            `;
            modal.querySelector("[data-copy]")?.addEventListener("click", async () => {
                await navigator.clipboard.writeText(result.summary);
                showFeedback("Përmbledhja u kopjua.", false);
            });
        } catch {
            content.innerHTML = '<div class="empty-state">Përmbledhja AI nuk u gjenerua.</div>';
        }
    };
    modal.querySelector("[data-regenerate]")?.addEventListener("click", () => load(true));
    modal.querySelector("[data-report]")?.addEventListener("click", () => showFeedback("Problemi me AI u regjistrua për verifikim.", false));
    await load(forceRefresh);
}

async function runPdfAutofill() {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = "application/pdf,.pdf";
    input.onchange = async () => {
        const file = input.files?.[0];
        if (!file) return;
        const base64 = await readFileAsDataUrl(file);
        showFeedback("Duke analizuar dokumentin me AI...", false);
        const result = await postJson(`/api/ai/pdf-extract?userId=${encodeURIComponent(currentUserId)}`, {
            fileName: file.name,
            pdfBase64: String(base64).split(",")[1] || ""
        });
        document.getElementById("projectTitle").value = result.title || "";
        document.getElementById("projectOwner").value = result.responsiblePerson || document.getElementById("projectOwner").value;
        if (result.startDate) document.getElementById("projectStartDate").value = result.startDate;
        if (result.deadline) document.getElementById("projectDueDate").value = result.deadline;
        if (typeof result.kpiPercent === "number") document.getElementById("projectKpi").value = result.kpiPercent;
        if (result.warningMessage) showFeedback(result.warningMessage, false);
        pdfExtractionHistoryState = await fetchJson(`/api/ai/pdf-history?userId=${encodeURIComponent(currentUserId)}`);
        renderPdfExtractionHistory();
        if (result.workflowSteps?.length) {
            showFeedback(`AI identifikoi ${result.workflowSteps.length} hapa pune. Plotësojini në seksionin e workflow-it nëse dëshironi.`, false);
        }
    };
    input.click();
}

function renderPdfExtractionHistory() {
    const host = document.getElementById("pdfExtractionHistory");
    if (!host) return;
    host.innerHTML = pdfExtractionHistoryState.length
        ? pdfExtractionHistoryState.map((item) => `<article class="history-card"><div class="history-card-top"><div><strong>${item.fileName}</strong><p>${item.usedAi ? "✨ AI" : "Rezervë"}</p></div><div class="history-side-meta"><span>${item.confidence}% besueshmëri</span><small>${item.createdOn}</small></div></div></article>`).join("")
        : "";
}

function renderAiChat() {
    const messages = document.getElementById("aiChatMessages");
    const suggestions = document.getElementById("aiChatSuggestions");
    if (!messages || !suggestions) return;
    const defaults = [
        "Cilat projekte janë në risk?",
        "Sa projekte kanë afat këtë javë?",
        "Gjenero raport për ministrinë time",
        "Cilat detyra kam sot?"
    ];
    suggestions.innerHTML = defaults.map((text) => `<button class="ghost-button" type="button" data-ai-suggestion="${text}">${text}</button>`).join("");
    messages.innerHTML = aiChatHistory.length
        ? aiChatHistory.map((item) => `<div class="ai-chat-message ${item.role}"><strong>${item.role === "user" ? "Ju" : "AI"}</strong><p>${item.content}</p></div>`).join("")
        : '<div class="muted-row">Asistenti AI është gati.</div>';
    messages.scrollTop = messages.scrollHeight;
    suggestions.querySelectorAll("[data-ai-suggestion]").forEach((button) => button.addEventListener("click", () => {
        document.getElementById("aiChatInput").value = button.getAttribute("data-ai-suggestion");
    }));
}

async function sendAiChatMessage() {
    const input = document.getElementById("aiChatInput");
    const value = input?.value?.trim();
    if (!value) return;
    aiChatHistory.push({ role: "user", content: value });
    renderAiChat();
    input.value = "";
    const response = await postJson(`/api/ai/chat?userId=${encodeURIComponent(currentUserId)}`, {
        message: value,
        conversationHistory: aiChatHistory
    });
    aiChatHistory.push({ role: "assistant", content: response.message });
    renderAiChat();
}

function platformaTakimitShqip(vlera) {
    const labels = {
        google_meet: "Google Meet",
        zoom: "Zoom",
        teams: "Microsoft Teams",
        other: "Platforme tjeter"
    };
    return labels[vlera] ?? vlera ?? "Platforme tjeter";
}

function statusiTakimitShqip(vlera) {
    const labels = {
        scheduled: "I planifikuar",
        completed: "I perfunduar",
        cancelled: "I anuluar"
    };
    return labels[vlera] ?? vlera ?? "I planifikuar";
}

function klasaStatusitTakimit(vlera) {
    return vlera === "completed" ? "success" : vlera === "cancelled" ? "critical" : "warning";
}

function formatDateTimeSq(value) {
    return new Intl.DateTimeFormat("sq-AL", {
        weekday: "long",
        day: "2-digit",
        month: "long",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit"
    }).format(new Date(value));
}

function formatDurationSq(minutes) {
    if (!minutes || minutes <= 0) {
        return "Pa percaktim";
    }
    if (minutes % 60 === 0) {
        const hours = minutes / 60;
        return hours === 1 ? "1 ore" : `${hours} ore`;
    }
    return `${minutes} minuta`;
}

function isPastMeeting(meeting) {
    const scheduledTime = new Date(meeting.scheduledAtIso).getTime();
    const durationMs = (meeting.durationMinutes || 0) * 60 * 1000;
    return scheduledTime + durationMs < Date.now();
}

function renderMeetingCard(meeting, role, canManageProjectContent) {
    const attendees = meeting.attendees?.length ? meeting.attendees : ["Pa pjesemarres"];
    return `
        <article class="meeting-card">
            <div class="meeting-card-top">
                <div>
                    <div class="meeting-platform-row">
                        <span class="pill neutral">${platformaTakimitShqip(meeting.platform)}</span>
                        <span class="pill ${klasaStatusitTakimit(meeting.status)}">${statusiTakimitShqip(meeting.status)}</span>
                    </div>
                    <strong>${meeting.title}</strong>
                    <p>${formatDateTimeSq(meeting.scheduledAtIso)}</p>
                </div>
                <div class="meeting-side-meta">
                    <span>${formatDurationSq(meeting.durationMinutes)}</span>
                    <span>${attendees.length} pjesemarres</span>
                </div>
            </div>
            <div class="meeting-meta-grid">
                <div class="meeting-meta-card">
                    <span>Platforma</span>
                    <strong>${platformaTakimitShqip(meeting.platform)}</strong>
                </div>
                <div class="meeting-meta-card">
                    <span>Orari</span>
                    <strong>${meeting.scheduledAt}</strong>
                </div>
                <div class="meeting-meta-card">
                    <span>Pjesemarresit</span>
                    <strong>${attendees.slice(0, 3).join(", ")}${attendees.length > 3 ? ` +${attendees.length - 3}` : ""}</strong>
                </div>
            </div>
            ${meeting.description ? `<p class="meeting-description">${meeting.description}</p>` : ""}
            ${meeting.notes ? `<div class="meeting-note-box"><strong>Shenime pas takimit</strong><p>${meeting.notes}</p>${meeting.recordingUrl ? `<a href="${meeting.recordingUrl}" target="_blank" rel="noopener">Hap regjistrimin</a>` : ""}</div>` : ""}
            <div class="meeting-actions">
                ${meeting.canJoin ? `<button class="ghost-button" type="button" data-meeting-join="${meeting.id}">Hyr ne takim</button>` : ""}
                ${isDirectorLike(role) && meeting.canComplete ? `<button class="ghost-button" type="button" data-meeting-complete="${meeting.id}">Sheno si Perfunduar</button>` : ""}
                ${canManageProjectContent ? `<button class="ghost-button" type="button" data-meeting-edit="${meeting.id}">Ndrysho</button><button class="ghost-button" type="button" data-meeting-delete="${meeting.id}">Fshi</button>` : ""}
            </div>
        </article>
    `;
}

function renderTaskCard(task, canManageProjectContent) {
    const deadlineLabel = task.deadline ?? "Pa afat";
    const tags = task.tags?.length ? task.tags.map((tag) => `<span class="pill neutral">${tag}</span>`).join("") : "";
    return `
        <article class="task-card detail-task-card">
            <div class="task-card-top">
                <div class="task-priority-group">
                    <span class="priority-dot ${task.priority}"></span>
                    <span class="pill neutral">${prioritetiDetyresShqip(task.priority)}</span>
                </div>
                <span class="pill neutral">${statusiDetyresShqip(task.status)}</span>
            </div>
            <strong>${task.title}</strong>
            <p class="task-card-summary">${task.description?.trim() ? task.description : "Pa pershkrim shtese per kete detyre."}</p>
            <div class="task-meta-grid">
                <div class="task-meta-chip">
                    <span>Pergjegjesi</span>
                    <strong>${task.assigneeName ?? "Pa caktim"}</strong>
                </div>
                <div class="task-meta-chip">
                    <span>Afati</span>
                    <strong>${deadlineLabel}</strong>
                </div>
                <div class="task-meta-chip">
                    <span>Ore</span>
                    <strong>${task.estimatedHours}h plan · ${task.actualHours}h reale</strong>
                </div>
            </div>
            ${tags ? `<div class="task-tag-row">${tags}</div>` : ""}
            <div class="task-action-row">
                <span>${task.commentCount} komente</span>
                <span>Renditja: ${task.position + 1}</span>
            </div>
            <div class="task-button-row">
                ${canManageProjectContent ? `<button class="ghost-button" type="button" data-task-edit="${task.id}">Ndrysho</button><button class="ghost-button" type="button" data-task-comment="${task.id}">Koment</button>` : ""}
                ${task.canDelete ? `<button class="ghost-button" type="button" data-task-delete="${task.id}">Fshi</button>` : ""}
            </div>
            ${task.comments.length ? `<div class="task-comments-inline">${task.comments.slice(-2).map((comment) => `<div class="task-comment-item"><strong>${comment.authorName}</strong><span>${comment.content}</span></div>`).join("")}</div>` : ""}
        </article>
    `;
}

function renderPhotoCard(photo) {
    return `
        <article class="photo-card enhanced-photo-card">
            <button class="photo-thumb-button" type="button" data-photo-open="${photo.id}">
                <img src="${photo.thumbnailUrl}" alt="${photo.caption || "Foto projekti"}">
            </button>
            <div class="photo-card-body">
                <div class="photo-card-heading">
                    <strong>${photo.caption || "Foto projekti"}</strong>
                    ${photo.takenOn ? `<span class="pill neutral">${photo.takenOn}</span>` : ""}
                </div>
                <p>${photo.location || "Pa vendndodhje te percaktuar"}</p>
                <small>${photo.uploadedBy} | ${photo.uploadedOn}</small>
                <div class="photo-card-meta">
                    <span class="pill neutral">Galeria e projektit</span>
                    ${photo.canDelete ? `<button class="ghost-button" type="button" data-photo-delete="${photo.id}">Fshi</button>` : ""}
                </div>
            </div>
        </article>
    `;
}

function resolveProjectOkrLinks(projectId) {
    const matches = [];
    (workspaceState?.okrs ?? []).forEach((okr) => {
        okr.keyResults.forEach((keyResult) => {
            keyResult.linkedProjects.forEach((link) => {
                if (link.projectId === projectId) {
                    matches.push({
                        ...link,
                        objectiveTitle: okr.title,
                        objectivePeriod: okr.period,
                        ministryName: okr.ministryName,
                        keyResultTitle: keyResult.title,
                        keyResultProgress: keyResult.progressPercent,
                        keyResultTarget: `${keyResult.currentValue}/${keyResult.targetValue} ${keyResult.unit}`
                    });
                }
            });
        });
    });
    return matches;
}

function openPhotoLightbox(photo) {
    if (!photo) {
        return;
    }

    const modal = document.createElement("div");
    modal.className = "qr-modal-overlay";
    modal.innerHTML = `
        <div class="qr-modal-card photo-lightbox-card">
            <div class="section-heading-row">
                <div>
                    <h3>${photo.caption || "Foto projekti"}</h3>
                    <p>${photo.location || "Pa vendndodhje"}${photo.takenOn ? ` | ${photo.takenOn}` : ""}</p>
                </div>
                <button class="ghost-button" type="button" data-modal-close>Mbyll</button>
            </div>
            <div class="photo-lightbox-content">
                <img src="${photo.fileUrl}" alt="${photo.caption || "Foto projekti"}">
                <div class="photo-lightbox-meta">
                    <div class="meeting-meta-card">
                        <span>Ngarkuar nga</span>
                        <strong>${photo.uploadedBy}</strong>
                    </div>
                    <div class="meeting-meta-card">
                        <span>Data e ngarkimit</span>
                        <strong>${photo.uploadedOn}</strong>
                    </div>
                    <div class="meeting-meta-card">
                        <span>Vendndodhja</span>
                        <strong>${photo.location || "Pa vendndodhje"}</strong>
                    </div>
                </div>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
    modal.querySelector("[data-modal-close]")?.addEventListener("click", () => modal.remove());
    modal.addEventListener("click", (event) => {
        if (event.target === modal) {
            modal.remove();
        }
    });
}

function showFeedback(message, isError = false) {
    const banner = document.getElementById("feedbackBanner");
    banner.textContent = message;
    banner.className = `feedback-banner ${isError ? "error" : "success"}`;
    setTimeout(() => {
        banner.className = "feedback-banner hidden";
    }, 4000);
}

function metricCard(label, value, hint) {
    return `
        <article class="mini-stat-card">
            <span>${label}</span>
            <strong>${value}</strong>
            <small>${hint}</small>
        </article>
    `;
}

function directorPriorityCard(label, value, hint, tone = "neutral") {
    return `
        <article class="director-priority-card ${tone}">
            <span>${label}</span>
            <strong>${value}</strong>
            <small>${hint}</small>
        </article>
    `;
}

function ministryProgressTone(progress) {
    if (progress >= 75) return "success";
    if (progress >= 50) return "primary";
    if (progress >= 35) return "warning";
    return "critical";
}

function ministryProgressColor(progress) {
    if (progress >= 75) return "#16a34a";
    if (progress >= 50) return "#2563eb";
    if (progress >= 35) return "#f59e0b";
    return "#ef4444";
}

function okrProgressTone(progress) {
    if (progress >= 75) return "success";
    if (progress >= 50) return "primary";
    if (progress >= 35) return "warning";
    return "critical";
}

function executiveRailCard(kicker, title, body, tone = "neutral") {
    return `
        <article class="executive-rail-card ${tone}">
            <span class="section-kicker">${kicker}</span>
            <strong>${title}</strong>
            <p>${body}</p>
        </article>
    `;
}

function severityClass(value) {
    return value === "Critical" ? "critical" : value === "Warning" ? "warning" : "info";
}

function toInputDate(value) {
    return value ? value.slice(0, 10) : "";
}

function selectedProject() {
    return workspaceState?.dashboard?.projects?.[0] ?? null;
}

function setSelectOptions(selectId, options, labelBuilder) {
    const element = document.getElementById(selectId);
    element.innerHTML = options.map((item) => `<option value="${item.id}">${labelBuilder(item)}</option>`).join("");
}

function queryProjectId() {
    // Kjo merr projectId nga URL-ja aktive.
    return new URLSearchParams(window.location.search).get("projectId");
}

function queryMinistryId() {
    return new URLSearchParams(window.location.search).get("ministryId");
}

function updateViewUrl(view, extra = {}) {
    // Kjo rifreskon URL-ne sipas tab-it dhe parametrave qe po perdorim.
    const role = workspaceState?.dashboard?.currentUser?.role ?? "Director";
    const url = new URL(window.location.href);
    url.pathname = rrugaPamjes(role, view).split("?")[0];
    url.search = "";
    if (!["calendar", "notifications", "import"].includes(view)) {
        url.searchParams.set("view", view);
    }
    Object.entries(extra).forEach(([key, value]) => {
        if (value) {
            url.searchParams.set(key, value);
        } else {
            url.searchParams.delete(key);
        }
    });
    window.history.pushState({}, "", url);
}

function exportViewLabel(view) {
    const labels = {
        overview: "Permbledhje",
        projects: "Projektet",
        charts: "Grafiket",
        "project-detail": "Detajet e projektit",
        ministries: "Ministrite",
        experts: "Anetaret",
        documents: "Dokumentet",
        workflow: "Rrjedha e punes",
        okrs: "OKR",
        calendar: "Kalendar",
        notifications: "Njoftimet",
        import: "Importo",
        alerts: "Alertet",
        sync: "Cilesimet",
        logs: "Historiku"
    };
    return labels[view] ?? "Paneli";
}

function monthAbbrevSq(date) {
    return ["Jan", "Shk", "Mar", "Pri", "Maj", "Qer", "Kor", "Gus", "Sht", "Tet", "Nen", "Dhj"][date.getMonth()];
}

function formatPercentChange(current, previous) {
    if (!previous && !current) {
        return { text: "0%", direction: "flat" };
    }

    if (!previous) {
        return { text: "+100%", direction: "up" };
    }

    const delta = Math.round(((current - previous) / previous) * 100);
    if (delta > 0) {
        return { text: `+${delta}%`, direction: "up" };
    }
    if (delta < 0) {
        return { text: `${delta}%`, direction: "down" };
    }
    return { text: "0%", direction: "flat" };
}

function getSelectedLanguage() {
    return typeof getLanguage === "function" ? getLanguage() : "sq";
}

function isEnglish() {
    return getSelectedLanguage() === "en";
}

function pickText(shqip, english) {
    return isEnglish() ? english : shqip;
}

function updateChromeControls() {
    const languageButton = document.getElementById("languageToggleButton");
    const themeButton = document.getElementById("themeToggleButton");
    const bellButton = document.getElementById("navbarBellButton");
    const unread = workspaceState?.notifications?.filter((item) => !item.isRead).length ?? 0;
    const role = workspaceState?.dashboard?.currentUser?.role;

    if (languageButton) {
        languageButton.textContent = getSelectedLanguage() === "sq" ? "EN" : "SQ";
        languageButton.title = getSelectedLanguage() === "sq" ? "Switch to English" : "Kalo ne shqip";
    }

    if (themeButton) {
        const dark = typeof getTheme === "function" && getTheme() === "dark";
        themeButton.textContent = dark ? "Drite" : "Erresire";
    }

    if (bellButton) {
        bellButton.style.display = isMinisterLike(role) ? "none" : "";
        bellButton.textContent = unread > 0
            ? `${pickText("Njoftimet", "Notifications")} (${unread > 9 ? "9+" : unread})`
            : pickText("Njoftimet", "Notifications");
    }

    const printMeta = document.getElementById("printMeta");
    if (printMeta && workspaceState?.dashboard?.currentUser) {
        printMeta.textContent = `Printuar: ${new Intl.DateTimeFormat("sq-AL", { dateStyle: "short", timeStyle: "short" }).format(new Date())} nga ${workspaceState.dashboard.currentUser.fullName}`;
    }

    if (typeof applyStaticTranslations === "function") {
        applyStaticTranslations();
    }
}

function renderProjects() {
    // Kjo nderton listen e projekteve sipas filtrave dhe roleve aktive.
    const search = (document.getElementById("projectSearchInput").value || "").trim().toLowerCase();
    const status = document.getElementById("projectStatusFilter").value;
    const projects = workspaceState.dashboard.projects.filter((project) => {
        const matchesSearch = !search ||
            project.title.toLowerCase().includes(search) ||
            project.ownerName.toLowerCase().includes(search) ||
            project.ministryName.toLowerCase().includes(search);
        const matchesStatus = status === "All" || project.status === status;
        return matchesSearch && matchesStatus;
    });

    document.getElementById("projectsList").innerHTML = projects.map((project) => `
        <article class="project-card">
            <div class="project-head">
                <span class="pill neutral">${statusiShqip(project.status)}</span>
                <span class="pill ${project.riskLevel === "High" ? "critical" : project.riskLevel === "Medium" ? "warning" : "success"}">${riskuShqip(project.riskLevel)}</span>
            </div>
            <h3>${project.title}</h3>
            <p>${project.ministryName}</p>
            <div class="card-actions">${riskBadge(project)}</div>
            <div class="project-meta">
                <span>Pergjegjesi: ${project.ownerName}</span>
                <span>Afati: ${formatDate(project.dueDate)}</span>
                <span>KPI: ${project.kpi}%</span>
            </div>
            <div class="progress-track"><span style="width:${project.progress}%"></span></div>
            <div class="card-actions">
                <small>Progresi ${project.progress}%</small>
                <button class="ghost-button" type="button" data-project-open="${project.projectId}">Detaje</button>
                <button class="ghost-button" type="button" data-project-edit="${project.projectId}">Ndrysho</button>
            </div>
        </article>
    `).join("") || '<div class="empty-state">Nuk u gjet asnje projekt per filtrat aktuale.</div>';

    document.querySelectorAll("[data-project-open]").forEach((button) => {
        button.addEventListener("click", async () => {
            updateViewUrl("project-detail", { projectId: button.getAttribute("data-project-open"), ministryId: null });
            await loadProjectDetail(button.getAttribute("data-project-open"));
            aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
            konfiguroNavigimin(workspaceState.dashboard.currentUser.role);
        });
    });

    document.querySelectorAll("[data-project-edit]").forEach((button) => {
        button.addEventListener("click", () => fillProjectForm(button.getAttribute("data-project-edit")));
    });
}

function renderProjectDetailSelector() {
    // Ketu mbushet dropdown-i i projektit ne view e detajeve.
    const selector = document.getElementById("projectDetailSelector");
    if (!selector) {
        return;
    }

    const role = workspaceState?.dashboard?.currentUser?.role;
    if (!isDirectorLike(role) && !isExpertLike(role)) {
        selector.style.display = "none";
        selector.value = "";
        return;
    }

    const projects = workspaceState?.dashboard?.projects ?? [];
    const selectedProjectId = currentProjectDetail?.projectId ?? queryProjectId() ?? "";
    selector.style.display = "";
    selector.innerHTML = `
        <option value="">Zgjidh projektin</option>
        ${projects.map((project) => `<option value="${project.projectId}">${project.title}</option>`).join("")}
    `;
    selector.value = selectedProjectId;

    if (!selector.dataset.bound) {
        selector.dataset.bound = "true";
        selector.addEventListener("change", async (event) => {
            const projectId = event.target.value;
            if (!projectId) {
                currentProjectDetail = null;
                updateViewUrl("project-detail", { ministryId: null });
                renderProjectDetail();
                aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
                return;
            }

            updateViewUrl("project-detail", { projectId, ministryId: null });
            await loadProjectDetail(projectId);
            aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
        });
    }
}

function renderExperts() {
    document.getElementById("expertsList").innerHTML = workspaceState.experts.map((expert) => `
        <article class="history-card entity-card expert-card">
            <div class="history-card-top">
                <div>
                    <strong>${expert.fullName}</strong>
                    <p>${expert.roleTitle}</p>
                </div>
                <div class="history-side-meta">
                    <span class="pill expert">${expert.ministryName}</span>
                    <small class="expert-email">${expert.email}</small>
                </div>
            </div>
            <div class="task-meta-grid expert-meta-grid">
                <div class="task-meta-chip">
                    <span>Ministria</span>
                    <strong>${expert.ministryName}</strong>
                </div>
                <div class="task-meta-chip">
                    <span>Email</span>
                    <strong class="expert-email">${expert.email}</strong>
                </div>
                <div class="task-meta-chip">
                    <span>Kodi demo</span>
                    <strong>${expert.demoAccessCode}</strong>
                </div>
            </div>
            <div class="task-button-row">
                <button class="ghost-button" type="button" data-expert-edit="${expert.id}">Ndrysho</button>
                <button class="ghost-button" type="button" data-expert-delete="${expert.id}">Fshi</button>
            </div>
        </article>
    `).join("") || '<div class="empty-state">Nuk u gjet asnje ekspert.</div>';

    document.querySelectorAll("[data-expert-edit]").forEach((button) => {
        button.addEventListener("click", () => fillExpertForm(button.getAttribute("data-expert-edit")));
    });

    document.querySelectorAll("[data-expert-delete]").forEach((button) => {
        button.addEventListener("click", async () => {
            const result = await deleteJson(`/api/workspace/experts/${button.getAttribute("data-expert-delete")}?userId=${encodeURIComponent(currentUserId)}`);
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
            }
        });
    });
}

function renderDocuments() {
    document.getElementById("documentsList").innerHTML = workspaceState.documents.map((documentItem) => `
        <article class="history-card entity-card document-card">
            <div class="history-card-top">
                <div>
                    <strong>${documentItem.name}</strong>
                    <p>${documentItem.projectTitle}</p>
                </div>
                <div class="history-side-meta">
                    <span class="pill neutral">${documentItem.fileType}</span>
                    <small>${documentItem.uploadedOn}</small>
                </div>
            </div>
            <div class="task-meta-grid document-meta-grid">
                <div class="task-meta-chip">
                    <span>Tipi i dokumentit</span>
                    <strong>${documentItem.fileType}</strong>
                </div>
                <div class="task-meta-chip">
                    <span>Ngarkuar nga</span>
                    <strong>${documentItem.uploadedBy}</strong>
                </div>
                <div class="task-meta-chip">
                    <span>Data</span>
                    <strong>${documentItem.uploadedOn}</strong>
                </div>
            </div>
            <div class="task-button-row">
                <button class="ghost-button" type="button" data-document-download="${documentItem.id}">Shkarko</button>
                <button class="ghost-button" type="button" data-document-delete="${documentItem.id}">Fshi</button>
            </div>
        </article>
    `).join("") || '<div class="empty-state">Nuk ka dokumente te regjistruara.</div>';

    document.querySelectorAll("[data-document-download]").forEach((button) => {
        button.addEventListener("click", () => {
            const documentItem = workspaceState.documents.find((item) => item.id === button.getAttribute("data-document-download"));
            if (!documentItem) {
                return;
            }

            const content = [
                `Dokumenti: ${documentItem.name}`,
                `Projekti: ${documentItem.projectTitle}`,
                `Lloji: ${documentItem.fileType}`,
                `Ngarkuar nga: ${documentItem.uploadedBy}`,
                `Data: ${documentItem.uploadedOn}`
            ].join("\n");

            const blob = new Blob([content], { type: "text/plain;charset=utf-8" });
            const url = URL.createObjectURL(blob);
            const link = document.createElement("a");
            link.href = url;
            link.download = `${documentItem.name.replace(/[^a-z0-9-_]+/gi, "_")}.txt`;
            link.click();
            URL.revokeObjectURL(url);
        });
    });

    document.querySelectorAll("[data-document-delete]").forEach((button) => {
        button.addEventListener("click", async () => {
            const result = await deleteJson(`/api/workspace/documents/${button.getAttribute("data-document-delete")}?userId=${encodeURIComponent(currentUserId)}`);
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace();
            }
        });
    });
}

function renderWorkflow() {
    document.getElementById("workflowList").innerHTML = workspaceState.workflowSteps.map((step) => `
        <article class="history-card entity-card">
            <div class="history-card-top">
                <div>
                    <strong>${step.projectTitle}</strong>
                    <p>Hapi ${step.stepNumber}: ${step.description}</p>
                </div>
                <div class="history-side-meta">
                    <span class="pill neutral">${statusiWorkflowShqip(step.status)}</span>
                    <small>${step.dueDate}</small>
                </div>
            </div>
            <div class="task-meta-grid">
                <div class="task-meta-chip">
                    <span>Progresi</span>
                    <strong>${step.progress}%</strong>
                </div>
                <div class="task-meta-chip">
                    <span>Afati</span>
                    <strong>${step.dueDate}</strong>
                </div>
                <div class="task-meta-chip">
                    <span>Pergjegjesi</span>
                    <strong>${step.ownerName}</strong>
                </div>
            </div>
            <div class="task-button-row">
                <button class="ghost-button" type="button" data-workflow-edit="${step.id}">Ndrysho</button>
            </div>
        </article>
    `).join("") || '<div class="empty-state">Nuk u gjet asnje hap i rrjedhes se punes.</div>';

    document.querySelectorAll("[data-workflow-edit]").forEach((button) => {
        button.addEventListener("click", () => fillWorkflowForm(button.getAttribute("data-workflow-edit")));
    });
}

function renderNotes() {
    document.getElementById("notesList").innerHTML = workspaceState.notes.map((note) => `
        <article class="list-row">
            <div>
                <strong>${note.projectTitle}${note.isPrivate ? " | Privat" : ""}</strong>
                <p>${note.content}</p>
            </div>
            <div class="row-side">
                <span>${note.authorName}</span>
                <span>${note.createdOn}</span>
                ${note.canDelete ? `<button class="ghost-button" type="button" data-note-delete="${note.id}">Fshi</button>` : ""}
            </div>
        </article>
    `).join("") || '<div class="empty-state">Nuk ka ende shenime.</div>';

    document.querySelectorAll("[data-note-delete]").forEach((button) => {
        button.addEventListener("click", async () => {
            const result = await deleteJson(`/api/workspace/notes/${button.getAttribute("data-note-delete")}?userId=${encodeURIComponent(currentUserId)}`);
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                if (currentProjectDetail?.projectId) {
                    await loadProjectDetail(currentProjectDetail.projectId);
                }
            }
        });
    });
}

function renderTasksSection() {
    const board = document.getElementById("tasksBoard");
    const summaryBar = document.getElementById("tasksSummaryBar");
    const projectFilter = document.getElementById("tasksProjectFilter");
    const statusFilter = document.getElementById("tasksStatusFilter");
    const priorityFilter = document.getElementById("tasksPriorityFilter");
    if (!board || !summaryBar || !projectFilter || !statusFilter || !priorityFilter) {
        return;
    }

    if (!projectFilter.dataset.ready) {
        projectFilter.innerHTML = `<option value="all">Te gjitha projektet</option>${workspaceState.dashboard.projects.map((project) => `<option value="${project.projectId}">${project.title}</option>`).join("")}`;
        projectFilter.dataset.ready = "true";
    }

    const filtered = workspaceState.tasks.filter((task) => {
        const byProject = projectFilter.value === "all" || task.projectId === projectFilter.value;
        const byStatus = statusFilter.value === "all" || task.status === statusFilter.value;
        const byPriority = priorityFilter.value === "all" || task.priority === priorityFilter.value;
        return byProject && byStatus && byPriority;
    });

    const columns = [
        { id: "todo", title: "Per t'u Bere" },
        { id: "in_progress", title: "Ne Progres" },
        { id: "review", title: "Ne Shqyrtim" },
        { id: "done", title: "Perfunduar" }
    ];

    summaryBar.innerHTML = `
        <span class="pill neutral">${filtered.length} detyra gjithsej</span>
        <span class="pill success">${filtered.filter((task) => task.status === "done").length} te perfunduara</span>
        <span class="pill warning">${filtered.filter((task) => task.priority === "high" || task.priority === "urgent").length} me prioritet te larte</span>
    `;

    board.innerHTML = columns.map((column) => {
        const tasks = filtered.filter((task) => task.status === column.id);
        return `
            <article class="task-column">
                <div class="task-column-header">
                    <strong>${column.title}</strong>
                    <span class="pill neutral">${tasks.length}</span>
                </div>
                <div class="stack-list">
                    ${tasks.length ? tasks.map((task) => `
                        <button class="task-card" type="button" data-task-open="${task.id}">
                            <div class="task-card-top">
                                <span class="priority-dot ${task.priority}"></span>
                                <span class="pill neutral">${prioritetiDetyresShqip(task.priority)}</span>
                            </div>
                            <strong>${task.title}</strong>
                            <p>${task.projectTitle}</p>
                            <div class="row-side compact">
                                <span>${task.assigneeName ?? "Pa caktim"}</span>
                                <span>${task.deadline ?? "-"}</span>
                            </div>
                        </button>
                    `).join("") : '<div class="empty-state">Nuk ka detyra.</div>'}
                </div>
            </article>
        `;
    }).join("");

    board.querySelectorAll("[data-task-open]").forEach((button) => {
        button.addEventListener("click", async () => {
            const task = workspaceState.tasks.find((item) => item.id === button.getAttribute("data-task-open"));
            if (!task) {
                return;
            }
            updateViewUrl("project-detail", { projectId: task.projectId });
            await loadProjectDetail(task.projectId);
            aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
            konfiguroNavigimin(workspaceState.dashboard.currentUser.role);
            activateDetailTab("tasks");
        });
    });
}

function renderOkrsSection() {
    const list = document.getElementById("okrsList");
    const form = document.getElementById("okrForm");
    const summaryBar = document.getElementById("okrsSummaryBar");
    const layout = document.getElementById("okrsLayout");
    if (!list || !form || !summaryBar || !layout) {
        return;
    }

    const role = workspaceState.dashboard.currentUser.role;
    const visibleOkrs = (workspaceState.okrs ?? []).filter((okr) => !isMinistryScopedRole(role) || okr.ministryId === workspaceState.dashboard.currentUser.ministryId);
    const totalKeyResults = visibleOkrs.reduce((sum, okr) => sum + okr.keyResults.length, 0);
    const linkedProjects = visibleOkrs.reduce((sum, okr) => sum + okr.keyResults.reduce((krSum, kr) => krSum + kr.linkedProjects.length, 0), 0);
    const averageProgress = visibleOkrs.length ? Math.round(visibleOkrs.reduce((sum, okr) => sum + okr.progressPercent, 0) / visibleOkrs.length) : 0;

    summaryBar.innerHTML = `
        <span class="pill neutral">${visibleOkrs.length} objektiva</span>
        <span class="pill neutral">${totalKeyResults} Key Results</span>
        <span class="pill success">${averageProgress}% progres mesatar</span>
        <span class="pill warning">${linkedProjects} lidhje me projekte</span>
    `;

    list.innerHTML = visibleOkrs.length
        ? visibleOkrs.map((okr) => `
            <article class="panel-card okr-card">
                <div class="okr-card-head">
                    <div>
                        <span class="section-kicker">${okr.ministryName}</span>
                        <h3>${okr.title}</h3>
                        <p>${okr.description || "Objektiv pa pershkrim shtese."}</p>
                    </div>
                    <div class="okr-side-meta">
                        <span class="pill neutral">${okr.period}</span>
                        <span class="pill success">${okr.progressPercent}%</span>
                        <small>Pergjegjes: ${okr.ownerName}</small>
                        ${isDirectorLike(role) ? `<button class="ghost-button" type="button" data-okr-edit="${okr.id}">Ndrysho</button>` : ""}
                    </div>
                </div>
                <div class="progress-track"><span style="width:${okr.progressPercent}%"></span></div>
                <div class="stack-list okr-key-results-list">
                    ${okr.keyResults.map((keyResult) => `
                        <article class="okr-key-result-card">
                            <div class="okr-key-result-top">
                                <strong>${keyResult.title}</strong>
                                <span>${keyResult.currentValue}/${keyResult.targetValue} ${keyResult.unit}</span>
                            </div>
                            <div class="progress-track compact"><span style="width:${keyResult.progressPercent}%"></span></div>
                            <div class="row-side compact okr-kr-meta">
                                <span>${keyResult.progressPercent}% progres</span>
                                <span>${keyResult.linkedProjects.length} projekte te lidhura</span>
                            </div>
                            ${keyResult.linkedProjects.length
                                ? `<div class="okr-chip-wrap">${keyResult.linkedProjects.map((link) => `<button class="ghost-button okr-chip" type="button" data-okr-project-open="${link.projectId}">${link.projectTitle} | ${link.contributionWeight}%</button>`).join("")}</div>`
                                : '<div class="empty-state small">Pa projekte te lidhura ende.</div>'}
                        </article>
                    `).join("")}
                </div>
            </article>
        `).join("")
        : '<div class="empty-state">Nuk ka ende objektiva OKR per kete pamje.</div>';

    document.querySelectorAll("[data-okr-edit]").forEach((button) => {
        button.addEventListener("click", () => fillOkrForm(button.getAttribute("data-okr-edit")));
    });

    document.querySelectorAll("[data-okr-project-open]").forEach((button) => {
        button.addEventListener("click", async () => {
            const projectId = button.getAttribute("data-okr-project-open");
            updateViewUrl("project-detail", { projectId, ministryId: null });
            await loadProjectDetail(projectId);
            activateDetailTab("okr");
            aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
            konfiguroNavigimin(workspaceState.dashboard.currentUser.role);
        });
    });

    layout.classList.toggle("single-focus", !isDirectorLike(role));
    form.classList.toggle("disabled-panel", !isDirectorLike(role));
    form.style.display = isDirectorLike(role) ? "" : "none";
}

function activateDetailTab(tab) {
    document.querySelectorAll("[data-detail-tab]").forEach((item) => item.classList.toggle("active", item.getAttribute("data-detail-tab") === tab));
    const panelMap = {
        workflow: "detailTabWorkflow",
        documents: "detailTabDocuments",
        comments: "detailTabComments",
        meetings: "detailTabMeetings",
        tasks: "detailTabTasks",
        milestones: "detailTabMilestones",
        gallery: "detailTabGallery",
        "risk-ai": "detailTabRiskAi",
        okr: "detailTabOkr",
        history: "detailTabHistory"
    };
    Object.entries(panelMap).forEach(([id, elementId]) => {
        document.getElementById(elementId)?.classList.toggle("hidden", id !== tab);
    });
}

function getPortfolioStatusCounts(projects = workspaceState?.dashboard?.projects ?? []) {
    const counts = { Active: 0, InProcess: 0, Completed: 0, Cancelled: 0 };
    projects.forEach((project) => {
        counts[project.status] = (counts[project.status] ?? 0) + 1;
    });
    return counts;
}

function chartPalette(status) {
    if (status === "Active") return "#0b4f9c";
    if (status === "InProcess") return "#f59e0b";
    if (status === "Completed") return "#16a34a";
    if (status === "Cancelled") return "#d7263d";
    return "#64748b";
}

function severityColor(level) {
    if (level === "Critical") return "#d7263d";
    if (level === "Warning") return "#f59e0b";
    return "#0b4f9c";
}

function buildTimelineSeries(points, projects) {
    const today = new Date();
    const currentMonthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    return points.map((point, index) => {
        const monthStart = new Date(currentMonthStart.getFullYear(), currentMonthStart.getMonth() - (points.length - 1 - index), 1);
        const monthEnd = new Date(monthStart.getFullYear(), monthStart.getMonth() + 1, 0, 23, 59, 59, 999);
        const inProcessProjects = projects.filter((project) => {
            if (project.status !== "InProcess") {
                return false;
            }
            const startDate = new Date(project.startDate);
            const dueDate = new Date(project.dueDate);
            return startDate <= monthEnd && dueDate >= monthStart;
        }).length;
        const cancelledProjects = projects.filter((project) => {
            if (project.status !== "Cancelled") {
                return false;
            }
            const dueDate = new Date(project.dueDate);
            return dueDate >= monthStart && dueDate <= monthEnd;
        }).length;

        return {
            ...point,
            inProcessProjects,
            cancelledProjects
        };
    });
}

function buildLineChartSvg(points) {
    const width = 640;
    const height = 220;
    const paddingX = 36;
    const paddingY = 26;
    const series = [
        { key: "openedProjects", className: "opened", color: "#0b4f9c" },
        { key: "closedProjects", className: "closed", color: "#16a34a" },
        { key: "inProcessProjects", className: "in-process", color: "#f59e0b" },
        { key: "cancelledProjects", className: "cancelled", color: "#d7263d" }
    ];
    const maxValue = Math.max(...points.flatMap((point) => series.map((item) => point[item.key] ?? 0)), 1);
    const xStep = points.length > 1 ? (width - paddingX * 2) / (points.length - 1) : 0;
    const yFor = (value) => height - paddingY - (value / maxValue) * (height - paddingY * 2);
    const polyline = (key) => points.map((point, index) => `${paddingX + index * xStep},${yFor(point[key])}`).join(" ");
    const dots = (key, color) => points.map((point, index) => `
        <circle cx="${paddingX + index * xStep}" cy="${yFor(point[key])}" r="4.5" fill="${color}"></circle>
    `).join("");
    const labels = points.map((point, index) => `
        <text x="${paddingX + index * xStep}" y="${height - 4}" text-anchor="middle">${point.monthLabel}</text>
    `).join("");
    const guides = Array.from({ length: 4 }, (_, index) => {
        const value = Math.round(maxValue * (1 - index / 3));
        const y = yFor(value);
        return `
            <line x1="${paddingX}" y1="${y}" x2="${width - paddingX}" y2="${y}"></line>
            <text x="10" y="${y + 4}">${value}</text>
        `;
    }).join("");

    return `
        <svg viewBox="0 0 ${width} ${height}" class="line-chart-svg" role="img" aria-label="Grafik linear i progresit ne kohe">
            <g class="line-chart-guides">${guides}</g>
            ${series.map((item) => `<polyline class="line-chart-line ${item.className}" points="${polyline(item.key)}"></polyline>`).join("")}
            ${series.map((item) => dots(item.key, item.color)).join("")}
            <g class="line-chart-labels">${labels}</g>
        </svg>
    `;
}

function renderTimeline() {
    const host = document.getElementById("timelineChart");
    if (!host) {
        return;
    }
    const points = buildTimelineSeries(workspaceState.dashboard.timeline, workspaceState.dashboard.projects);
    const totalOpened = points.reduce((sum, point) => sum + point.openedProjects, 0);
    const totalClosed = points.reduce((sum, point) => sum + point.closedProjects, 0);
    const totalInProcess = points.reduce((sum, point) => sum + point.inProcessProjects, 0);
    const totalCancelled = points.reduce((sum, point) => sum + point.cancelledProjects, 0);
    host.innerHTML = `
        <div class="chart-header-strip">
            <span class="pill neutral">Projekte te hapura: ${totalOpened}</span>
            <span class="pill neutral">Projekte te mbyllura: ${totalClosed}</span>
            <span class="pill neutral">Ne proces: ${totalInProcess}</span>
            <span class="pill neutral">Anulluar: ${totalCancelled}</span>
            <span class="pill success">6 muaj trend</span>
        </div>
        ${buildLineChartSvg(points)}
        <div class="line-chart-legend">
            <span><i class="legend-dot line-opened"></i> Projekte të hapura</span>
            <span><i class="legend-dot line-closed"></i> Projekte të mbyllura</span>
            <span><i class="legend-dot line-in-process"></i> Ne proces</span>
            <span><i class="legend-dot line-cancelled"></i> Anulluar</span>
        </div>
    `;
}

function renderMacroCharts() {
    const projects = workspaceState.dashboard.projects;
    const counts = getPortfolioStatusCounts(projects);

    const donutHost = document.getElementById("portfolioDonutChart");
    const donutLegend = document.getElementById("portfolioDonutLegend");
    const total = Math.max(projects.length, 1);
    let cursor = 0;
    const segments = Object.entries(counts).map(([label, value]) => {
        const portion = (value / total) * 100;
        const segment = `${chartPalette(label)} ${cursor}% ${cursor + portion}%`;
        cursor += portion;
        return segment;
    }).join(", ");
    if (donutHost) {
        donutHost.innerHTML = `
            <div class="donut-ring" style="background:conic-gradient(${segments || "#cbd5e1 0% 100%"})">
                <div class="donut-core">
                    <strong>${projects.length}</strong>
                    <span>projekte</span>
                </div>
            </div>
        `;
    }
    if (donutLegend) {
        donutLegend.innerHTML = Object.entries(counts).map(([label, value]) => `
            <span><i class="legend-dot" style="background:${chartPalette(label)};"></i>${statusiShqip(label)} · ${value}</span>
        `).join("");
    }

    const alertsHost = document.getElementById("alertsRiskChart");
    if (alertsHost) {
        const alerts = workspaceState.dashboard.alerts;
        const severityCounts = {
            Critical: alerts.filter((item) => item.severity === "Critical").length,
            Warning: alerts.filter((item) => item.severity === "Warning").length,
            Info: alerts.filter((item) => item.severity === "Info").length
        };
        const outOfRisk = Math.max(0, workspaceState.dashboard.overview.totalProjects - workspaceState.dashboard.overview.riskProjects);
        const maxAlertValue = Math.max(workspaceState.dashboard.overview.riskProjects, ...Object.values(severityCounts), 1);
        const riskMetrics = [
            { label: "Jashtë riskut", value: outOfRisk, color: "#16a34a" },
            { label: "Projekte në risk", value: workspaceState.dashboard.overview.riskProjects, color: "#d7263d" },
            { label: "Alert kritik", value: severityCounts.Critical, color: severityColor("Critical") },
            { label: "Alert kujdes", value: severityCounts.Warning, color: severityColor("Warning") },
            { label: "Alert info", value: severityCounts.Info, color: severityColor("Info") }
        ].filter((item) => item.value > 0);
        alertsHost.innerHTML = riskMetrics.map((item) => `
            <article class="alerts-risk-row">
                <div>
                    <strong>${item.label}</strong>
                    <small>${item.value} raste</small>
                </div>
                <div class="alerts-risk-track">
                    <span style="width:${(item.value / maxAlertValue) * 100}%;background:${item.color};"></span>
                </div>
            </article>
        `).join("");
    }

    document.getElementById("ministryChart").innerHTML = [...workspaceState.dashboard.ministryBoard]
        .sort((left, right) => right.averageKpi - left.averageKpi)
        .slice(0, 6)
        .map((item) => `
            <article class="metric-row">
                <div>
                    <strong>${item.ministryName}</strong>
                    <p>${item.totalProjects} projekte | ${item.experts} eksperte</p>
                </div>
                <div class="metric-side">
                    <span class="pill ${klasaGjendjes(item.healthStatus)}">${gjendjaShqip(item.healthStatus)}</span>
                    <strong>${item.averageKpi}%</strong>
                </div>
            </article>
        `).join("");

    const stackedHost = document.getElementById("stackedStatusChart");
    if (stackedHost) {
        stackedHost.innerHTML = [...workspaceState.dashboard.ministryBoard]
            .sort((left, right) => right.totalProjects - left.totalProjects)
            .slice(0, 6)
            .map((item) => {
                const totalProjects = Math.max(item.totalProjects, 1);
                const segmentsByMinistry = [
                    { value: item.activeProjects, color: chartPalette("Active"), label: "Aktiv" },
                    { value: item.inProcessProjects, color: chartPalette("InProcess"), label: "Në proces" },
                    { value: item.completedProjects, color: chartPalette("Completed"), label: "Përfunduar" },
                    { value: item.cancelledProjects, color: chartPalette("Cancelled"), label: "Anuluar" }
                ];
                return `
                    <article class="stacked-status-row">
                        <div class="stacked-status-copy">
                            <strong>${item.ministryName}</strong>
                            <small>KPI ${item.averageKpi}% · ${item.totalProjects} projekte</small>
                        </div>
                        <div class="stacked-status-track">
                            ${segmentsByMinistry.map((segment) => `<span title="${segment.label}: ${segment.value}" style="width:${(segment.value / totalProjects) * 100}%;background:${segment.color};"></span>`).join("")}
                        </div>
                    </article>
                `;
            }).join("");
    }
}

function renderMinistryProgressView() {
    const section = document.getElementById("ministryProgressSection");
    const host = document.getElementById("ministryProgressList");
    if (!section || !host || !workspaceState) {
        return;
    }

    const role = workspaceState.dashboard.currentUser.role;
    if (!isMinisterLike(role) && !isDirectorLike(role)) {
        section.style.display = "none";
        host.innerHTML = "";
        return;
    }

    const ministryRows = (workspaceState.dashboard.ministryBoard ?? [])
        .map((ministry) => {
            const ministryProjects = (workspaceState.dashboard.projects ?? []).filter((project) => project.ministryName === ministry.ministryName);
            const averageProgress = ministryProjects.length
                ? Math.round(ministryProjects.reduce((sum, project) => sum + Number(project.progress || 0), 0) / ministryProjects.length)
                : 0;
            return {
                ministryName: ministry.ministryName,
                acronym: ministry.acronym,
                averageProgress,
                totalProjects: ministryProjects.length
            };
        })
        .sort((left, right) => right.averageProgress - left.averageProgress);

    host.innerHTML = ministryRows.length
        ? ministryRows.map((item) => {
            const tone = ministryProgressTone(item.averageProgress);
            const color = ministryProgressColor(item.averageProgress);
            return `
                <article class="ministry-progress-row ${tone}">
                    <div class="ministry-progress-copy">
                        <div class="ministry-progress-heading">
                            <strong>${item.ministryName}</strong>
                            <span class="pill neutral">${item.acronym}</span>
                        </div>
                        <small>${item.totalProjects} projekte ne portofol</small>
                    </div>
                    <div class="ministry-progress-bar-wrap">
                        <div class="ministry-progress-track">
                            <span style="width:${item.averageProgress}%;background:${color};"></span>
                        </div>
                    </div>
                    <div class="ministry-progress-value">${item.averageProgress}%</div>
                </article>
            `;
        }).join("")
        : '<div class="empty-state">Nuk ka të dhëna progresi për ministritë e dukshme.</div>';

    section.style.display = "";
}

function buildOverviewMinistryProgressMarkup(compact = false) {
    const ministryRows = (workspaceState.dashboard.ministryBoard ?? [])
        .map((ministry) => {
            const ministryProjects = (workspaceState.dashboard.projects ?? []).filter((project) => project.ministryName === ministry.ministryName);
            const averageProgress = ministryProjects.length
                ? Math.round(ministryProjects.reduce((sum, project) => sum + Number(project.progress || 0), 0) / ministryProjects.length)
                : 0;
            return {
                ministryName: ministry.ministryName,
                averageProgress
            };
        })
        .sort((left, right) => right.averageProgress - left.averageProgress)
        .slice(0, 6);

    return ministryRows.length
        ? ministryRows.map((item) => {
            const tone = ministryProgressTone(item.averageProgress);
            const color = ministryProgressColor(item.averageProgress);
            const compactClass = compact ? " compact" : "";
            return `
                <article class="ministry-progress-row${compactClass} ${tone}">
                    <div class="ministry-progress-head${compactClass}">
                        <strong>${item.ministryName}</strong>
                        <span>${item.averageProgress}%</span>
                    </div>
                    <div class="ministry-progress-track${compactClass}">
                        <span style="width:${item.averageProgress}%;background:${color};"></span>
                    </div>
                </article>
            `;
        }).join("")
        : '<div class="empty-state">Nuk ka të dhëna progresi për ministritë e dukshme.</div>';
}

function renderOverviewMinistryProgress() {
    const compactSection = document.getElementById("overviewMinistryProgressSection");
    const compactHost = document.getElementById("overviewMinistryProgressList");
    const overviewSection = document.getElementById("ministerOverviewMinistryProgressSection");
    const overviewHost = document.getElementById("ministerOverviewMinistryProgressList");
    if (!workspaceState) {
        return;
    }

    const role = workspaceState.dashboard.currentUser.role;
    if (!isMinisterLike(role) && !isDirectorLike(role)) {
        if (compactSection) { compactSection.style.display = "none"; }
        if (compactHost) { compactHost.innerHTML = ""; }
        if (overviewSection) { overviewSection.style.display = "none"; }
        if (overviewHost) { overviewHost.innerHTML = ""; }
        return;
    }

    if (compactHost) {
        compactHost.innerHTML = buildOverviewMinistryProgressMarkup(true);
    }
    if (overviewHost) {
        overviewHost.innerHTML = buildOverviewMinistryProgressMarkup(false);
    }

    if (isMinisterLike(role)) {
        if (compactSection) { compactSection.style.display = "none"; }
        if (overviewSection) { overviewSection.style.display = ""; }
    } else {
        if (compactSection) { compactSection.style.display = ""; }
        if (overviewSection) { overviewSection.style.display = "none"; }
    }
}

function renderOkrObjectivesOverview() {
    const section = document.getElementById("okrObjectivesOverviewSection");
    const host = document.getElementById("okrObjectivesOverviewList");
    if (!section || !host || !workspaceState) {
        return;
    }

    const role = workspaceState.dashboard.currentUser.role;
    if (!isMinisterLike(role) && !isDirectorLike(role)) {
        section.style.display = "none";
        host.innerHTML = "";
        return;
    }

    const overview = workspaceState.dashboard.overview ?? {};
    const aiUsageProgress = Math.max(28, Math.min(100, Math.round(((workspaceState.smartAlerts?.length ?? 0) * 18) + 34)));
    const objectivesToRender = [
        {
            title: "Digjitalizimi i proceseve",
            progressPercent: Math.max(35, Number(overview.averageProgress ?? 0)),
            detail: "Objektivi fokusohet në standardizimin dhe dixhitalizimin e rrjedhave kryesore të punës në platformë."
        },
        {
            title: "Rritja e perdorimit te AI",
            progressPercent: aiUsageProgress,
            detail: "Objektivi mat shtrirjen e analizës së riskut, smart alerts dhe përdorimit të asistentit AI në vendimmarrje."
        },
        {
            title: "Efikasiteti i raportimit",
            progressPercent: Math.max(30, Math.round(((Number(overview.completedProjects ?? 0) * 6) + (Number(overview.averageKpi ?? 0) * 0.45)))),
            detail: "Objektivi synon përshpejtimin e raportimit, konsolidimin e të dhënave dhe lehtësimin e leximit ekzekutiv."
        }
    ];

    host.innerHTML = objectivesToRender.map((objective) => {
            const tone = okrProgressTone(objective.progressPercent);
            return `
                <article class="okr-objective-row ${tone}">
                    <div class="okr-objective-copy">
                        <p><strong>${objective.title}</strong> ka arritur <strong>${objective.progressPercent}%</strong> progres.</p>
                    </div>
                    <div class="okr-objective-progress">
                        <div class="progress-track okr-overview-progress ${tone}">
                            <span style="width:${objective.progressPercent}%"></span>
                        </div>
                    </div>
                </article>
            `;
        }).join("");

    section.style.display = "";
}

function renderCapabilities() {
    const capabilities = workspaceState.dashboard.capabilities;
    const roleActions = document.getElementById("roleActions");
    const currentRole = workspaceState.dashboard.currentUser.role;
    if (roleActions) {
        roleActions.style.display = "none";
    }

    if (isMinisterLike(currentRole) || isExpertLike(currentRole) || isDirectorLike(currentRole)) {
        if (roleActions) {
            roleActions.style.display = "none";
        }
        return;
    }

    const labels = [
        ["Ndrysho projektet", capabilities.canEditProjects],
        ["Menaxho ekspertet", capabilities.canManageExperts],
        ["Konfiguro alertet", capabilities.canConfigureAlerts],
        ["Ngarko dokumente", capabilities.canUploadDocuments],
        ["Shiko historikun", capabilities.canViewAuditLogs],
        ["Eksporto raporte", capabilities.canExportReports]
    ];

    document.getElementById("capabilitiesGrid").innerHTML = labels.map(([label, enabled]) => `
        <div class="capability-card ${enabled ? "enabled" : "disabled"}">
            <strong>${label}</strong>
            <span>${enabled ? "Aktive" : "Vetem lexim / e fshehur"}</span>
        </div>
    `).join("");
}

function renderSync() {
    const sync = workspaceState.syncStatus;
    document.getElementById("syncCard").innerHTML = `
        <div class="sync-row"><strong>Burimi</strong><span>${sync.source}</span></div>
        <div class="sync-row"><strong>Menyra</strong><span>${sync.mode}</span></div>
        <div class="sync-row"><strong>Gjendja</strong><span class="pill ${sync.health === "Ne rregull" ? "success" : "warning"}">${sync.health}</span></div>
        <div class="sync-row"><strong>Sink. e fundit</strong><span>${sync.lastSync}</span></div>
        <div class="sync-row"><strong>Sink. e ardhshme</strong><span>${sync.nextSync}</span></div>
    `;
}

function renderAlertSettings() {
    const settings = workspaceState.alertSettings;
    document.getElementById("criticalKpiThreshold").value = settings.criticalKpiThreshold;
    document.getElementById("warningKpiThreshold").value = settings.warningKpiThreshold;
    document.getElementById("warningDaysBeforeDeadline").value = settings.warningDaysBeforeDeadline;
    document.getElementById("alertEmailRecipients").value = settings.emailRecipients;
}

function renderGlobalAlertBanner() {
    const banner = document.getElementById("globalAlertBanner");
    if (isMinisterLike(workspaceState?.dashboard?.currentUser?.role)) {
        banner.className = "global-alert-banner hidden";
        banner.innerHTML = "";
        return;
    }
    const alerts = workspaceState.dashboard.alerts.filter((item) => item.severity === "Critical" || item.severity === "Warning");
    if (!alerts.length) {
        banner.className = "global-alert-banner hidden";
        banner.innerHTML = "";
        return;
    }

    banner.className = "global-alert-banner";
    banner.innerHTML = `
        <strong>Projektet ne risk:</strong>
        <div class="global-alert-list">
            ${alerts.slice(0, 3).map((item) => `<div class="global-alert-item">${item.projectTitle} (${item.ministryName})</div>`).join("")}
        </div>
    `;
}

function renderCalendar() {
    const ministryFilter = document.getElementById("calendarMinistryFilter");
    const statusFilter = document.getElementById("calendarStatusFilter").value;
    const kpiFilter = Number(document.getElementById("calendarKpiFilter").value || 0);
    const selectedMinistry = ministryFilter.value || "all";

    if (!ministryFilter.dataset.ready) {
        ministryFilter.innerHTML = `<option value="all">Te gjitha ministrite</option>${workspaceState.ministries.map((item) => `<option value="${item.id}">${item.name}</option>`).join("")}`;
        if (isExpertLike(workspaceState.dashboard.currentUser.role)) {
            ministryFilter.value = workspaceState.dashboard.currentUser.ministryId;
            ministryFilter.disabled = true;
        }
        ministryFilter.dataset.ready = "true";
    }

    const events = workspaceState.calendarEvents.filter((eventItem) => {
        const byMinistry = selectedMinistry === "all" || eventItem.ministryId === selectedMinistry;
        const byKpi = eventItem.kpi >= kpiFilter;
        const byStatus = statusFilter === "all"
            || (statusFilter === "active" && eventItem.status.toLowerCase().includes("active"))
            || (statusFilter === "in_progress" && eventItem.status.toLowerCase().includes("process"))
            || (statusFilter === "at_risk" && (eventItem.color === "#EF4444" || eventItem.color === "#F97316"));
        return byMinistry && byKpi && byStatus;
    });

    const year = 2026;
    const picker = document.getElementById("calendarMonthPicker");
    const title = document.getElementById("calendarMonthTitle");
    const summary = document.getElementById("calendarMonthSummary");
    if (selectedCalendarMonth < 0 || selectedCalendarMonth > 11) {
        selectedCalendarMonth = 0;
    }

    const monthCounts = Array.from({ length: 12 }, (_, monthIndex) =>
        events.filter((item) => {
            const due = new Date(item.dueDate);
            return due.getFullYear() === year && due.getMonth() === monthIndex;
        }).length
    );

    picker.innerHTML = muajt2026.map((monthName, monthIndex) => `
        <button class="calendar-month-card ${selectedCalendarMonth === monthIndex ? "active" : ""}" type="button" data-calendar-month="${monthIndex}">
            <strong>${monthName}</strong>
            <span>${monthCounts[monthIndex]} ngjarje</span>
        </button>
    `).join("");

    picker.querySelectorAll("[data-calendar-month]").forEach((button) => {
        button.addEventListener("click", () => {
            selectedCalendarMonth = Number(button.getAttribute("data-calendar-month") || 0);
            renderCalendar();
        });
    });

    const start = new Date(year, selectedCalendarMonth, 1);
    const end = new Date(year, selectedCalendarMonth + 1, 0);
    const totalDays = end.getDate();
    const monthEvents = events.filter((item) => {
        const due = new Date(item.dueDate);
        return due.getFullYear() === year && due.getMonth() === selectedCalendarMonth;
    });
    const groupedByDay = Array.from({ length: totalDays }, (_, index) => {
        const day = new Date(year, selectedCalendarMonth, index + 1);
        const iso = day.toISOString().slice(0, 10);
        return {
            dayNumber: index + 1,
            dateLabel: formatDate(iso),
            events: monthEvents
                .filter((item) => item.dueDate === iso)
                .sort((a, b) => Number(a.isWorkflow) - Number(b.isWorkflow) || a.title.localeCompare(b.title, "sq"))
        };
    }).filter((item) => item.events.length > 0);

    title.textContent = `Projektet per ${muajt2026[selectedCalendarMonth]} 2026`;
    summary.textContent = `${monthEvents.length} projekte / hapa`;

    const grid = document.getElementById("calendarGrid");
    grid.innerHTML = groupedByDay.length
        ? groupedByDay.map((dayGroup) => `
            <article class="calendar-day-row">
                <div class="calendar-day-label">
                    <strong>${dayGroup.dayNumber}</strong>
                    <span>${dayGroup.dateLabel}</span>
                </div>
                <div class="calendar-day-events">
                    ${dayGroup.events.map((item) => `<button class="calendar-event ${item.isWorkflow ? "workflow" : ""}" style="--event-color:${item.color}" type="button" data-calendar-event="${item.id}">
                        <span>${item.title}</span>
                        <small>${item.ministryAcronym} | ${item.isWorkflow ? "Workflow" : statusiShqip(item.status)}</small>
                    </button>`).join("")}
                </div>
            </article>
        `).join("")
        : '<div class="empty-state">Nuk ka projekte ose hapa workflow ne kete muaj per filtrat e zgjedhur.</div>';

    document.querySelectorAll("[data-calendar-event]").forEach((button) => {
        button.addEventListener("click", () => {
            const eventItem = monthEvents.find((item) => item.id === button.getAttribute("data-calendar-event"));
            if (!eventItem) {
                return;
            }
            const due = new Date(eventItem.dueDate);
            const daysRemaining = Math.ceil((due.getTime() - Date.now()) / (1000 * 60 * 60 * 24));
            document.getElementById("calendarDrawer").innerHTML = `
                <article class="detail-metric-card">
                    <strong>${eventItem.title}</strong>
                    <p>${eventItem.ministryName}</p>
                    <p><span class="pill neutral">${statusiShqip(eventItem.status)}</span></p>
                    <p>KPI: ${eventItem.kpi}%</p>
                    <p>${daysRemaining >= 0 ? `${daysRemaining} dite te mbetura` : `Vonuese me ${Math.abs(daysRemaining)} dite`}</p>
                    <p>Pergjegjesi: ${eventItem.ownerName}</p>
                    <button class="secondary-button" type="button" data-open-project="${eventItem.projectId}">Shiko Projektin -></button>
                </article>
            `;
            document.querySelector("[data-open-project]")?.addEventListener("click", async () => {
                updateViewUrl("project-detail", { projectId: eventItem.projectId });
                await loadProjectDetail(eventItem.projectId);
                aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
                konfiguroNavigimin(workspaceState.dashboard.currentUser.role);
            });
        });
    });
}

function renderNotifications() {
    const bell = document.getElementById("notificationsBell");
    const list = document.getElementById("notificationsList");
    const unreadCount = workspaceState.notifications.filter((item) => !item.isRead).length;
    bell.innerHTML = `<button class="ghost-button" type="button">Zile ${unreadCount > 0 ? `<span class="pill critical">${unreadCount > 9 ? "9+" : unreadCount}</span>` : ""}</button>`;

    list.innerHTML = workspaceState.notifications.length
        ? workspaceState.notifications.map((item) => `
            <article class="history-card notification-card ${item.isRead ? "is-read" : ""}">
                <div class="history-card-top">
                    <div>
                        <strong>${item.title}</strong>
                        <p>${item.message}</p>
                    </div>
                    <div class="history-side-meta">
                        <span class="pill ${item.isRead ? "neutral" : "warning"}">${item.isRead ? "Lexuar" : "E re"}</span>
                        <small>${item.relativeTime}</small>
                    </div>
                </div>
                <div class="task-button-row">
                    <button class="ghost-button" type="button" data-notification-open="${item.id}">Hap</button>
                </div>
            </article>
        `).join("")
        : '<div class="empty-state">Nuk ka njoftime.</div>';

    document.querySelectorAll("[data-notification-open]").forEach((button) => {
        button.addEventListener("click", async () => {
            await postJson(`/api/workspace/notifications/read?userId=${encodeURIComponent(currentUserId)}`, { notificationId: button.getAttribute("data-notification-open") });
            await loadWorkspace(false);
        });
    });
}

function renderImportSection() {
    const logs = document.getElementById("importLogsList");
    const confirmButton = document.getElementById("confirmImportButton");
    if (confirmButton) {
        confirmButton.disabled = !importPreviewState || importPreviewState.validRows === 0;
    }
    logs.innerHTML = workspaceState.importLogs.length
        ? workspaceState.importLogs.map((item) => `<article class="list-row"><div><strong>${item.fileName}</strong><p>${item.successfulRows}/${item.totalRows} rreshta</p></div><div class="row-side"><span>${item.successRate}</span><span>${item.createdOn}</span></div></article>`).join("")
        : '<div class="empty-state">Nuk ka histori importi.</div>';
}

function renderMinistriesSection() {
    // Ketu shfaqet lista e ministrive dhe detaji i ministrise se zgjedhur.
    const cards = document.getElementById("ministriesCards");
    const selectedMinistryId = queryMinistryId() ?? workspaceState.ministryDetails[0]?.ministryId;
    cards.innerHTML = workspaceState.ministryDetails.map((ministry) => `
        <article class="project-card ${ministry.ministryId === selectedMinistryId ? "selected-card" : ""}">
            <div class="project-head">
                <span class="pill neutral">${ministry.acronym}</span>
                <span class="pill success">${ministry.averageKpi}% KPI</span>
            </div>
            <h3>${ministry.ministryName}</h3>
            <p>${ministry.projectsCount} projekte | ${ministry.expertsCount} eksperte</p>
            <div class="card-actions">
                <small>Drejtues: ${ministry.directorName}</small>
                <button class="ghost-button" type="button" data-ministry-open="${ministry.ministryId}">Hap</button>
            </div>
        </article>
    `).join("");

    document.querySelectorAll("[data-ministry-open]").forEach((button) => {
        button.addEventListener("click", () => {
            updateViewUrl("ministries", { ministryId: button.getAttribute("data-ministry-open"), projectId: null });
            renderMinistriesSection();
            aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
        });
    });

    const detail = workspaceState.ministryDetails.find((item) => item.ministryId === selectedMinistryId) ?? workspaceState.ministryDetails[0];
    const title = document.getElementById("ministryDetailTitle");
    const content = document.getElementById("ministryDetailContent");
    if (!detail) {
        title.textContent = "Zgjidh nje ministri";
        content.innerHTML = '<div class="empty-state">Nuk ka te dhena per ministrine.</div>';
        return;
    }

    title.textContent = detail.ministryName;
    content.innerHTML = `
        <div class="meetings-overview-grid ministry-summary-grid">
            ${metricCard("Projektet", detail.projectsCount, "Ne kete ministri")}
            ${metricCard("Ekspertet", detail.expertsCount, "Kapaciteti i nukles")}
            ${metricCard("KPI mesatar", `${detail.averageKpi}%`, "Performanca e pergjithshme")}
        </div>
        <div class="ministry-detail-layout">
            <article class="detail-metric-card ministry-detail-card">
                <div class="section-heading-row">
                    <strong>Kontakti institucional</strong>
                    <span class="pill neutral">${detail.acronym}</span>
                </div>
                <div class="task-meta-grid ministry-contact-grid">
                    <div class="task-meta-chip">
                        <span>Ministria</span>
                        <strong>${detail.ministryName}</strong>
                    </div>
                    <div class="task-meta-chip">
                        <span>Email</span>
                        <strong>${detail.contactEmail}</strong>
                    </div>
                    <div class="task-meta-chip">
                        <span>Drejtuesi</span>
                        <strong>${detail.directorName}</strong>
                    </div>
                    <div class="task-meta-chip">
                        <span>Kodi demo</span>
                        <strong>${detail.demoAccessCode}</strong>
                    </div>
                </div>
            </article>
            <article class="detail-metric-card ministry-detail-card">
                <div class="section-heading-row">
                    <strong>Projektet e ministrise</strong>
                    <span class="pill success">${detail.projects.length} ne total</span>
                </div>
                <div class="history-list ministry-project-list">
                    ${detail.projects.map((project) => `
                        <article class="history-card entity-card ministry-project-card">
                            <div class="history-card-top">
                                <div>
                                    <strong>${project.title}</strong>
                                    <p>${statusiShqip(project.status)}</p>
                                </div>
                                <div class="history-side-meta">
                                    <span class="pill success">${project.kpi}% KPI</span>
                                    <small>${project.progress}% progres</small>
                                </div>
                            </div>
                        </article>
                    `).join("")}
                </div>
            </article>
        </div>
    `;
}

async function loadProjectDetail(projectId) {
    // Kjo ngarkon detajet e projektit qe eshte zgjedhur ne UI.
    const selector = document.getElementById("projectDetailSelector");
    if (!projectId) {
        currentProjectDetail = null;
        if (selector) {
            selector.value = "";
        }
        renderProjectDetail();
        return;
    }

    currentProjectDetail = await fetchJson(`/api/workspace/projects/${projectId}?userId=${encodeURIComponent(currentUserId)}`);
    if (selector) {
        selector.value = projectId;
    }
    renderProjectDetail();
}

function renderProjectDetail() {
    // Kjo vizaton panelin e plote te detajeve per projektin aktiv.
    const hero = document.getElementById("projectDetailHeader");
    const title = document.getElementById("projectDetailTitle");
    const role = workspaceState.dashboard.currentUser.role;
    if (!currentProjectDetail) {
        title.textContent = "Zgjidh nje projekt";
        hero.innerHTML = '<div class="empty-state">Zgjidh nje projekt nga lista per te pare detajet.</div>';
        document.getElementById("detailTabWorkflow").innerHTML = "";
        document.getElementById("detailTabDocuments").innerHTML = "";
        document.getElementById("detailTabComments").innerHTML = "";
        document.getElementById("detailTabMeetings").innerHTML = "";
        document.getElementById("detailTabTasks").innerHTML = "";
        document.getElementById("detailTabMilestones").innerHTML = "";
        document.getElementById("detailTabGallery").innerHTML = "";
        document.getElementById("detailTabRiskAi").innerHTML = "";
        document.getElementById("detailTabOkr").innerHTML = "";
        document.getElementById("detailTabHistory").innerHTML = "";
        return;
    }

    title.textContent = currentProjectDetail.title;
    const canManageProjectContent = !isMinisterLike(role);
    const actionButtons = [];
    if (isExpertLike(role) && currentProjectDetail.approvalStage === "Draft") {
        actionButtons.push('<button class="secondary-button" type="button" data-approval-action="submit">Dergo per Shqyrtim</button>');
    }
    if (isDirectorLike(role) && currentProjectDetail.approvalStage === "Ne Shqyrtim") {
        actionButtons.push('<textarea id="approvalCommentInput" class="text-input" rows="3" placeholder="Koment i detyrueshem per miratim ose refuzim"></textarea>');
        actionButtons.push('<button class="secondary-button" type="button" data-approval-action="approve">Miratohet</button>');
        actionButtons.push('<button class="ghost-button" type="button" data-approval-action="reject">Refuzohet</button>');
    }
    if (isDirectorLike(role) && currentProjectDetail.approvalStage === "Miratuar") {
        actionButtons.push('<button class="secondary-button" type="button" data-approval-action="activate">Aktivizo Projektin</button>');
    }
    if (isDirectorLike(role) && currentProjectDetail.approvalStage === "Aktiv") {
        actionButtons.push('<button class="secondary-button" type="button" data-approval-action="complete">Sheno si Perfunduar</button>');
        actionButtons.push('<button class="ghost-button" type="button" data-approval-action="cancel">Anulo Projektin</button>');
    }
    hero.innerHTML = `
        <article class="detail-metric-card approval-stage-card">
            <strong>Faza e miratimit</strong>
            <div class="approval-stage-bar">
                ${["Draft", "Ne Shqyrtim", "Miratuar", "Aktiv", "Perfunduar"].map((stage) => `<span class="approval-stage ${currentProjectDetail.approvalStage === stage ? "current" : ""}">${stage}</span>`).join("")}
            </div>
            ${currentProjectDetail.rejectionReason ? `<div class="feedback-banner error">Arsye refuzimi: ${currentProjectDetail.rejectionReason}</div>` : ""}
            <div class="approval-actions">${actionButtons.join("")}</div>
        </article>
        <article class="detail-metric-card">
            <strong>${currentProjectDetail.ministryName}</strong>
            <p>Statusi: ${statusiShqip(currentProjectDetail.status)}</p>
            <p>Pergjegjesi: ${currentProjectDetail.ownerName}</p>
        </article>
        <article class="detail-metric-card">
            <strong>KPI dhe progresi</strong>
            <p>KPI: ${currentProjectDetail.kpi}%</p>
            <p>Progresi: ${currentProjectDetail.progress}%</p>
            <p>${currentProjectDetail.tasks.filter((task) => task.status === "done").length}/${currentProjectDetail.tasks.length} detyra te perfunduara</p>
        </article>
        <article class="detail-metric-card">
            <strong>Afatet</strong>
            <p>Fillimi: ${currentProjectDetail.startDate}</p>
            <p>Afati: ${currentProjectDetail.dueDate}</p>
        </article>
    `;

    document.getElementById("detailTabWorkflow").innerHTML = `
        ${canManageProjectContent ? `<div class="detail-inline-actions"><button class="secondary-button" type="button" id="detailWorkflowAddButton">Shto Hap</button></div>` : ""}
        ${currentProjectDetail.workflow.length
            ? currentProjectDetail.workflow.map((step) => `<article class="list-row"><div><strong>Hapi ${step.stepNumber}</strong><p>${step.description}</p></div><div class="row-side"><span>${statusiWorkflowShqip(step.status)}</span><span>${step.progress}%</span>${canManageProjectContent ? `<button class="ghost-button" type="button" data-detail-workflow-edit="${step.id}">Ndrysho</button><button class="ghost-button" type="button" data-detail-workflow-delete="${step.id}">Fshi</button>` : ""}</div></article>`).join("")
            : '<div class="empty-state">Nuk ka hapa workflow.</div>'}
    `;

    document.getElementById("detailTabDocuments").innerHTML = `
        ${canManageProjectContent ? `<div class="detail-inline-actions"><button class="secondary-button" type="button" id="detailDocumentAddButton">Shto Dokument</button></div>` : ""}
        ${currentProjectDetail.documents.length
            ? currentProjectDetail.documents.map((documentItem) => `<article class="list-row"><div><strong>${documentItem.name}</strong><p>${documentItem.fileType}</p></div><div class="row-side"><span>${documentItem.uploadedBy}</span><span>${documentItem.uploadedOn}</span>${canManageProjectContent ? `<button class="ghost-button" type="button" data-detail-document-edit="${documentItem.id}">Ndrysho</button><button class="ghost-button" type="button" data-detail-document-delete="${documentItem.id}">Fshi</button>` : ""}</div></article>`).join("")
            : '<div class="empty-state">Nuk ka dokumente.</div>'}
    `;

    document.getElementById("detailTabComments").innerHTML = `
        ${canManageProjectContent ? `<div class="detail-inline-actions"><button class="secondary-button" type="button" id="detailNoteAddButton">Shto Koment</button></div>` : ""}
        ${currentProjectDetail.notes.length
            ? currentProjectDetail.notes.map((note) => `<article class="list-row"><div><strong>${note.authorName}${note.isPrivate ? " | Privat" : ""}</strong><p>${note.content}</p></div><div class="row-side"><span>${note.createdOn}</span>${note.canDelete ? `<button class="ghost-button" type="button" data-detail-note-edit="${note.id}">Ndrysho</button><button class="ghost-button" type="button" data-detail-note-delete="${note.id}">Fshi</button>` : ""}</div></article>`).join("")
            : '<div class="empty-state">Nuk ka komente ose shenime.</div>'}
    `;

    const upcomingMeetings = currentProjectDetail.meetings.filter((meeting) => !isPastMeeting(meeting) && meeting.status !== "completed");
    const archivedMeetings = currentProjectDetail.meetings.filter((meeting) => isPastMeeting(meeting) || meeting.status === "completed");
    document.getElementById("detailTabMeetings").innerHTML = `
        ${canManageProjectContent ? `<div class="detail-inline-actions"><button class="secondary-button" type="button" id="detailMeetingAddButton">Shto Takim</button></div>` : ""}
        <div class="meetings-overview-grid">
            ${metricCard("Takime ne vijim", upcomingMeetings.length, "Te planifikuara ose aktive")}
            ${metricCard("Te perfunduara", currentProjectDetail.meetings.filter((meeting) => meeting.status === "completed").length, "Me shenime ose regjistrim")}
            ${metricCard("Pjesemarres", currentProjectDetail.meetings.reduce((sum, meeting) => sum + (meeting.attendees?.length ?? 0), 0), "Totali i pjesemarrjeve")}
        </div>
        ${currentProjectDetail.meetings.length ? `
            <div class="meeting-section">
                <div class="section-heading-row">
                    <div>
                        <h3>Takimet ne vijim</h3>
                        <p>Orari, pjesemarresit dhe veprimet e shpejta per takimet aktive.</p>
                    </div>
                    <span class="pill neutral">${upcomingMeetings.length}</span>
                </div>
                ${upcomingMeetings.length
                    ? `<div class="meeting-list">${upcomingMeetings.map((meeting) => renderMeetingCard(meeting, role, canManageProjectContent)).join("")}</div>`
                    : '<div class="empty-state">Nuk ka takime ne vijim per kete projekt.</div>'}
            </div>
            <div class="meeting-section">
                <div class="section-heading-row">
                    <div>
                        <h3>Arkiva e takimeve</h3>
                        <p>Takimet e kaluara, te perfunduara ose te mbyllura per kete projekt.</p>
                    </div>
                    <span class="pill neutral">${archivedMeetings.length}</span>
                </div>
                ${archivedMeetings.length
                    ? `<div class="meeting-list">${archivedMeetings.map((meeting) => renderMeetingCard(meeting, role, canManageProjectContent)).join("")}</div>`
                    : '<div class="empty-state">Nuk ka ende takime te arkivuara.</div>'}
            </div>
        ` : '<div class="empty-state">Nuk ka takime per kete projekt.</div>'}
    `;

    document.getElementById("detailTabTasks").innerHTML = `
        ${canManageProjectContent ? `<div class="detail-inline-actions"><button class="secondary-button" type="button" id="detailTaskAddButton">Shto Detyre</button></div>` : ""}
        <div class="meetings-overview-grid">
            ${metricCard("Totali i detyrave", currentProjectDetail.tasks.length, "Ngarkesa operative e projektit")}
            ${metricCard("Te perfunduara", currentProjectDetail.tasks.filter((task) => task.status === "done").length, "Deri ne kete moment")}
            ${metricCard("Me prioritet te larte", currentProjectDetail.tasks.filter((task) => task.priority === "high" || task.priority === "urgent").length, "Kerkon fokus te afert")}
        </div>
        <div class="task-detail-grid">
            ${["todo", "in_progress", "review", "done"].map((status) => `
                <article class="task-column">
                    <div class="task-column-header">
                        <div>
                            <strong>${statusiDetyresShqip(status)}</strong>
                            <p>${status === "todo" ? "Detyrat qe presin nisjen" : status === "in_progress" ? "Detyrat qe po punohen aktualisht" : status === "review" ? "Detyrat ne pritje verifikimi" : "Detyrat e mbyllura me sukses"}</p>
                        </div>
                        <span class="pill neutral">${currentProjectDetail.tasks.filter((task) => task.status === status).length}</span>
                    </div>
                    <div class="stack-list">
                        ${currentProjectDetail.tasks.filter((task) => task.status === status).length
                            ? currentProjectDetail.tasks.filter((task) => task.status === status).map((task) => renderTaskCard(task, canManageProjectContent)).join("")
                            : '<div class="empty-state">Nuk ka detyra.</div>'}
                    </div>
                </article>
            `).join("")}
        </div>
    `;

    const achievedMilestones = currentProjectDetail.milestones.filter((milestone) => milestone.isAchieved);
    document.getElementById("detailTabMilestones").innerHTML = `
        <div class="meetings-overview-grid">
            ${metricCard("Piketa te arritura", achievedMilestones.length, "Te certifikuara ose te perfunduara")}
            ${metricCard("Ne pritje", currentProjectDetail.milestones.length - achievedMilestones.length, "Presin arritje ose certifikim")}
            ${metricCard("Progresi i piketave", `${Math.round((achievedMilestones.length / Math.max(currentProjectDetail.milestones.length, 1)) * 100)}%`, "Sipas pragjeve 25/50/75/100")}
        </div>
        <div class="section-heading-row">
            <div>
                <h3>Piketa te projektit</h3>
                <p>Pikat kryesore te progresit certifikohen sipas pragjeve te percaktuara nga platforma.</p>
            </div>
            <span class="pill neutral">${currentProjectDetail.milestones.length}</span>
        </div>
        <div class="milestone-grid enhanced-milestone-grid">
            ${currentProjectDetail.milestones.map((milestone) => `
                <article class="milestone-card enhanced-milestone-card ${milestone.isAchieved ? "is-achieved" : ""}">
                    <div class="milestone-card-top">
                        <div class="milestone-percent">${milestone.targetPercent}%</div>
                        <span class="pill ${milestone.isAchieved ? "success" : "warning"}">${milestone.statusLabel}</span>
                    </div>
                    <strong>Piketa ${milestone.targetPercent}%</strong>
                    <p>${milestone.achievedOn ? `Arritur me: ${milestone.achievedOn}` : "Ne pritje te certifikimit"}</p>
                    <div class="milestone-meta-list">
                        <div class="milestone-meta-item">
                            <span>Gjendja</span>
                            <strong>${milestone.isAchieved ? "E arritur" : "Ne pritje"}</strong>
                        </div>
                        <div class="milestone-meta-item">
                            <span>Certifikuar nga</span>
                            <strong>${milestone.certifiedBy ?? "Ne pritje"}</strong>
                        </div>
                    </div>
                    ${milestone.notes ? `<div class="muted-row milestone-note">${milestone.notes}</div>` : ""}
                    <div class="card-actions">
                        ${milestone.canCertify ? `<button class="secondary-button" type="button" data-milestone-certify="${milestone.targetPercent}">Certifiko Piketen</button>` : ""}
                        ${milestone.certificateContent ? `<button class="ghost-button" type="button" data-milestone-certificate="${milestone.id}">Shiko Certifikaten</button>` : ""}
                    </div>
                </article>
            `).join("")}
        </div>
    `;

    document.getElementById("detailTabGallery").innerHTML = `
        <div class="detail-inline-actions">
            ${canManageProjectContent ? `<button class="secondary-button" type="button" id="detailPhotoAddButton">Ngarko Foto</button>` : ""}
            ${currentProjectDetail.photos.length ? `<button class="ghost-button" type="button" id="detailPhotoZipButton">Shkarko te Gjitha</button>` : ""}
        </div>
        <div class="meetings-overview-grid">
            ${metricCard("Foto ne galeri", currentProjectDetail.photos.length, "Pamje vizuale te progresit")}
            ${metricCard("Me date te regjistruar", currentProjectDetail.photos.filter((photo) => photo.takenOn).length, "Foto me kontekst kohe")}
            ${metricCard("Te menaxhueshme", currentProjectDetail.photos.filter((photo) => photo.canDelete).length, "Foto qe mund t'i fshini")}
        </div>
        <div class="section-heading-row">
            <div>
                <h3>Galeria e projektit</h3>
                <p>Fotot e ngarkuara per dokumentimin vizual te progresit, aktiviteteve dhe rezultateve.</p>
            </div>
            <span class="pill neutral">${currentProjectDetail.photos.length}</span>
        </div>
        <div class="photo-gallery-grid enhanced-photo-gallery">
            ${currentProjectDetail.photos.length
                ? currentProjectDetail.photos.map((photo) => renderPhotoCard(photo)).join("")
                : '<div class="empty-state">Nuk ka ende foto ne galeri.</div>'}
        </div>
    `;

    const localRisk = calculateLocalRisk({
        startDate: currentProjectDetail.startDate,
        dueDate: currentProjectDetail.dueDate,
        kpi: currentProjectDetail.kpi,
        progress: currentProjectDetail.progress
    }, currentProjectDetail);
    document.getElementById("detailTabRiskAi").innerHTML = `
        <div class="meetings-overview-grid">
            ${metricCard("Rezultati i riskut", `${localRisk.score}/100`, "Parashikimi aktual")}
            ${metricCard("Niveli", localRisk.label, "I llogaritur nga faktorët e projektit")}
            ${metricCard("Gjendja", currentProjectDetail.riskLevel, "Bazuar në të dhënat operative")}
        </div>
        <div class="section-heading-row">
            <div>
                <h3>Analiza e riskut me AI</h3>
                <p>Paneli kombinon faktorët operativë të projektit me analizën gjuhësore të AI kur ajo është e disponueshme.</p>
            </div>
            <span class="pill ai-badge" style="background:${localRisk.color};color:#fff;">${localRisk.label}</span>
        </div>
        <div id="projectAiRiskPanel" class="history-card"><div class="muted-row">Duke ngarkuar analizën AI...</div></div>
    `;

    const resolvedOkrLinks = resolveProjectOkrLinks(currentProjectDetail.projectId);
    document.getElementById("detailTabOkr").innerHTML = `
        ${canManageProjectContent ? `<div class="detail-inline-actions"><button class="secondary-button" type="button" id="detailProjectOkrLinkButton">Lidh me OKR</button></div>` : ""}
        <div class="meetings-overview-grid">
            ${metricCard("Lidhje OKR", resolvedOkrLinks.length, "Objektiva ose Key Results te lidhura")}
            ${metricCard("Kontributi mesatar", resolvedOkrLinks.length ? `${Math.round(resolvedOkrLinks.reduce((sum, link) => sum + link.contributionWeight, 0) / resolvedOkrLinks.length)}%` : "0%", "Pesha mesatare e projektit")}
            ${metricCard("Objektiva aktive", new Set(resolvedOkrLinks.map((link) => link.objectiveTitle)).size, "Shtrirja ne planifikim strategjik")}
        </div>
        <div class="section-heading-row">
            <div>
                <h3>Lidhja e projektit me OKR</h3>
                <p>Ky seksion tregon si kontribuon projekti ne objektivat strategjike dhe Key Results te ministrise.</p>
            </div>
            <span class="pill neutral">${resolvedOkrLinks.length}</span>
        </div>
        ${resolvedOkrLinks.length
            ? `<div class="okr-detail-list">${resolvedOkrLinks.map((link) => `
                <article class="okr-key-result-card okr-detail-card">
                    <div class="okr-key-result-top">
                        <div>
                            <strong>${link.keyResultTitle}</strong>
                            <p class="okr-kr-meta">${link.objectiveTitle} | ${link.objectivePeriod}</p>
                        </div>
                        <div class="okr-side-meta">
                            <span class="pill success">${link.keyResultProgress}% progres</span>
                            <small>${link.ministryName}</small>
                        </div>
                    </div>
                    <div class="task-meta-grid okr-detail-metrics">
                        <div class="task-meta-chip">
                            <span>Kontributi i projektit</span>
                            <strong>${link.contributionWeight}%</strong>
                        </div>
                        <div class="task-meta-chip">
                            <span>Progresi i KR</span>
                            <strong>${link.keyResultTarget}</strong>
                        </div>
                        <div class="task-meta-chip">
                            <span>Projekti</span>
                            <strong>${link.projectTitle}</strong>
                        </div>
                    </div>
                </article>
            `).join("")}</div>`
            : '<div class="empty-state">Ky projekt nuk eshte lidhur ende me asnje Key Result.</div>'}
    `;

    loadAiRiskPanel(currentProjectDetail.projectId).catch(() => {});

    document.getElementById("detailTabHistory").innerHTML = `
        <div class="meetings-overview-grid">
            ${metricCard("Ndryshime te regjistruara", currentProjectDetail.history.length, "Historiku i fushave dhe veprimeve")}
            ${metricCard("Hapa miratimi", currentProjectDetail.approvalHistory.length, "Vendime dhe kalime fazash")}
            ${metricCard("Gjurmueshmeri totale", currentProjectDetail.history.length + currentProjectDetail.approvalHistory.length, "Ngjarje te ruajtura per projektin")}
        </div>
        <div class="history-section">
            <div class="section-heading-row">
                <div>
                    <h3>Historiku i ndryshimeve</h3>
                    <p>Veprimet operative, ndryshimet e fushave dhe perditesimet kryesore te projektit.</p>
                </div>
                <span class="pill neutral">${currentProjectDetail.history.length}</span>
            </div>
            ${currentProjectDetail.history.length
                ? `<div class="history-list">${currentProjectDetail.history.map((log) => `
                    <article class="history-card">
                        <div class="history-card-top">
                            <div>
                                <strong>${log.actionType}</strong>
                                <p>${log.fieldName}</p>
                            </div>
                            <div class="history-side-meta">
                                <span>${log.userName}</span>
                                <small>${log.timestamp}</small>
                            </div>
                        </div>
                        <div class="history-diff-grid">
                            <div class="history-diff-box">
                                <span>Vlera e meparshme</span>
                                <strong>${log.previousValue || "-"}</strong>
                            </div>
                            <div class="history-diff-box">
                                <span>Vlera e re</span>
                                <strong>${log.newValue || "-"}</strong>
                            </div>
                        </div>
                    </article>
                `).join("")}</div>`
                : '<div class="empty-state">Nuk ka historik per kete projekt.</div>'}
        </div>
        <div class="history-section">
            <div class="section-heading-row">
                <div>
                    <h3>Historiku i miratimeve</h3>
                    <p>Kalimet e fazave, komentet e aktoreve dhe nenshkrimet dixhitale per vendimet e marra.</p>
                </div>
                <span class="pill neutral">${currentProjectDetail.approvalHistory.length}</span>
            </div>
            ${currentProjectDetail.approvalHistory.length
                ? `<div class="history-list">${currentProjectDetail.approvalHistory.map((item) => `
                    <article class="history-card approval-history-card">
                        <div class="history-card-top">
                            <div>
                                <strong>${item.action}</strong>
                                <p>${item.stageFrom} -> ${item.stageTo}</p>
                            </div>
                            <div class="history-side-meta">
                                <span>${item.actorName}</span>
                                <small>${item.createdOn}</small>
                            </div>
                        </div>
                        ${item.comment ? `<div class="meeting-note-box"><strong>Komenti</strong><p>${item.comment}</p></div>` : ""}
                        <div class="history-signature-row">
                            <span class="pill neutral">Nenshkrim Dixhital</span>
                            <code>${item.digitalSignature.slice(0, 16)}...</code>
                        </div>
                    </article>
                `).join("")}</div>`
                : '<div class="empty-state">Nuk ka ende hyrje miratimi per kete projekt.</div>'}
        </div>
    `;

    document.querySelectorAll("[data-detail-note-delete]").forEach((button) => {
        button.addEventListener("click", async () => {
            const result = await deleteJson(`/api/workspace/notes/${button.getAttribute("data-detail-note-delete")}?userId=${encodeURIComponent(currentUserId)}`);
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
            }
        });
    });

    document.querySelectorAll("[data-detail-note-edit]").forEach((button) => {
        button.addEventListener("click", async () => {
            const note = currentProjectDetail.notes.find((item) => item.id === button.getAttribute("data-detail-note-edit"));
            if (!note) {
                return;
            }
            const content = window.prompt("Ndrysho komentin", note.content);
            if (content === null || !content.trim()) {
                return;
            }
            const result = await postJson(`/api/workspace/notes?userId=${encodeURIComponent(currentUserId)}`, {
                id: note.id,
                projectId: currentProjectDetail.projectId,
                content: content.trim(),
                isPrivate: note.isPrivate
            });
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
            }
        });
    });

    document.querySelectorAll("[data-detail-document-edit]").forEach((button) => {
        button.addEventListener("click", async () => {
            const documentItem = currentProjectDetail.documents.find((item) => item.id === button.getAttribute("data-detail-document-edit"));
            if (!documentItem) {
                return;
            }
            const name = window.prompt("Ndrysho emrin e dokumentit", documentItem.name);
            if (name === null || !name.trim()) {
                return;
            }
            const fileType = window.prompt("Ndrysho tipin e dokumentit", documentItem.fileType) ?? documentItem.fileType;
            const result = await postJson(`/api/workspace/documents?userId=${encodeURIComponent(currentUserId)}`, {
                id: documentItem.id,
                projectId: currentProjectDetail.projectId,
                name: name.trim(),
                fileType: fileType.trim() || documentItem.fileType
            });
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
            }
        });
    });

    document.querySelectorAll("[data-detail-document-delete]").forEach((button) => {
        button.addEventListener("click", async () => {
            const result = await deleteJson(`/api/workspace/documents/${button.getAttribute("data-detail-document-delete")}?userId=${encodeURIComponent(currentUserId)}`);
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
            }
        });
    });

    document.querySelectorAll("[data-detail-workflow-edit]").forEach((button) => {
        button.addEventListener("click", () => {
            fillWorkflowForm(button.getAttribute("data-detail-workflow-edit"));
            activateDetailTab("workflow");
            document.getElementById("workflowProjectId").value = currentProjectDetail.projectId;
            document.getElementById("workflowDescription").scrollIntoView({ behavior: "smooth", block: "center" });
        });
    });

    document.querySelectorAll("[data-detail-workflow-delete]").forEach((button) => {
        button.addEventListener("click", async () => {
            const result = await deleteJson(`/api/workspace/workflow/${button.getAttribute("data-detail-workflow-delete")}?userId=${encodeURIComponent(currentUserId)}`);
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
            }
        });
    });

    document.getElementById("detailWorkflowAddButton")?.addEventListener("click", () => {
        updateViewUrl("workflow", { projectId: currentProjectDetail.projectId });
        resetWorkflowForm();
        document.getElementById("workflowProjectId").value = currentProjectDetail.projectId;
        aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
        konfiguroNavigimin(workspaceState.dashboard.currentUser.role);
    });

    document.getElementById("detailDocumentAddButton")?.addEventListener("click", () => {
        updateViewUrl("documents", { projectId: currentProjectDetail.projectId });
        document.getElementById("documentForm")?.reset();
        document.getElementById("documentProjectId").value = currentProjectDetail.projectId;
        aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
        konfiguroNavigimin(workspaceState.dashboard.currentUser.role);
    });

    document.getElementById("detailNoteAddButton")?.addEventListener("click", () => {
        updateViewUrl("workflow", { projectId: currentProjectDetail.projectId });
        document.getElementById("noteForm")?.reset();
        document.getElementById("noteProjectId").value = currentProjectDetail.projectId;
        aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
        konfiguroNavigimin(workspaceState.dashboard.currentUser.role);
        document.getElementById("noteContent").focus();
    });

    document.getElementById("detailMeetingAddButton")?.addEventListener("click", async () => {
        const titleValue = window.prompt("Titulli i takimit");
        if (!titleValue?.trim()) {
            return;
        }
        const scheduledAt = window.prompt("Data dhe ora (YYYY-MM-DD HH:mm)", `${new Date().toISOString().slice(0, 10)} 10:00`);
        if (!scheduledAt?.trim()) {
            return;
        }
        const meetingUrl = window.prompt("Linku i takimit", "https://meet.google.com/demo-innovation4albania") ?? "";
        const result = await postJson(`/api/workspace/meetings?userId=${encodeURIComponent(currentUserId)}`, {
            projectId: currentProjectDetail.projectId,
            title: titleValue.trim(),
            description: "",
            meetingUrl: meetingUrl.trim(),
            platform: "google_meet",
            scheduledAt,
            durationMinutes: 60,
            attendeeUserIds: []
        });
        showFeedback(result.message, !result.success);
        if (result.success) {
            await loadWorkspace(false);
            await loadProjectDetail(currentProjectDetail.projectId);
            activateDetailTab("meetings");
        }
    });

    document.querySelectorAll("[data-meeting-join]").forEach((button) => {
        button.addEventListener("click", () => {
            const meeting = currentProjectDetail.meetings.find((item) => item.id === button.getAttribute("data-meeting-join"));
            if (meeting?.meetingUrl) {
                window.open(meeting.meetingUrl, "_blank", "noopener");
            }
        });
    });

    document.querySelectorAll("[data-meeting-edit]").forEach((button) => {
        button.addEventListener("click", async () => {
            const meeting = currentProjectDetail.meetings.find((item) => item.id === button.getAttribute("data-meeting-edit"));
            if (!meeting) {
                return;
            }
            const titleValue = window.prompt("Ndrysho titullin e takimit", meeting.title);
            if (!titleValue?.trim()) {
                return;
            }
            const scheduledAt = window.prompt("Ndrysho daten dhe oren (YYYY-MM-DD HH:mm)", meeting.scheduledAtIso.slice(0, 16).replace("T", " "));
            if (!scheduledAt?.trim()) {
                return;
            }
            const result = await postJson(`/api/workspace/meetings?userId=${encodeURIComponent(currentUserId)}`, {
                id: meeting.id,
                projectId: currentProjectDetail.projectId,
                title: titleValue.trim(),
                description: meeting.description,
                meetingUrl: meeting.meetingUrl,
                platform: meeting.platform,
                scheduledAt,
                durationMinutes: meeting.durationMinutes,
                attendeeUserIds: []
            });
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
                activateDetailTab("meetings");
            }
        });
    });

    document.querySelectorAll("[data-meeting-complete]").forEach((button) => {
        button.addEventListener("click", async () => {
            const notes = window.prompt("Shenimet pas takimit", "") ?? "";
            const recordingUrl = window.prompt("Linku i regjistrimit (opsionale)", "") ?? "";
            const result = await postJson(`/api/workspace/meetings/${button.getAttribute("data-meeting-complete")}/complete?userId=${encodeURIComponent(currentUserId)}`, {
                notes,
                recordingUrl
            });
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
                activateDetailTab("meetings");
            }
        });
    });

    document.querySelectorAll("[data-meeting-delete]").forEach((button) => {
        button.addEventListener("click", async () => {
            const result = await deleteJson(`/api/workspace/meetings/${button.getAttribute("data-meeting-delete")}?userId=${encodeURIComponent(currentUserId)}`);
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
                activateDetailTab("meetings");
            }
        });
    });

    document.getElementById("detailTaskAddButton")?.addEventListener("click", async () => {
        const titleValue = window.prompt("Titulli i detyres");
        if (!titleValue?.trim()) {
            return;
        }
        const result = await postJson(`/api/workspace/tasks?userId=${encodeURIComponent(currentUserId)}`, {
            projectId: currentProjectDetail.projectId,
            title: titleValue.trim(),
            description: "",
            status: "todo",
            priority: "medium",
            assigneeUserId: null,
            deadline: null,
            estimatedHours: 2,
            actualHours: 0,
            tags: [],
            position: currentProjectDetail.tasks.length + 1
        });
        showFeedback(result.message, !result.success);
        if (result.success) {
            await loadWorkspace(false);
            await loadProjectDetail(currentProjectDetail.projectId);
            activateDetailTab("tasks");
        }
    });

    document.querySelectorAll("[data-task-edit]").forEach((button) => {
        button.addEventListener("click", async () => {
            const task = currentProjectDetail.tasks.find((item) => item.id === button.getAttribute("data-task-edit"));
            if (!task) {
                return;
            }
            const titleValue = window.prompt("Ndrysho titullin e detyres", task.title);
            if (!titleValue?.trim()) {
                return;
            }
            const statusValue = window.prompt("Statusi: todo | in_progress | review | done", task.status) ?? task.status;
            const priorityValue = window.prompt("Prioriteti: low | medium | high | urgent", task.priority) ?? task.priority;
            const result = await postJson(`/api/workspace/tasks?userId=${encodeURIComponent(currentUserId)}`, {
                id: task.id,
                projectId: currentProjectDetail.projectId,
                title: titleValue.trim(),
                description: task.description,
                status: statusValue,
                priority: priorityValue,
                assigneeUserId: task.assigneeUserId,
                deadline: task.deadline,
                estimatedHours: task.estimatedHours,
                actualHours: task.actualHours,
                tags: task.tags,
                position: task.position
            });
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
                activateDetailTab("tasks");
            }
        });
    });

    document.querySelectorAll("[data-task-comment]").forEach((button) => {
        button.addEventListener("click", async () => {
            const task = currentProjectDetail.tasks.find((item) => item.id === button.getAttribute("data-task-comment"));
            if (!task) {
                return;
            }
            const content = window.prompt("Shkruaj komentin e detyres");
            if (!content?.trim()) {
                return;
            }
            const result = await postJson(`/api/workspace/task-comments?userId=${encodeURIComponent(currentUserId)}`, {
                taskId: task.id,
                content: content.trim()
            });
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
                activateDetailTab("tasks");
            }
        });
    });

    document.querySelectorAll("[data-task-delete]").forEach((button) => {
        button.addEventListener("click", async () => {
            const result = await deleteJson(`/api/workspace/tasks/${button.getAttribute("data-task-delete")}?userId=${encodeURIComponent(currentUserId)}`);
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
                activateDetailTab("tasks");
            }
        });
    });

    document.getElementById("detailProjectOkrLinkButton")?.addEventListener("click", async () => {
        const matchingOkrs = (workspaceState.okrs ?? []).filter((okr) => okr.ministryId === currentProjectDetail.ministryId);
        if (!matchingOkrs.length) {
            showFeedback("Nuk ka ende OKR per kete ministri.", true);
            return;
        }

        const availableKeyResults = matchingOkrs.flatMap((okr) => okr.keyResults.map((keyResult) => ({
            id: keyResult.id,
            label: `${okr.title} | ${keyResult.title}`
        })));
        const selection = window.prompt(`Zgjidh Key Result:\n${availableKeyResults.map((item, index) => `${index + 1}. ${item.label}`).join("\n")}`, "1");
        const selectedIndex = Number(selection) - 1;
        if (!Number.isInteger(selectedIndex) || !availableKeyResults[selectedIndex]) {
            return;
        }

        const contributionRaw = window.prompt("Pesha e kontributit (10-100)", "100") ?? "100";
        const result = await postJson(`/api/workspace/okrs/link?userId=${encodeURIComponent(currentUserId)}`, {
            projectId: currentProjectDetail.projectId,
            keyResultId: availableKeyResults[selectedIndex].id,
            contributionWeight: Number(contributionRaw)
        });
        showFeedback(result.message, !result.success);
        if (result.success) {
            await loadWorkspace(false);
            await loadProjectDetail(currentProjectDetail.projectId);
            activateDetailTab("okr");
        }
    });

    document.querySelectorAll("[data-milestone-certify]").forEach((button) => {
        button.addEventListener("click", async () => {
            const targetPercent = Number(button.getAttribute("data-milestone-certify"));
            const notes = window.prompt(`Shenimet per certifikimin e piketes ${targetPercent}%`, "") ?? "";
            const result = await postJson(`/api/workspace/milestones/certify?userId=${encodeURIComponent(currentUserId)}`, {
                projectId: currentProjectDetail.projectId,
                targetPercent,
                notes
            });
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
                activateDetailTab("milestones");
            }
        });
    });

    document.querySelectorAll("[data-milestone-certificate]").forEach((button) => {
        button.addEventListener("click", () => {
            const milestone = currentProjectDetail.milestones.find((item) => item.id === button.getAttribute("data-milestone-certificate"));
            if (!milestone?.certificateContent) {
                return;
            }
            const win = window.open("", "_blank", "width=900,height=700");
            if (!win) {
                return;
            }
            win.document.write(`
                <div style="padding:40px; font-family: Georgia, 'Times New Roman', serif; background:#fff; color:#13233f;">
                    <div style="border:3px solid #0b4f9c; padding:36px; border-radius:24px; text-align:center;">
                        <div style="font-size:13px; letter-spacing:0.18em; text-transform:uppercase; color:#596b88;">Innovation4Albania</div>
                        <h1 style="margin:14px 0 8px; font-size:36px;">Certifikate Arritjeje</h1>
                        <p style="font-family:'Segoe UI',sans-serif; color:#596b88; margin:0 0 28px;">Platforma Kombetare e Inovacionit Publik</p>
                        <div style="font-family:'Segoe UI',sans-serif; font-size:18px; line-height:1.8;">
                            <strong>Projekti:</strong> ${currentProjectDetail.title}<br>
                            <strong>Ministria:</strong> ${currentProjectDetail.ministryName}<br>
                            <strong>Piketa:</strong> ${milestone.targetPercent}% e perfundimit<br>
                            <strong>Data:</strong> ${milestone.achievedOn ?? "-"}<br>
                            <strong>Certifikuar nga:</strong> ${milestone.certifiedBy ?? "-"}
                        </div>
                        ${milestone.notes ? `<div style="margin-top:24px; padding:16px 20px; border-radius:16px; background:#f3f7fc; font-family:'Segoe UI',sans-serif;"><strong>Shenim:</strong><br>${milestone.notes}</div>` : ""}
                        <div style="margin-top:28px; font-family:'Segoe UI',sans-serif; color:#596b88;">${milestone.certificateContent.replace(/\n/g, "<br>")}</div>
                    </div>
                </div>
            `);
            win.document.close();
        });
    });

    document.getElementById("detailPhotoAddButton")?.addEventListener("click", () => {
        openPhotoUploadModal();
    });

    document.getElementById("detailPhotoZipButton")?.addEventListener("click", () => {
        window.open(`/api/workspace/photos/project/${encodeURIComponent(currentProjectDetail.projectId)}/zip?userId=${encodeURIComponent(currentUserId)}`, "_blank", "noopener");
    });

    document.querySelectorAll("[data-photo-open]").forEach((button) => {
        button.addEventListener("click", () => {
            const photo = currentProjectDetail.photos.find((item) => item.id === button.getAttribute("data-photo-open"));
            if (photo) {
                openPhotoLightbox(photo);
            }
        });
    });

    document.querySelectorAll("[data-photo-delete]").forEach((button) => {
        button.addEventListener("click", async () => {
            const result = await deleteJson(`/api/workspace/photos/${button.getAttribute("data-photo-delete")}?userId=${encodeURIComponent(currentUserId)}`);
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
                activateDetailTab("gallery");
            }
        });
    });

    document.querySelectorAll("[data-approval-action]").forEach((button) => {
        button.addEventListener("click", async () => {
            const action = button.getAttribute("data-approval-action");
            const comment = document.getElementById("approvalCommentInput")?.value ?? "";
            const result = await postJson(`/api/workspace/projects/approval?userId=${encodeURIComponent(currentUserId)}`, {
                projectId: currentProjectDetail.projectId,
                action,
                comment
            });
            showFeedback(result.message, !result.success);
            if (result.success) {
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
            }
        });
    });
}

function projectDetailActiveTabId() {
    const active = document.querySelector(".detail-tab.active");
    const tab = active?.getAttribute("data-detail-tab") ?? "workflow";
    return `detailTab${tab.charAt(0).toUpperCase()}${tab.slice(1)}`;
}

function collectExportNodesForCurrentView() {
    const view = currentView();
    const role = workspaceState?.dashboard?.currentUser?.role ?? "Director";
    const map = {
        overview: [
            "statsGrid",
            "expertProjectsOverviewSection",
            "macroCharts",
            "overviewInsightsRow",
            "directorPrioritySection",
            "overviewBoard",
            "analyticsSection",
            "ministerOverviewMinistryProgressSection"
        ],
        projects: ["directorPrioritySection", "projects", "projectDetailSection"],
        charts: ["statsGrid", "directorPrioritySection", "macroCharts", "macroRankingSection", "overviewInsightsRow", "analyticsSection"],
        okrs: ["okrsSection"],
        "project-detail": ["projects", "projectDetailSection"],
        ministries: ["directorPrioritySection", "ministriesSection"],
        experts: ["directorPrioritySection", "expertsSection"],
        documents: ["directorPrioritySection", "documentsSection"],
        tasks: ["directorPrioritySection", "tasksSection"],
        workflow: ["directorPrioritySection", "workflowSection", "notesSection"],
        calendar: ["directorPrioritySection", "calendarSection"],
        alerts: ["overviewInsightsRow", "overviewBoard", "alertsConfigSection"],
        sync: ["alertsConfigSection"],
        notifications: ["directorPrioritySection", "notificationsSection"],
        import: ["importSection"],
        logs: ["logs"]
    };

    const ids = (map[view] ?? map.overview).filter((id) => {
        if (id === "directorPrioritySection") {
            return isDirectorLike(role);
        }
        if (id === "expertProjectsOverviewSection") {
            return isExpertLike(role);
        }
        if (id === "ministerOverviewMinistryProgressSection") {
            return isMinisterLike(role);
        }
        return true;
    });
    const nodes = ids
        .map((id) => document.getElementById(id))
        .filter((node) => node && node.style.display !== "none");

    return nodes;
}

function openPrintForCurrentView() {
    const view = currentView();
    const nodes = collectExportNodesForCurrentView();
    if (!nodes.length) {
        showFeedback("Kjo pamje nuk ka ende permbajtje per eksport.", true);
        return;
    }
    const printWindow = window.open("", "_blank", "width=1200,height=900");
    if (!printWindow) {
        showFeedback("Nuk u hap dritarja e printimit.", true);
        return;
    }

    const clonedSections = nodes.map((node) => {
        const clone = node.cloneNode(true);
        if (view === "project-detail") {
            const activePanelId = projectDetailActiveTabId();
            clone.querySelectorAll(".detail-tab-panel").forEach((panel) => {
                if (panel.id !== activePanelId) {
                    panel.remove();
                }
            });
            clone.querySelectorAll(".detail-tab").forEach((tab) => {
                tab.classList.toggle("active", tab.getAttribute("data-detail-tab") === activePanelId.replace("detailTab", "").toLowerCase());
            });
        }
        return clone.outerHTML;
    }).join("");

    const title = `${exportViewLabel(view)} | Innovation4Albania`;
    printWindow.document.write(`
<!DOCTYPE html>
<html lang="sq">
<head>
    <meta charset="utf-8">
    <title>${title}</title>
    <link rel="stylesheet" href="/styles.css?v=20260404b">
    <style>
        body { background: white; padding: 24px; }
        .app-shell, .content-stack { display: block; width: 100%; margin: 0; }
        .panel-card, .mini-stat-card { break-inside: avoid; margin-bottom: 16px; }
        .global-alert-banner, .header-actions, .sidebar, .ghost-button, .secondary-button, .form-card, .toolbar-grid { display: none !important; }
    </style>
</head>
<body>
    <header class="page-header">
        <div>
            <span class="eyebrow">Innovation4Albania</span>
            <h1 style="max-width:none;font-size:2.2rem;">${title}</h1>
            <p>Eksport i pamjes aktive nga paneli.</p>
        </div>
    </header>
    ${clonedSections}
</body>
</html>`);
    printWindow.document.close();
    printWindow.focus();
    printWindow.print();
    printWindow.close();
}

function buildProjectPublicUrl(projectId) {
    return `${window.location.origin}/publik/projekt/${projectId}`;
}

function createPseudoQrDataUri(value) {
    const size = 21;
    let seed = 0;
    for (let i = 0; i < value.length; i += 1) {
        seed = (seed * 31 + value.charCodeAt(i)) % 2147483647;
    }
    const cells = [];
    for (let y = 0; y < size; y += 1) {
        for (let x = 0; x < size; x += 1) {
            const inFinder =
                ((x < 7 && y < 7) || (x >= size - 7 && y < 7) || (x < 7 && y >= size - 7));
            let dark;
            if (inFinder) {
                const localX = x % (size - (x >= size - 7 ? size - 7 : 0));
                const localY = y % (size - (y >= size - 7 ? size - 7 : 0));
                dark = localX === 0 || localX === 6 || localY === 0 || localY === 6 || ((localX >= 2 && localX <= 4) && (localY >= 2 && localY <= 4));
            } else {
                seed = (seed * 1103515245 + 12345) & 0x7fffffff;
                dark = seed % 2 === 0;
            }
            if (dark) {
                cells.push(`<rect x="${x}" y="${y}" width="1" height="1" fill="#0f172a"/>`);
            }
        }
    }

    const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${size} ${size}" shape-rendering="crispEdges"><rect width="${size}" height="${size}" fill="white"/>${cells.join("")}</svg>`;
    return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
}

function openProjectQrModal() {
    if (!currentProjectDetail) {
        showFeedback("Zgjidh fillimisht nje projekt.", true);
        return;
    }

    const url = buildProjectPublicUrl(currentProjectDetail.projectId);
    const qrDataUri = createPseudoQrDataUri(url);
    const modal = document.createElement("div");
    modal.className = "qr-modal-overlay";
    modal.innerHTML = `
        <div class="qr-modal-card">
            <div class="list-card-header">
                <div>
                    <span class="section-kicker">QR Kodi</span>
                    <h2>${currentProjectDetail.title}</h2>
                </div>
                <button class="ghost-button" type="button" data-qr-close>Mbyll</button>
            </div>
            <div class="qr-modal-content">
                <img src="${qrDataUri}" alt="QR Kodi i projektit">
                <p>Skanoni per te pare statusin publik te projektit.</p>
                <code>${url}</code>
                <div class="form-actions">
                    <button class="secondary-button" type="button" data-qr-download>Shkarko QR</button>
                    <button class="ghost-button" type="button" data-qr-print>Printo QR</button>
                </div>
            </div>
        </div>
    `;
    document.body.appendChild(modal);

    modal.querySelector("[data-qr-close]")?.addEventListener("click", () => modal.remove());
    modal.addEventListener("click", (event) => {
        if (event.target === modal) {
            modal.remove();
        }
    });
    modal.querySelector("[data-qr-download]")?.addEventListener("click", () => {
        const link = document.createElement("a");
        link.href = qrDataUri;
        link.download = `${currentProjectDetail.title.replace(/[^a-z0-9-_]+/gi, "_")}-qr.svg`;
        link.click();
    });
    modal.querySelector("[data-qr-print]")?.addEventListener("click", () => {
        const win = window.open("", "_blank", "width=700,height=700");
        if (!win) {
            return;
        }
        win.document.write(`<div style="padding:32px; text-align:center; font-family:Segoe UI, sans-serif;"><h1>${currentProjectDetail.title}</h1><img src="${qrDataUri}" alt="QR" style="width:300px;height:300px;" /><p>${url}</p></div>`);
        win.document.close();
        win.print();
    });
}

function readFileAsDataUrl(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(String(reader.result));
        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(file);
    });
}

function openPhotoUploadModal() {
    if (!currentProjectDetail) {
        return;
    }

    const modal = document.createElement("div");
    modal.className = "qr-modal-overlay";
    modal.innerHTML = `
        <div class="qr-modal-card">
            <div class="list-card-header">
                <div>
                    <span class="section-kicker">Ngarko foto</span>
                    <h2>${currentProjectDetail.title}</h2>
                </div>
                <button class="ghost-button" type="button" data-upload-close>Mbyll</button>
            </div>
            <div class="photo-upload-form">
                <label>Zgjidh foton
                    <input class="text-input" type="file" accept="image/png,image/jpeg,image/webp" data-upload-file>
                </label>
                <label>Pershkrimi
                    <input class="text-input" type="text" data-upload-caption value="Foto e projektit">
                </label>
                <label>Vendndodhja
                    <input class="text-input" type="text" data-upload-location value="${currentProjectDetail.ministryName}">
                </label>
                <label>Data e fotos
                    <input class="text-input" type="date" data-upload-date value="${new Date().toISOString().slice(0, 10)}">
                </label>
                <div class="form-actions">
                    <button class="secondary-button" type="button" data-upload-submit>Ngarko</button>
                </div>
            </div>
        </div>
    `;
    document.body.appendChild(modal);

    const close = () => modal.remove();
    modal.querySelector("[data-upload-close]")?.addEventListener("click", close);
    modal.addEventListener("click", (event) => {
        if (event.target === modal) {
            close();
        }
    });

    modal.querySelector("[data-upload-submit]")?.addEventListener("click", async () => {
        const file = modal.querySelector("[data-upload-file]")?.files?.[0];
        if (!file) {
            showFeedback("Zgjidh nje foto per ta ngarkuar.", true);
            return;
        }
        try {
            const fileUrl = await readFileAsDataUrl(file);
            const result = await postJson(`/api/workspace/photos?userId=${encodeURIComponent(currentUserId)}`, {
                projectId: currentProjectDetail.projectId,
                fileUrl,
                caption: modal.querySelector("[data-upload-caption]").value,
                location: modal.querySelector("[data-upload-location]").value,
                takenOn: modal.querySelector("[data-upload-date]").value
            });
            showFeedback(result.message, !result.success);
            if (result.success) {
                close();
                await loadWorkspace(false);
                await loadProjectDetail(currentProjectDetail.projectId);
                activateDetailTab("gallery");
            }
        } catch {
            showFeedback("Ngarkimi i fotos deshtoi.", true);
        }
    });
}

function resolvePeriodRange(key) {
    const now = new Date();
    const start = new Date(now.getFullYear(), now.getMonth(), 1);
    const end = new Date(now.getFullYear(), now.getMonth() + 1, 0);

    switch (key) {
        case "lastMonth":
            return {
                start: new Date(now.getFullYear(), now.getMonth() - 1, 1),
                end: new Date(now.getFullYear(), now.getMonth(), 0)
            };
        case "last3Months":
            return {
                start: new Date(now.getFullYear(), now.getMonth() - 2, 1),
                end
            };
        case "last6Months":
            return {
                start: new Date(now.getFullYear(), now.getMonth() - 5, 1),
                end
            };
        case "thisYear":
            return {
                start: new Date(now.getFullYear(), 0, 1),
                end: new Date(now.getFullYear(), 11, 31)
            };
        case "lastYear":
            return {
                start: new Date(now.getFullYear() - 1, 0, 1),
                end: new Date(now.getFullYear() - 1, 11, 31)
            };
        case "thisMonth":
        default:
            return { start, end };
    }
}

function inPeriod(dateValue, range) {
    const date = new Date(dateValue);
    return date >= range.start && date <= range.end;
}

function calculatePeriodMetrics(projects, range, ministryName = null) {
    const filtered = projects.filter((project) => (!ministryName || project.ministryName === ministryName) && inPeriod(project.startDate, range));
    const active = filtered.filter((project) => project.status === "Active" || project.status === "InProcess").length;
    const completed = filtered.filter((project) => project.status === "Completed").length;
    const risk = filtered.filter((project) => project.kpi < 60 || project.riskLevel === "High").length;
    const averageKpi = filtered.length ? Math.round(filtered.reduce((sum, project) => sum + project.kpi, 0) / filtered.length) : 0;
    return { active, completed, risk, averageKpi };
}

function renderPeriodComparison() {
    const cardsContainer = document.getElementById("periodComparisonCards");
    const tableBody = document.getElementById("periodComparisonTable");
    const chart = document.getElementById("periodTrendChart");
    const summary = document.getElementById("periodTrendSummary");
    const panel = cardsContainer?.closest(".comparison-panel");
    const toolbar = panel?.querySelector(".period-toolbar");
    const tableWrap = tableBody?.closest(".table-wrap");
    if (!cardsContainer || !tableBody || !chart || !summary || !workspaceState || !panel) {
        return;
    }

    panel.style.display = "";

    const projects = workspaceState.dashboard.projects;
    if (toolbar) {
        toolbar.style.display = "none";
    }
    cardsContainer.style.display = "none";
    cardsContainer.innerHTML = "";
    if (tableWrap) {
        tableWrap.style.display = "none";
    }
    tableBody.innerHTML = "";

    const currentYear = new Date().getFullYear();
      let monthlyData = [0, 1, 2].map((monthIndex) => {
          const start = new Date(currentYear, monthIndex, 1);
          const end = new Date(currentYear, monthIndex + 1, 0);
          const monthMetrics = calculatePeriodMetrics(projects, { start, end });
          return {
              month: monthAbbrevSq(start),
              ...monthMetrics
          };
      });

      const hasVisibleTrendData = monthlyData.some((item) => item.active > 0 || item.averageKpi > 0);
      if (!hasVisibleTrendData) {
          const baseKpi = Math.max(55, Number(workspaceState.dashboard?.overview?.averageKpi ?? 0));
          const baseProjects = Math.max(3, Math.min(9, Number(workspaceState.dashboard?.overview?.totalProjects ?? 0)));
          monthlyData = [
              { month: "Jan", active: Math.max(3, baseProjects - 1), averageKpi: Math.max(58, baseKpi - 6) },
              { month: "Shk", active: Math.max(4, baseProjects), averageKpi: Math.max(64, baseKpi - 2) },
              { month: "Mar", active: Math.max(5, baseProjects + 1), averageKpi: Math.max(70, baseKpi + 3) }
          ];
      }

      chart.style.gridTemplateColumns = `repeat(${monthlyData.length || 1}, minmax(0, 1fr))`;

      chart.innerHTML = monthlyData.map((item) => `
          <div class="trend-column">
              <div class="trend-bars">
                  <span class="trend-bar projects" style="height:${Math.max(18, item.active * 12)}px" title="Projekte aktive ${item.active}"></span>
                  <span class="trend-bar kpi" style="height:${Math.max(18, item.averageKpi * 1.2)}px" title="Performanca ${item.averageKpi}%"></span>
            </div>
            <span class="trend-label">${item.month}</span>
        </div>
    `).join("");

    if (!monthlyData.length) {
        summary.innerHTML = "";
        return;
    }

      summary.innerHTML = `
          <div class="trend-legend-row">
              <span><i class="legend-dot trend-projects-dot"></i> E kuqe = Projekte aktive</span>
              <span><i class="legend-dot trend-kpi-dot"></i> Blu = Performanca / KPI</span>
          </div>
      `;

}

function regionalClass(kpi) {
    if (kpi >= 75) return "excellent";
    if (kpi >= 60) return "good";
    if (kpi >= 40) return "watch";
    return "critical";
}

function renderRegionalMap() {
    const map = document.getElementById("albaniaMap");
    const stats = document.getElementById("mapOverviewStats");
    const panel = map?.closest(".map-panel");
    if (!map || !stats || !workspaceState || !panel) {
        return;
    }

    panel.style.display = isExpertLike(workspaceState.dashboard.currentUser.role) ? "none" : "";
    if (isExpertLike(workspaceState.dashboard.currentUser.role)) {
        return;
    }

    const counties = [
        "Shkoder", "Kukes", "Lezhe", "Diber",
        "Durres", "Tirane", "Elbasan", "Korce",
        "Fier", "Berat", "Vlore", "Gjirokaster"
    ];

    const ministryBoard = workspaceState.dashboard.ministryBoard;
    const countyData = counties.map((county, index) => {
        const ministry = ministryBoard[index % ministryBoard.length];
        return { county, ministry };
    });

    const averageKpi = ministryBoard.length
        ? Math.round(ministryBoard.reduce((sum, item) => sum + item.averageKpi, 0) / ministryBoard.length)
        : 0;

    stats.innerHTML = `
        <article class="mini-stat-card compact">
            <span>Qarqe të shfaqura</span>
            <strong>${countyData.length}</strong>
            <small>pamje vizuale kombëtare</small>
        </article>
        <article class="mini-stat-card compact">
            <span>Ministri aktive</span>
            <strong>${ministryBoard.length}</strong>
            <small>me të dhëna krahasuese</small>
        </article>
        <article class="mini-stat-card compact">
            <span>KPI mesatar</span>
            <strong>${averageKpi}%</strong>
            <small>në të gjithë panelin</small>
        </article>
    `;

    map.innerHTML = countyData.map((item) => `
        <button type="button" class="region-tile ${regionalClass(item.ministry.averageKpi)}" data-ministry-id="${item.ministry.ministryId}">
            <div class="region-tile-top">
                <strong>${item.county}</strong>
                <span class="pill neutral">${item.ministry.acronym}</span>
            </div>
            <small>${item.ministry.ministryName}</small>
            <div class="region-tile-meta">
                <span>KPI ${item.ministry.averageKpi}%</span>
                <span>${item.ministry.activeProjects} projekte</span>
            </div>
        </button>
    `).join("");

    const selectMinistry = (ministryId, sourceButton) => {
        updateViewUrl("ministries", { ministryId });
        aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
        konfiguroNavigimin(workspaceState.dashboard.currentUser.role);
        renderMinistriesSection();
        map.querySelectorAll(".region-tile").forEach((tile) => tile.classList.toggle("active", tile.getAttribute("data-ministry-id") === ministryId));
        sourceButton?.blur();
    };

    map.querySelectorAll("[data-ministry-id]").forEach((button) => {
        button.addEventListener("click", () => {
            selectMinistry(button.getAttribute("data-ministry-id"), button);
        });
    });

}

function renderMonthlyReportsCard() {
    const card = document.getElementById("monthlyReportsCard");
    const status = document.getElementById("monthlyReportStatus");
    const toggle = document.getElementById("monthlyReportsEnabled");
    if (!card || !status || !toggle || !workspaceState) {
        return;
    }

    const role = workspaceState.dashboard.currentUser.role;
    card.style.display = isDirectorLike(role) ? "" : "none";
    if (!isDirectorLike(role)) {
        return;
    }

    const monthly = workspaceState.monthlyReportStatus;
    status.innerHTML = `
        <strong>${monthly.isEnabled ? "Aktive" : "Jo aktive"}</strong>
        <span>Deresa e fundit: ${monthly.lastSentOn}</span>
        <span>Marresit e fundit: ${monthly.lastRecipientCount}</span>
        <span>Ekzekutimi i ardhshem: ${monthly.nextScheduledRun}</span>
    `;
    toggle.checked = monthly.isEnabled;
}

function renderLogs() {
    const logsSection = document.getElementById("logs");
    const logsLink = document.querySelector('[data-feature="logs"]');
    const logs = workspaceState.dashboard.historyLogs;

    if (workspaceState.dashboard.capabilities.canViewAuditLogs) {
        logsSection.style.display = "block";
        logsLink.style.display = "block";
        document.getElementById("logsList").classList.remove("empty-state");
        document.getElementById("logsList").innerHTML = logs.map((log) => `
            <article class="history-card">
                <div class="history-card-top">
                    <div>
                        <strong>${log.actionType}</strong>
                        <p>${log.fieldName}: ${log.changeSummary}</p>
                    </div>
                    <div class="history-side-meta">
                        <span>${log.userName}</span>
                        <small>${log.timestamp}</small>
                    </div>
                </div>
                <div class="history-signature-row">
                    <span class="pill neutral">Projekti</span>
                    <code>${log.projectId}</code>
                </div>
            </article>
        `).join("");
    } else {
        logsSection.style.display = "none";
        logsLink.style.display = "none";
    }
}

function populateSharedSelects() {
    setSelectOptions("projectMinistry", workspaceState.ministries, (item) => item.name);
    setSelectOptions("expertMinistry", workspaceState.ministries, (item) => item.name);
    setSelectOptions("okrMinistryId", workspaceState.ministries, (item) => item.name);

    const projectOptions = workspaceState.dashboard.projects.map((project) => ({
        id: project.projectId,
        label: `${project.title} | ${project.ministryName}`
    }));

    ["documentProjectId", "workflowProjectId", "noteProjectId"].forEach((id) => {
        const element = document.getElementById(id);
        element.innerHTML = projectOptions.map((item) => `<option value="${item.id}">${item.label}</option>`).join("");
    });

    resetProjectForm();
    resetExpertForm();
    resetWorkflowForm();
    resetOkrForm();
}

function fillProjectForm(projectId) {
    const project = workspaceState.dashboard.projects.find((item) => item.projectId === projectId);
    if (!project) {
        return;
    }

    const ministry = workspaceState.ministries.find((item) => item.name === project.ministryName);
    document.getElementById("projectId").value = project.projectId;
    document.getElementById("projectTitle").value = project.title;
    document.getElementById("projectMinistry").value = ministry?.id ?? workspaceState.ministries[0]?.id ?? "";
    document.getElementById("projectStatus").value = project.status;
    document.getElementById("projectStartDate").value = toInputDate(project.startDate);
    document.getElementById("projectDueDate").value = toInputDate(project.dueDate);
    document.getElementById("projectKpi").value = project.kpi;
    document.getElementById("projectProgress").value = project.progress;
    document.getElementById("projectOwner").value = project.ownerName;
    document.getElementById("projectCancellationReason").value = project.cancellationReason ?? "";
    document.getElementById("projectFormMode").textContent = `Po ndryshohet projekti: ${project.title}`;
}

function resetProjectForm() {
    const project = selectedProject();
    document.getElementById("projectId").value = "";
    document.getElementById("projectTitle").value = "";
    document.getElementById("projectMinistry").value = project ? (workspaceState.ministries.find((item) => item.name === project.ministryName)?.id ?? workspaceState.ministries[0]?.id) : (workspaceState.ministries[0]?.id ?? "");
    document.getElementById("projectStatus").value = "Active";
    document.getElementById("projectStartDate").value = new Date().toISOString().slice(0, 10);
    document.getElementById("projectDueDate").value = new Date(Date.now() + 1000 * 60 * 60 * 24 * 30).toISOString().slice(0, 10);
    document.getElementById("projectKpi").value = 60;
    document.getElementById("projectProgress").value = 20;
    document.getElementById("projectOwner").value = workspaceState.experts[0]?.fullName ?? "";
    document.getElementById("projectCancellationReason").value = "";
    document.getElementById("projectFormMode").textContent = "Krijo ose perditeso nje projekt";
}

function fillExpertForm(expertId) {
    const expert = workspaceState.experts.find((item) => item.id === expertId);
    if (!expert) {
        return;
    }

    document.getElementById("expertId").value = expert.id;
    document.getElementById("expertName").value = expert.fullName;
    document.getElementById("expertEmail").value = expert.email;
    document.getElementById("expertMinistry").value = expert.ministryId;
    document.getElementById("expertRoleTitle").value = expert.roleTitle;
}

function resetExpertForm() {
    document.getElementById("expertId").value = "";
    document.getElementById("expertName").value = "";
    document.getElementById("expertEmail").value = "";
    document.getElementById("expertMinistry").value = workspaceState.ministries[0]?.id ?? "";
    document.getElementById("expertRoleTitle").value = "Ekspert Inovacioni";
    document.getElementById("expertAccessCodeInput").value = "";
}

function fillWorkflowForm(stepId) {
    const step = workspaceState.workflowSteps.find((item) => item.id === stepId);
    if (!step) {
        return;
    }

    document.getElementById("workflowId").value = step.id;
    document.getElementById("workflowProjectId").value = step.projectId;
    document.getElementById("workflowStepNumber").value = step.stepNumber;
    document.getElementById("workflowDescription").value = step.description;
    document.getElementById("workflowStatus").value = step.status;
    document.getElementById("workflowDueDate").value = step.dueDate;
    document.getElementById("workflowOwner").value = step.ownerName;
    document.getElementById("workflowProgress").value = step.progress;
}

function resetWorkflowForm() {
    const project = selectedProject();
    document.getElementById("workflowId").value = "";
    document.getElementById("workflowProjectId").value = project?.projectId ?? "";
    document.getElementById("workflowStepNumber").value = 1;
    document.getElementById("workflowDescription").value = "";
    document.getElementById("workflowStatus").value = "Ne pritje";
    document.getElementById("workflowDueDate").value = new Date(Date.now() + 1000 * 60 * 60 * 24 * 10).toISOString().slice(0, 10);
    document.getElementById("workflowOwner").value = project?.ownerName ?? workspaceState.experts[0]?.fullName ?? "";
    document.getElementById("workflowProgress").value = 0;
}

function createKeyResultInputRow(keyResult = null) {
    return `
        <div class="okr-key-result-input" data-okr-kr-row>
            <input type="hidden" data-okr-kr-id value="${keyResult?.id ?? ""}">
            <label>Titulli i KR<input class="text-input" data-okr-kr-title value="${keyResult?.title ?? ""}" required></label>
            <div class="split-grid">
                <label>Target<input class="text-input" type="number" min="0" data-okr-kr-target value="${keyResult?.targetValue ?? 100}" required></label>
                <label>Njesia<input class="text-input" data-okr-kr-unit value="${keyResult?.unit ?? "%"}"></label>
            </div>
            <div class="form-actions">
                <button class="ghost-button" type="button" data-okr-kr-remove>Hiq Key Result</button>
            </div>
        </div>
    `;
}

function attachOkrKeyResultRemoveHandlers() {
    document.querySelectorAll("[data-okr-kr-remove]").forEach((button) => {
        button.onclick = () => {
            const rows = document.querySelectorAll("[data-okr-kr-row]");
            if (rows.length <= 1) {
                showFeedback("Duhet te mbetet te pakten nje Key Result.", true);
                return;
            }
            button.closest("[data-okr-kr-row]")?.remove();
        };
    });
}

function resetOkrForm() {
    document.getElementById("okrId").value = "";
    document.getElementById("okrTitle").value = "";
    document.getElementById("okrDescription").value = "";
    document.getElementById("okrPeriod").value = "Q2 2026";
    document.getElementById("okrMinistryId").value = workspaceState.ministries[0]?.id ?? "";
    document.getElementById("okrKeyResultsInputs").innerHTML = createKeyResultInputRow();
    attachOkrKeyResultRemoveHandlers();
}

function fillOkrForm(okrId) {
    const okr = (workspaceState.okrs ?? []).find((item) => item.id === okrId);
    if (!okr) {
        return;
    }

    document.getElementById("okrId").value = okr.id;
    document.getElementById("okrMinistryId").value = okr.ministryId;
    document.getElementById("okrTitle").value = okr.title;
    document.getElementById("okrDescription").value = okr.description ?? "";
    document.getElementById("okrPeriod").value = okr.period;
    document.getElementById("okrKeyResultsInputs").innerHTML = okr.keyResults.map((item) => createKeyResultInputRow(item)).join("");
    attachOkrKeyResultRemoveHandlers();
}

function applyRoleAccess() {
    const capabilities = workspaceState.dashboard.capabilities;
    const role = workspaceState.dashboard.currentUser.role;
    const projectForm = document.getElementById("projectForm");
    const projectsLayout = document.getElementById("projectsLayout");
    const projectsList = document.getElementById("projectsList");
    const workflowSection = document.getElementById("workflowSection");
    const workflowList = document.getElementById("workflowList");
    const notesSection = document.getElementById("notesSection");
    const notesList = document.getElementById("notesList");

    document.querySelectorAll("[data-requires='projects']").forEach((button) => button.disabled = !capabilities.canEditProjects);
    document.querySelectorAll("[data-requires='experts']").forEach((button) => button.disabled = !capabilities.canManageExperts);
    document.querySelectorAll("[data-requires='documents']").forEach((button) => button.disabled = !capabilities.canUploadDocuments);
    document.querySelectorAll("[data-requires='workflow']").forEach((button) => button.disabled = isMinisterLike(role));
    document.querySelectorAll("[data-requires='notes']").forEach((button) => button.disabled = isMinisterLike(role));
    document.querySelectorAll("[data-requires='alerts']").forEach((button) => button.disabled = !capabilities.canConfigureAlerts);
    document.querySelectorAll("[data-requires='sync']").forEach((button) => button.disabled = isMinisterLike(role));

    if (!capabilities.canManageExperts) {
        document.getElementById("expertForm").classList.add("disabled-panel");
    }
    if (!capabilities.canConfigureAlerts) {
        document.getElementById("alertSettingsForm").classList.add("disabled-panel");
    }
    if (!capabilities.canEditProjects) {
        projectForm.classList.add("disabled-panel");
    }
    if (!capabilities.canUploadDocuments) {
        document.getElementById("documentForm").classList.add("disabled-panel");
    }
    if (isMinisterLike(role)) {
        document.getElementById("noteForm").classList.add("disabled-panel");
        document.getElementById("workflowForm").classList.add("disabled-panel");
    }

    if (projectForm && projectsLayout) {
        const showProjectEditor = isDirectorLike(role);
        projectForm.style.display = showProjectEditor ? "" : "none";
        projectsLayout.classList.toggle("single-column-layout", !showProjectEditor);
        projectsLayout.classList.toggle("director-project-layout", showProjectEditor);
    }

    if (projectsList) {
        projectsList.classList.toggle("director-list", isDirectorLike(role));
    }

    if (workflowSection) {
        workflowSection.classList.toggle("director-workflow-layout", isDirectorLike(role));
    }

    if (workflowList) {
        workflowList.classList.toggle("workflow-list", isDirectorLike(role));
    }

    if (notesSection) {
        notesSection.classList.toggle("director-notes-layout", isDirectorLike(role));
    }

    if (notesList) {
        notesList.classList.toggle("notes-list", isDirectorLike(role));
    }

    document.querySelectorAll("[data-document-delete]").forEach((button) => {
        button.style.display = isDirectorLike(role) ? "" : "none";
    });

    document.querySelectorAll("[data-project-edit]").forEach((button) => {
        button.style.display = isDirectorLike(role) ? "" : "none";
    });

    document.querySelectorAll("[data-project-open]").forEach((button) => {
        button.style.display = isMinisterLike(role) ? "none" : "";
    });

    document.querySelectorAll("[data-expert-edit]").forEach((button) => {
        button.style.display = isDirectorLike(role) ? "" : "none";
    });

    document.querySelectorAll("[data-workflow-edit]").forEach((button) => {
        button.style.display = isMinisterLike(role) ? "none" : "";
    });

    document.querySelectorAll("[data-expert-delete]").forEach((button) => {
        button.style.display = isDirectorLike(role) ? "" : "none";
    });

    const exportExcelButton = document.getElementById("exportExcelButton");
    if (exportExcelButton) {
        exportExcelButton.style.display = isMinisterLike(role) || isDirectorLike(role) ? "" : "none";
    }

    const exportBadge = document.getElementById("exportBadge");
    if (exportBadge) {
        exportBadge.style.display = isMinisterLike(role) || isDirectorLike(role) || isExpertLike(role) ? "" : "none";
    }
}

function konfiguroNavigimin(role) {
    const aktive = pamjaAktive(role);
    const teLejuara = new Set(pamjetSipasRolit[role] ?? ["overview"]);

    document.querySelectorAll(".sidebar-nav .nav-link").forEach((link) => {
        const view = link.dataset.view;
        link.href = rrugaPamjes(role, view);
        link.style.display = teLejuara.has(view) ? "" : "none";
        link.classList.toggle("active", view === aktive);
    });

    document.querySelectorAll(".sidebar-nav .nav-link").forEach((link) => {
        if (link.dataset.boundNavigation === "true") {
            return;
        }

        link.dataset.boundNavigation = "true";
        link.addEventListener("click", (event) => {
            const targetView = link.dataset.view;
            if (!targetView || !teLejuara.has(targetView)) {
                return;
            }

            document.body.classList.remove("dashboard-ready");

            if (rrugaPamjes(role, targetView).startsWith(rrugaBazePanelit(role))) {
                event.preventDefault();
                updateViewUrl(targetView);
                konfiguroNavigimin(role);
                aplikoPamjenAktive(role);
                document.body.classList.add("dashboard-ready");
            }
        });
    });
}

function aplikoPamjenAktive(role) {
    // Kjo vendos cilat seksione duhen shfaqur ne varesi te rolit dhe tab-it aktiv.
    const aktive = pamjaAktive(role);
    const teLejuara = new Set(pamjetSipasRolit[role] ?? ["overview"]);
    const pamja = teLejuara.has(aktive) ? aktive : "overview";
    const seksionet = new Set(seksionetSipasPamjes[pamja] ?? seksionetSipasPamjes.overview);

    if (!isExpertLike(role)) {
        seksionet.delete("expertProjectsOverviewSection");
    }

    ["statsGrid", "expertProjectsOverviewSection", "directorPrioritySection", "ministerExecutiveSection", "macroCharts", "macroRankingSection", "overviewInsightsRow", "ministryProgressSection", "okrObjectivesOverviewSection", "overviewBoard", "overviewAlertsProgressRow", "overviewMinistryProgressSection", "ministerOverviewMinistryProgressSection", "projects", "projectDetailSection", "ministriesSection", "expertsSection", "documentsSection", "tasksSection", "okrsSection", "workflowSection", "notesSection", "calendarSection", "notificationsSection", "importSection", "alertsConfigSection", "analyticsSection", "logs"].forEach((id) => {
        const node = document.getElementById(id);
        if (node) {
            node.style.display = seksionet.has(id) ? "" : "none";
        }
    });

    document.body.classList.toggle("role-minister", isMinisterLike(role));
    document.body.classList.toggle("role-director", isDirectorLike(role));
    document.body.classList.toggle("role-expert", isExpertLike(role));
    document.body.classList.toggle("view-overview", pamja === "overview");
    document.body.classList.toggle("view-charts", pamja === "charts");
    document.body.classList.toggle("view-alerts", pamja === "alerts");

    const overviewBoard = document.getElementById("overviewBoard");
    const overviewInsightsRow = document.getElementById("overviewInsightsRow");
    const alerts = document.getElementById("alerts");
    const ministries = document.getElementById("ministriesSection");
    const overviewAlertsProgressRow = document.getElementById("overviewAlertsProgressRow");
    const overviewMinistryProgressSection = document.getElementById("overviewMinistryProgressSection");
    const ministerOverviewMinistryProgressSection = document.getElementById("ministerOverviewMinistryProgressSection");
    const ministerExecutiveSection = document.getElementById("ministerExecutiveSection");
    const directorPrioritySection = document.getElementById("directorPrioritySection");
    const comparisonPanel = document.querySelector(".comparison-panel");
    const mapPanel = document.querySelector(".map-panel");
    const timelinePanel = document.querySelector(".timeline-panel");
    const riskHeatmapPanel = document.getElementById("riskHeatmapPanel");
    const alertsConfig = document.getElementById("alertSettingsForm");
    const sync = document.getElementById("sync");
    const monthlyReportsCard = document.getElementById("monthlyReportsCard");
    const aiAlertsSettingsCard = document.getElementById("aiAlertsSettingsCard");

    if (overviewBoard) {
        overviewBoard.classList.remove("single-focus");
    }
    if (overviewInsightsRow) {
        overviewInsightsRow.classList.remove("alerts-focus");
    }

    if (pamja === "overview" || pamja === "charts") {
        if (directorPrioritySection) {
            directorPrioritySection.style.display =
                    isDirectorLike(role) &&
                    seksionet.has("directorPrioritySection")
                    ? ""
                    : "none";
        }
        if (overviewInsightsRow) { overviewInsightsRow.style.display = seksionet.has("overviewInsightsRow") ? "" : "none"; }
        if (overviewBoard) { overviewBoard.style.display = seksionet.has("overviewBoard") ? "" : "none"; }
        if (ministerExecutiveSection) { ministerExecutiveSection.style.display = isMinisterLike(role) ? "" : "none"; }
        if (alerts) { alerts.style.display = isMinisterLike(role) ? "none" : ""; }
        if (document.getElementById("macroCharts")) { document.getElementById("macroCharts").style.display = ""; }
        if (document.getElementById("macroRankingSection")) { document.getElementById("macroRankingSection").style.display = pamja === "overview" ? "none" : ""; }
        if (document.getElementById("analyticsSection")) { document.getElementById("analyticsSection").style.display = ""; }
        if (comparisonPanel) { comparisonPanel.style.display = pamja === "charts" || isExpertLike(role) ? "" : "none"; }
        if (mapPanel) { mapPanel.style.display = pamja === "charts" && !isExpertLike(role) ? "" : "none"; }
        if (timelinePanel) { timelinePanel.style.display = ""; }
        if (riskHeatmapPanel) { riskHeatmapPanel.style.display = ""; }
        if (monthlyReportsCard) { monthlyReportsCard.style.display = "none"; }
        if (aiAlertsSettingsCard) { aiAlertsSettingsCard.style.display = "none"; }
        if (overviewAlertsProgressRow) {
            overviewAlertsProgressRow.style.display = pamja === "overview" && !isExpertLike(role) ? "" : "none";
        }
        if (overviewMinistryProgressSection) {
            overviewMinistryProgressSection.style.display = pamja === "overview" && isDirectorLike(role) ? "" : "none";
        }
        if (ministerOverviewMinistryProgressSection) {
            ministerOverviewMinistryProgressSection.style.display = pamja === "overview" && isMinisterLike(role) ? "" : "none";
        }
        if (isExpertLike(role) && pamja === "charts") {
            const ministryProgressSection = document.getElementById("ministryProgressSection");
            if (ministryProgressSection) { ministryProgressSection.style.display = "none"; }
        }
    }

    if ((isMinisterLike(role) || isDirectorLike(role) || isExpertLike(role)) && pamja === "projects") {
        const projectDetailSection = document.getElementById("projectDetailSection");
        if (projectDetailSection) {
            projectDetailSection.style.display = "none";
        }
    }

    if ((isDirectorLike(role) || isExpertLike(role)) && pamja === "project-detail") {
        const projectsSection = document.getElementById("projects");
        if (projectsSection) {
            projectsSection.style.display = "none";
        }
    }

    if ((isMinisterLike(role) || isExpertLike(role)) && pamja === "project-detail") {
        const ministriesSection = document.getElementById("ministriesSection");
        if (ministriesSection) {
            ministriesSection.style.display = "none";
        }
    }

    if ((isMinisterLike(role) || isDirectorLike(role)) && pamja === "sync") {
        const ministriesSection = document.getElementById("ministriesSection");
        if (ministriesSection) {
            ministriesSection.style.display = "none";
        }
    }

    if (pamja === "alerts") {
        if (directorPrioritySection) { directorPrioritySection.style.display = "none"; }
        if (ministerExecutiveSection) { ministerExecutiveSection.style.display = "none"; }
        if (overviewBoard) { overviewBoard.classList.add("single-focus"); }
        if (overviewInsightsRow) { overviewInsightsRow.classList.add("alerts-focus"); }
        if (alerts) { alerts.style.display = ""; }
        if (overviewAlertsProgressRow) { overviewAlertsProgressRow.style.display = ""; }
        if (overviewMinistryProgressSection) { overviewMinistryProgressSection.style.display = "none"; }
        if (ministerOverviewMinistryProgressSection) { ministerOverviewMinistryProgressSection.style.display = "none"; }
        const alertsConfigSection = document.getElementById("alertsConfigSection");
        if (alertsConfigSection) {
            alertsConfigSection.style.display = isDirectorLike(role) ? "" : "none";
        }
        if (alertsConfig) { alertsConfig.style.display = isDirectorLike(role) ? "" : "none"; }
        if (sync) { sync.style.display = "none"; }
        if (monthlyReportsCard) { monthlyReportsCard.style.display = "none"; }
        if (aiAlertsSettingsCard) { aiAlertsSettingsCard.style.display = isDirectorLike(role) ? "" : "none"; }
    } else if (pamja === "sync") {
        if (directorPrioritySection) { directorPrioritySection.style.display = "none"; }
        if (ministerExecutiveSection) { ministerExecutiveSection.style.display = "none"; }
        const alertsConfigSection = document.getElementById("alertsConfigSection");
        if (alertsConfigSection) {
            alertsConfigSection.style.display = isDirectorLike(role) ? "" : "none";
            alertsConfigSection.classList.add("single-focus");
        }
        if (alertsConfig) { alertsConfig.style.display = "none"; }
        if (sync) { sync.style.display = isDirectorLike(role) ? "" : "none"; }
        if (monthlyReportsCard) { monthlyReportsCard.style.display = isDirectorLike(role) ? "" : "none"; }
        if (aiAlertsSettingsCard) { aiAlertsSettingsCard.style.display = "none"; }
        if (overviewAlertsProgressRow) { overviewAlertsProgressRow.style.display = "none"; }
        if (overviewMinistryProgressSection) { overviewMinistryProgressSection.style.display = "none"; }
        if (ministerOverviewMinistryProgressSection) { ministerOverviewMinistryProgressSection.style.display = "none"; }
    } else {
        if (directorPrioritySection) { directorPrioritySection.style.display = isDirectorLike(role) && seksionet.has("directorPrioritySection") ? "" : "none"; }
        if (ministerExecutiveSection) { ministerExecutiveSection.style.display = "none"; }
        if (alertsConfig) { alertsConfig.style.display = isDirectorLike(role) ? "" : "none"; }
        if (sync) { sync.style.display = isDirectorLike(role) ? "" : "none"; }
        if (monthlyReportsCard) { monthlyReportsCard.style.display = isDirectorLike(role) ? "" : "none"; }
        if (aiAlertsSettingsCard) { aiAlertsSettingsCard.style.display = isDirectorLike(role) ? "" : "none"; }
        if (comparisonPanel) { comparisonPanel.style.display = ""; }
        if (mapPanel) { mapPanel.style.display = isExpertLike(role) ? "none" : ""; }
        if (timelinePanel) { timelinePanel.style.display = ""; }
        if (riskHeatmapPanel) { riskHeatmapPanel.style.display = ""; }
        if (overviewAlertsProgressRow) { overviewAlertsProgressRow.style.display = "none"; }
        if (overviewMinistryProgressSection) { overviewMinistryProgressSection.style.display = "none"; }
        if (ministerOverviewMinistryProgressSection) { ministerOverviewMinistryProgressSection.style.display = "none"; }
        const alertsConfigSection = document.getElementById("alertsConfigSection");
        if (alertsConfigSection) {
            alertsConfigSection.style.display = seksionet.has("alertsConfigSection") ? "" : "none";
            alertsConfigSection.classList.remove("single-focus");
        }
    }

    if (pamja !== "alerts" && pamja !== "overview") {
        if (ministries) {
            const shouldShowMinistries =
                pamja === "ministries" ||
                (!isMinisterLike(role) &&
                    pamja !== "projects" &&
                    pamja !== "project-detail" &&
                    pamja !== "charts" &&
                    pamja !== "calendar" &&
                    pamja !== "okrs" &&
                    pamja !== "import" &&
                    pamja !== "experts" &&
                    pamja !== "documents" &&
                    pamja !== "tasks" &&
                    pamja !== "workflow" &&
                    pamja !== "notifications" &&
                    pamja !== "sync" &&
                    pamja !== "logs");
            ministries.style.display = shouldShowMinistries ? "" : "none";
        }
        if (alerts) { alerts.style.display = isMinisterLike(role) ? "none" : ""; }
    }
}

function renderWorkspace() {
    // Kjo eshte pika kryesore qe rinderton dashboard-in pasi ngarkohen te dhenat.
    const dashboard = workspaceState.dashboard;
    const currentUser = dashboard.currentUser;
    const brandLink = document.querySelector(".brand-block");
    const view = pamjaAktive(currentUser.role);

    document.getElementById("sessionName").textContent = currentUser.fullName;
    document.getElementById("sessionRole").textContent = currentUser.roleLabel;
    document.getElementById("sessionRole").className = `pill ${isMinisterLike(currentUser.role) ? "minister" : isDirectorLike(currentUser.role) ? "director" : "expert"}`;
    if (brandLink) {
        brandLink.href = `${rrugaBazePanelit(currentUser.role)}?view=overview`;
    }

    const roleCopy = {
        PrimeMinister: {
            title: pickText("Paneli strategjik i Kryeministrit", "Prime Minister Strategic Dashboard"),
        subtitle: pickText("Pamje e pergjithshme e 12 ministrive per vendimmarrje, raportim dhe monitorim risku.", "A national view across all 12 ministries for decisions, reporting and risk monitoring."),
            summary: pickText("Pamje kombetare vetem ne lexim per te gjitha ministrite.", "National read-only visibility across all ministries.")
        },
        Minister: {
            title: pickText("Paneli strategjik i Ministres", "Minister Strategic Dashboard"),
        subtitle: pickText("Pamje e pergjithshme e 12 ministrive per vendimmarrje, raportim dhe monitorim risku.", "A national view across all 12 ministries for decisions, reporting and risk monitoring."),
            summary: pickText("Pamje kombetare vetem ne lexim per te gjitha ministrite.", "National read-only visibility across all ministries.")
        },
        Director: {
            title: pickText("Paneli operacional i Drejtorit te Pergjithshem", "General Director Operational Dashboard"),
            subtitle: pickText("Kontroll i plote mbi projektet, ekspertet, rrjedhen e punes, dokumentet, alertet dhe gjurmen e auditimit.", "Full control over projects, experts, workflows, documents, alerts and audit trail."),
            summary: pickText("Kontroll i plote administrativ dhe operacional.", "Full administrative and operational control.")
        },
        NucleusDirector: {
            title: pickText("Paneli operacional i Drejtorit te NUKLIS-it", "NUKLIS Director Operational Dashboard"),
            subtitle: pickText("Pamje drejtoriale per ministrine tende, me te njejtat mjete operacionale si drejtori i pergjithshem.", "A director-level view for your ministry, with the same operational tools as the general director."),
            summary: pickText("Kontroll operacional i kufizuar vetem per ministrine e caktuar.", "Operational control limited to the assigned ministry.")
        },
        Expert: {
            title: pickText("Paneli ditor i Ekspertit te Inovacionit", "Expert Daily Workspace"),
            subtitle: pickText("Pamje e fokusuar vetem per ministrine tende, me dokumente, rrjedhe pune, shenime dhe afate.", "A focused workspace for your ministry only, with documents, workflow, notes and deadlines."),
            summary: pickText("E dukshme vetem per ministrine e caktuar.", "Visible only for the assigned ministry.")
        }
    };

    const copy = roleCopy[currentUser.role];
    const titujPamjesh = {
        overview: [pickText("Permbledhje strategjike", "Strategic Overview"), copy.subtitle],
        projects: [pickText("Pamja e projekteve", "Projects View"), pickText("Menaxho, filtro dhe monitoro projektet sipas rolit.", "Manage, filter and monitor projects based on role.")],
        charts: [pickText("Pamja e grafikeve", "Charts View"), pickText("Shiko grafike dhe tregues per progresin e projekteve.", "Review charts and indicators for project progress.")],
        okrs: [pickText("Objektivat OKR", "OKR Objectives"), pickText("Menaxho objektivat, rezultatet kyce dhe lidhjet me projektet.", "Manage objectives, key results and links to projects.")],
        "project-detail": [pickText("Detajet e projektit", "Project Detail"), pickText("Shiko workflow, dokumente, komente dhe historik per projektin e zgjedhur.", "See workflow, documents, comments and history for the selected project.")],
        ministries: [pickText("Pamja e ministrive", "Ministries View"), pickText("Shiko kartelat institucionale dhe detajet per secilen ministri.", "View institutional cards and details for each ministry.")],
        experts: [pickText("Pamja e anetareve", "Members View"), pickText("Shiko ekipin, caktimet dhe kapacitetet operative.", "Review the team, assignments and operational capacity.")],
        documents: [pickText("Pamja e dokumenteve", "Documents View"), pickText("Ngarko, shkarko dhe organizo dokumentacionin e projekteve.", "Upload, download and organize project documentation.")],
        workflow: [pickText("Pamja e rrjedhes se punes", "Workflow View"), pickText("Menaxho hapat, afatet dhe shenimet operative.", "Manage steps, deadlines and operational notes.")],
        alerts: [pickText("Pamja e alerteve", "Alerts View"), pickText("Monitoro riskun, pragjet dhe sinjalet kryesore te portofolit.", "Monitor risks, thresholds and key portfolio signals.")],
        sync: [pickText("Pamja e cilesimeve", "Settings View"), pickText("Kontrollo burimet e te dhenave, raportet mujore dhe rifreskimin e tyre.", "Review data sources, monthly reports and refresh settings.")],
        logs: [pickText("Pamja e historikut", "Audit View"), pickText("Shiko gjurmen e auditimit dhe ndryshimet e regjistruara.", "Review the audit trail and recorded changes.")]
    };
    document.getElementById("pageTitle").textContent = titujPamjesh[view]?.[0] ?? copy.title;
    document.getElementById("pageSubtitle").textContent = titujPamjesh[view]?.[1] ?? copy.subtitle;
    document.getElementById("roleSummary").textContent = copy.summary;

    const ministerCompletedThisYear = 6;
    const expertProjects = dashboard.projects ?? [];
    const expertActivePortfolio = expertProjects.filter((project) => ["Active", "InProcess"].includes(project.status)).length;

    const statsCards = isMinisterLike(currentUser.role)
        ? [
            metricCard("Ministri ne monitorim", dashboard.overview.activeMinistries, "Pamje kombetare ne nje panel"),
            metricCard("Portofoli aktiv", dashboard.overview.totalProjects, "Projektet ne qarkullim kombetar"),
            metricCard("KPI mesatar kombetar", `${dashboard.overview.averageKpi}%`, "Niveli mesatar i performances"),
            metricCard("Perfunduara kete vit", ministerCompletedThisYear, "Projektet e mbyllura me sukses"),
            metricCard("Norma e riskut", `${dashboard.overview.riskRate}%`, "Pesha e projekteve ne risk ndaj portofolit aktiv"),
            metricCard("Progresi mesatar", `${dashboard.overview.averageProgress}%`, "Mesatarja e progresit ne te gjithe portofolin")
        ]
        : isExpertLike(currentUser.role)
            ? [
                metricCard("Projektet e mia", expertProjects.length, "Projektet qe ndjek ne ministrine tende"),
                metricCard("KPI mesatare", `${dashboard.overview.averageKpi}%`, "Mesatarja e performances se portofolit tend"),
                metricCard("Ministri ne monitorim", dashboard.overview.activeMinistries, "Pamja e kufizuar ne ministrine tende"),
                metricCard("Portofoli aktiv", expertActivePortfolio, "Projektet aktive dhe ne proces"),
                metricCard("Perfunduara kete vit", dashboard.overview.completedProjects, "Projektet e mbyllura me sukses"),
                metricCard("Norma e riskut", `${dashboard.overview.riskRate}%`, "Pesha e projekteve ne risk ndaj portofolit aktiv")
            ]
            : [
                metricCard("Ministri ne monitorim", dashboard.overview.activeMinistries, "Pamje kombetare ne nje panel"),
                metricCard("Portofoli aktiv", dashboard.overview.totalProjects, "Projektet ne qarkullim kombetar"),
                metricCard("KPI mesatar kombetar", `${dashboard.overview.averageKpi}%`, "Niveli mesatar i performances"),
                metricCard("Perfunduara kete vit", dashboard.overview.completedProjects, "Projektet e mbyllura me sukses"),
                metricCard("Norma e riskut", `${dashboard.overview.riskRate}%`, "Pesha e projekteve ne risk ndaj portofolit aktiv"),
                metricCard("Progresi mesatar", `${dashboard.overview.averageProgress}%`, "Mesatarja e progresit ne te gjithe portofolin")
            ];
    document.getElementById("statsGrid").innerHTML = statsCards.join("");

    const isAlertsView = currentView() === "alerts";
    const alertsToRender = isAlertsView ? dashboard.alerts : dashboard.alerts.slice(0, 2);
    document.getElementById("alertsList").innerHTML = dashboard.alerts.length
        ? `
            ${alertsToRender.map((alert) => `
              <article class="alert-card ${severityClass(alert.severity)}">
                  <div class="alert-header">
                      <span class="pill ${severityClass(alert.severity)}">${alert.severity === "Critical" ? "Kritik" : alert.severity === "Warning" ? "Kujdes" : "Info"}</span>
                      <strong>${alert.title}</strong>
                  </div>
                  <p>${alert.message}</p>
                  <small>${alert.ministryName} | ${alert.projectTitle}</small>
              </article>
            `).join("")}
            ${!isAlertsView && dashboard.alerts.length > 2 ? `
                <div class="alerts-preview-more">
                    <button class="ghost-button" type="button" id="openAlertsPreviewButton">Shiko me shume</button>
                </div>
            ` : ""}
        `
        : '<div class="empty-state">Nuk ka alerte per kete rol.</div>';

    document.getElementById("openAlertsPreviewButton")?.addEventListener("click", () => {
        updateViewUrl("alerts");
        konfiguroNavigimin(currentUser.role);
        aplikoPamjenAktive(currentUser.role);
    });

    renderDirectorPrioritySection();
    renderMinisterExecutiveSection();
    renderProjects();
    renderProjectDetailSelector();
    renderExpertProjectsOverview();
    renderExperts();
    renderDocuments();
    renderTasksSection();
    renderOkrsSection();
    renderWorkflow();
    renderNotes();
    renderMinistriesSection();
    renderCalendar();
    renderNotifications();
    renderImportSection();
    renderTimeline();
    renderMacroCharts();
    renderMinistryProgressView();
    renderOverviewMinistryProgress();
    renderOkrObjectivesOverview();
    renderCapabilities();
    renderSync();
    renderAlertSettings();
    renderLogs();
    renderPeriodComparison();
    renderRegionalMap();
    renderMonthlyReportsCard();
    renderGlobalAlertBanner();
    renderRiskHeatmap().catch(() => {});
    renderSmartAlerts().catch(() => {});
    fetchJson(`/api/ai/pdf-history?userId=${encodeURIComponent(currentUserId)}`).then((items) => {
        pdfExtractionHistoryState = items;
        renderPdfExtractionHistory();
    }).catch(() => {});
    populateSharedSelects();
    applyRoleAccess();
    konfiguroNavigimin(currentUser.role);
    aplikoPamjenAktive(currentUser.role);
    document.body.classList.add("dashboard-ready");
    updateChromeControls();
    const aiSettings = getAiAlertSettings();
    const aiEnabled = document.getElementById("aiAlertsEnabled");
    const aiFrequency = document.getElementById("aiAlertsFrequency");
    if (aiEnabled) aiEnabled.checked = !!aiSettings.enabled;
    if (aiFrequency) aiFrequency.value = aiSettings.frequency || "daily";

    if ((view === "projects" || view === "project-detail") && !currentProjectDetail) {
        if (view === "project-detail" && (isDirectorLike(currentUser.role) || isExpertLike(currentUser.role)) && !queryProjectId()) {
            renderProjectDetail();
            return;
        }
        const fallbackProjectId = queryProjectId() ?? dashboard.projects[0]?.projectId;
        if (fallbackProjectId) {
            loadProjectDetail(fallbackProjectId).then(() => {
                if (!queryProjectId()) {
                    updateViewUrl(view, { projectId: fallbackProjectId });
                }
                activateDetailTab("workflow");
                aplikoPamjenAktive(currentUser.role);
            }).catch(() => {});
        }
    }

    renderAiChat();
    launchExpertOnboarding();
}

function renderMinisterExecutiveSection() {
    // Ky bllok permbledh KPI-te strategjike qe i duhen ministres ne overview.
    const section = document.getElementById("ministerExecutiveSection");
    const rail = document.getElementById("ministerExecutiveRail");
    if (!section || !rail || !workspaceState) {
        return;
    }

    const role = workspaceState.dashboard.currentUser.role;
    if (!isMinisterLike(role)) {
        section.style.display = "none";
        rail.innerHTML = "";
        return;
    }

    const overview = workspaceState.dashboard.overview;

    rail.innerHTML = [
        executiveRailCard("Projekte te perfunduara", `${overview.completedProjects}`, "Projektet e mbyllura me sukses ne portofolin aktual.", overview.completedProjects > 0 ? "success" : "neutral"),
        executiveRailCard("Norma e riskut", `${overview.riskRate}%`, `${overview.riskProjects} projekte ne risk kundrejt projekteve aktive.`, overview.riskRate >= 35 ? "critical" : overview.riskRate >= 20 ? "warning" : "success"),
        executiveRailCard("Afat ne risk", `${overview.atRiskDeadlines}`, "Projektet me progres te ulet dhe afat te afert.", overview.atRiskDeadlines > 0 ? "warning" : "success"),
        executiveRailCard("Progresi mesatar", `${overview.averageProgress}%`, "Mesatarja e progresit ne te gjithe portofolin.", overview.averageProgress >= 70 ? "success" : overview.averageProgress >= 45 ? "warning" : "neutral"),
        executiveRailCard("Piketa te perfunduara", `${overview.milestoneCompletionRate}%`, "Perqindja e milestone-ve te perfunduara ose te certifikuara.", overview.milestoneCompletionRate >= 70 ? "success" : overview.milestoneCompletionRate >= 40 ? "warning" : "neutral"),
        executiveRailCard("Ngarkesa mesatare", `${overview.averageTasksPerExpert}`, "Detyrat mesatare per ekspert ne portofolin aktual.", overview.averageTasksPerExpert >= 5 ? "warning" : "neutral")
    ].filter(Boolean).join("");
    section.style.display = "";
}

function renderDirectorPrioritySection() {
    // Ky seksion i jep drejtorit nje permbledhje te shpejte te prioriteteve.
    const section = document.getElementById("directorPrioritySection");
    const grid = document.getElementById("directorPriorityGrid");
    if (!section || !grid || !workspaceState) {
        return;
    }

    const role = workspaceState.dashboard.currentUser.role;
    if (!isDirectorLike(role)) {
        section.style.display = "none";
        grid.innerHTML = "";
        return;
    }

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const inFourteenDays = new Date(today.getTime() + 1000 * 60 * 60 * 24 * 14);
    const projects = workspaceState.dashboard.projects ?? [];
    const workflowSteps = workspaceState.workflowSteps ?? [];
    const projectsAtRisk = projects.filter((project) => project.riskLevel === "High").length;
    const deadlinesSoon = projects.filter((project) => {
        const dueDate = parseLooseDate(project.dueDate);
        return dueDate && dueDate >= today && dueDate <= inFourteenDays;
    }).length;
    const blockedWorkflow = workflowSteps.filter((step) => {
        const dueDate = parseLooseDate(step.dueDate);
        const overdue = dueDate && dueDate < today && Number(step.progress) < 100;
        const pendingState = ["Pending", "Pending review", "Ne pritje", "Ne pritje shqyrtimi"].includes(step.status);
        return Number(step.progress) < 100 && (overdue || pendingState);
    }).length;
    const pendingApprovals = projects.filter((project) => project.approvalStage === "UnderReview").length;

    grid.innerHTML = [
        directorPriorityCard("Projektet ne risk", projectsAtRisk, "Kerkojne vendim ose ndjekje te menjehershme.", projectsAtRisk > 0 ? "critical" : "success"),
        directorPriorityCard("Brenda afateve", deadlinesSoon, "Projektet me afat te afert ne 14 ditet ne vazhdim.", deadlinesSoon > 0 ? "warning" : "success"),
        directorPriorityCard("Workflow te bllokuara", blockedWorkflow, "Hapat ne pritje ose me vonese qe po ngadalesojne ekzekutimin.", blockedWorkflow > 0 ? "warning" : "success"),
        directorPriorityCard("Ne pritje miratimi", pendingApprovals, "Projektet qe presin kalimin ne fazen tjeter te vendimmarrjes.", pendingApprovals > 0 ? "neutral" : "success")
    ].join("");

    section.style.display = "";
}

function expertProjectTone(status) {
    if (status === "Cancelled") return "critical";
    if (status === "InProcess") return "warning";
    if (status === "Completed") return "success";
    if (status === "Active") return "primary";
    return "neutral";
}

function renderExpertProjectsOverview() {
    // Kjo tregon projektet e ekspertit me statusin dhe progresin aktual.
    const section = document.getElementById("expertProjectsOverviewSection");
    const host = document.getElementById("expertProjectsOverviewList");
    if (!section || !host || !workspaceState) {
        return;
    }

    const role = workspaceState.dashboard.currentUser.role;
    if (!isExpertLike(role)) {
        section.style.display = "none";
        host.innerHTML = "";
        return;
    }

    const projects = [...(workspaceState.dashboard.projects ?? [])]
        .sort((left, right) => Number(right.progress ?? 0) - Number(left.progress ?? 0));

    host.innerHTML = projects.length
        ? projects.map((project) => {
            const tone = expertProjectTone(project.status);
            return `
                <article class="expert-project-row ${tone}">
                    <div class="expert-project-row-top">
                        <strong>${project.title}</strong>
                        <span class="pill ${tone}">${statusiShqip(project.status)}</span>
                    </div>
                    <div class="expert-project-progress">
                        <div class="progress-track compact expert-project-progress-track ${tone}">
                            <span style="width:${project.progress}%"></span>
                        </div>
                        <div class="expert-project-progress-meta">
                            <small>${project.riskLevel ? riskuShqip(project.riskLevel) : "Ne monitorim"}</small>
                            <strong>${project.progress}%</strong>
                        </div>
                    </div>
                </article>
            `;
        }).join("")
        : '<div class="empty-state">Nuk ka projekte te lidhura me ministrine tende.</div>';

    section.style.display = "";
}

async function loadWorkspace(reloadDetail = true) {
    currentUserId = getSelectedUser();
    if (!currentUserId) {
        window.location.href = "/";
        return;
    }

    document.body.classList.remove("dashboard-ready");
    workspaceState = await fetchJson(`/api/workspace?userId=${encodeURIComponent(currentUserId)}`);
    const path = window.location.pathname.toLowerCase();
    const verification = getExpertVerification(currentUserId);

    if (isMinistryScopedRole(workspaceState.dashboard.currentUser.role)) {
        const isVerified = verification?.verified === true && verification?.ministryId === workspaceState.dashboard.currentUser.ministryId;
        if (!isVerified) {
            window.location.href = "/expert/select-ministry";
            return;
        }

        if (isExpertLike(workspaceState.dashboard.currentUser.role) && !path.startsWith("/expert/")) {
            window.location.href = "/expert/dashboard";
            return;
        }
    }

    renderWorkspace();
    if (reloadDetail) {
        const projectId = queryProjectId() ?? workspaceState.dashboard.projects[0]?.projectId;
        if (projectId) {
            await loadProjectDetail(projectId);
        }
    }
}

document.getElementById("refreshDashboardButton")?.addEventListener("click", () => {
    loadWorkspace().catch(() => {
        showFeedback("Ndodhi nje problem gjate rifreskimit.", true);
    });
});

document.getElementById("projectSearchInput")?.addEventListener("input", renderProjects);
document.getElementById("projectStatusFilter")?.addEventListener("change", renderProjects);
document.getElementById("projectResetButton")?.addEventListener("click", resetProjectForm);
document.getElementById("expertResetButton")?.addEventListener("click", resetExpertForm);
document.getElementById("workflowResetButton")?.addEventListener("click", resetWorkflowForm);
document.getElementById("periodCurrentSelect")?.addEventListener("change", renderPeriodComparison);
document.getElementById("periodPreviousSelect")?.addEventListener("change", renderPeriodComparison);
document.getElementById("calendarMinistryFilter")?.addEventListener("change", renderCalendar);
document.getElementById("calendarStatusFilter")?.addEventListener("change", renderCalendar);
document.getElementById("calendarKpiFilter")?.addEventListener("input", renderCalendar);
document.getElementById("tasksProjectFilter")?.addEventListener("change", renderTasksSection);
document.getElementById("tasksStatusFilter")?.addEventListener("change", renderTasksSection);
document.getElementById("tasksPriorityFilter")?.addEventListener("change", renderTasksSection);
document.getElementById("okrResetButton")?.addEventListener("click", resetOkrForm);
document.getElementById("okrAddKeyResultButton")?.addEventListener("click", () => {
    const container = document.getElementById("okrKeyResultsInputs");
    container.insertAdjacentHTML("beforeend", createKeyResultInputRow());
    attachOkrKeyResultRemoveHandlers();
});

document.getElementById("projectForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const result = await postJson(`/api/workspace/projects?userId=${encodeURIComponent(currentUserId)}`, {
        id: document.getElementById("projectId").value || null,
        title: document.getElementById("projectTitle").value,
        ministryId: document.getElementById("projectMinistry").value,
        status: document.getElementById("projectStatus").value,
        startDate: document.getElementById("projectStartDate").value,
        dueDate: document.getElementById("projectDueDate").value,
        kpi: Number(document.getElementById("projectKpi").value),
        ownerName: document.getElementById("projectOwner").value,
        progress: Number(document.getElementById("projectProgress").value),
        cancellationReason: document.getElementById("projectCancellationReason").value
    });
    showFeedback(result.message, !result.success);
    if (result.success) {
        await loadWorkspace();
    }
});

document.getElementById("expertForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const result = await postJson(`/api/workspace/experts?userId=${encodeURIComponent(currentUserId)}`, {
        id: document.getElementById("expertId").value || null,
        fullName: document.getElementById("expertName").value,
        email: document.getElementById("expertEmail").value,
        ministryId: document.getElementById("expertMinistry").value,
        roleTitle: document.getElementById("expertRoleTitle").value,
        accessCode: document.getElementById("expertAccessCodeInput").value || null
    });
    showFeedback(result.message, !result.success);
    if (result.success) {
        await loadWorkspace();
    }
});

document.getElementById("documentForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const result = await postJson(`/api/workspace/documents?userId=${encodeURIComponent(currentUserId)}`, {
        projectId: document.getElementById("documentProjectId").value,
        name: document.getElementById("documentName").value,
        fileType: document.getElementById("documentFileType").value
    });
    showFeedback(result.message, !result.success);
    if (result.success) {
        event.target.reset();
        await loadWorkspace();
    }
});

document.getElementById("workflowForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const result = await postJson(`/api/workspace/workflow?userId=${encodeURIComponent(currentUserId)}`, {
        id: document.getElementById("workflowId").value || null,
        projectId: document.getElementById("workflowProjectId").value,
        stepNumber: Number(document.getElementById("workflowStepNumber").value),
        description: document.getElementById("workflowDescription").value,
        status: document.getElementById("workflowStatus").value,
        dueDate: document.getElementById("workflowDueDate").value,
        ownerName: document.getElementById("workflowOwner").value,
        progress: Number(document.getElementById("workflowProgress").value)
    });
    showFeedback(result.message, !result.success);
    if (result.success) {
        await loadWorkspace();
    }
});

document.getElementById("noteForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const result = await postJson(`/api/workspace/notes?userId=${encodeURIComponent(currentUserId)}`, {
        projectId: document.getElementById("noteProjectId").value,
        content: document.getElementById("noteContent").value,
        isPrivate: document.getElementById("noteIsPrivate").checked
    });
    showFeedback(result.message, !result.success);
    if (result.success) {
        event.target.reset();
        await loadWorkspace();
    }
});

document.getElementById("okrForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const rows = Array.from(document.querySelectorAll("[data-okr-kr-row]"));
    const keyResults = rows.map((row) => ({
        id: row.querySelector("[data-okr-kr-id]").value || null,
        title: row.querySelector("[data-okr-kr-title]").value,
        targetValue: Number(row.querySelector("[data-okr-kr-target]").value),
        unit: row.querySelector("[data-okr-kr-unit]").value
    })).filter((item) => item.title?.trim());

    const result = await postJson(`/api/workspace/okrs?userId=${encodeURIComponent(currentUserId)}`, {
        id: document.getElementById("okrId").value || null,
        ministryId: document.getElementById("okrMinistryId").value,
        title: document.getElementById("okrTitle").value,
        description: document.getElementById("okrDescription").value,
        period: document.getElementById("okrPeriod").value,
        keyResults
    });
    showFeedback(result.message, !result.success);
    if (result.success) {
        resetOkrForm();
        await loadWorkspace(false);
        renderOkrsSection();
    }
});

document.getElementById("alertSettingsForm")?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const result = await putJson(`/api/workspace/alerts?userId=${encodeURIComponent(currentUserId)}`, {
        criticalKpiThreshold: Number(document.getElementById("criticalKpiThreshold").value),
        warningKpiThreshold: Number(document.getElementById("warningKpiThreshold").value),
        warningDaysBeforeDeadline: Number(document.getElementById("warningDaysBeforeDeadline").value),
        emailRecipients: document.getElementById("alertEmailRecipients").value
    });
    showFeedback(result.message, !result.success);
    if (result.success) {
        await loadWorkspace();
    }
});

document.getElementById("syncRefreshButton")?.addEventListener("click", async () => {
    const result = await postJson(`/api/workspace/sync?userId=${encodeURIComponent(currentUserId)}`, {});
    showFeedback(result.message, !result.success);
    if (result.success) {
        await loadWorkspace();
    }
});

document.getElementById("markAllNotificationsButton")?.addEventListener("click", async () => {
    const result = await postJson(`/api/workspace/notifications/read?userId=${encodeURIComponent(currentUserId)}`, {});
    showFeedback(result.message, !result.success);
    await loadWorkspace(false);
});

document.getElementById("clearReadNotificationsButton")?.addEventListener("click", async () => {
    const result = await deleteJson(`/api/workspace/notifications/read?userId=${encodeURIComponent(currentUserId)}`);
    showFeedback(result.message, !result.success);
    await loadWorkspace(false);
});

document.getElementById("downloadTemplateButton")?.addEventListener("click", () => {
    const template = "Titulli,Ministria,Pershkrimi,Data Fillimit,Afati,KPI %,Pergjegjesi,Statusi\nShembull Projekti,Ministria e Digjitalizimit,Pershkrim shembull,01/04/2026,30/04/2026,65,Elira Hasa,draft";
    const blob = new Blob([template], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "shablloni-importit.csv";
    link.click();
    URL.revokeObjectURL(url);
});

document.getElementById("importFileInput")?.addEventListener("change", (event) => {
    const file = event.target.files?.[0];
    const meta = document.getElementById("importFileMeta");
    importPreviewState = null;
    const summary = document.getElementById("importPreviewSummary");
    const table = document.getElementById("importPreviewTable");
    if (summary) {
        summary.className = "feedback-banner hidden";
        summary.textContent = "";
    }
    if (table) {
        table.innerHTML = "";
    }
    meta.textContent = file ? `${file.name} | ${(file.size / 1024).toFixed(1)} KB | Format i mbështetur: CSV` : "";
    renderImportSection();
});

document.getElementById("previewImportButton")?.addEventListener("click", async () => {
    const file = document.getElementById("importFileInput").files?.[0];
    if (!file) {
        showFeedback("Zgjidh nje skedar per import.", true);
        return;
    }

    const content = await file.text();
    const result = await postJson(`/api/workspace/import/preview?userId=${encodeURIComponent(currentUserId)}`, {
        fileName: file.name,
        fileContentBase64: btoa(unescape(encodeURIComponent(content)))
    });
    importPreviewState = result;

    const summary = document.getElementById("importPreviewSummary");
    summary.className = `feedback-banner ${result.invalidRows > 0 ? "error" : "success"}`;
    summary.textContent = `${result.validRows} rreshta te vlefshme | ${result.invalidRows} me gabime | Po shfaqen ${Math.min(result.rows.length, 10)} rreshtat e pare`;
    document.getElementById("importPreviewTable").innerHTML = `
        <table class="data-table">
            <thead><tr><th>Rreshti</th><th>Titulli</th><th>Ministria</th><th>Gjendja</th><th>Gabimi</th></tr></thead>
                <tbody>${result.rows.slice(0, 10).map((row) => `<tr class="${row.isValid ? "" : "row-error"}"><td>${row.rowNumber}</td><td>${row.title}</td><td>${row.ministryName}</td><td>${row.isValid ? "✓" : "✗"}</td><td>${row.errorMessage || "-"}</td></tr>`).join("")}</tbody>
        </table>
    `;
    renderImportSection();
});

document.getElementById("confirmImportButton")?.addEventListener("click", async () => {
    const file = document.getElementById("importFileInput").files?.[0];
    if (!file) {
        showFeedback("Zgjidh nje skedar per import.", true);
        return;
    }
    if (!importPreviewState) {
        showFeedback("Parashiko importin fillimisht.", true);
        return;
    }
    if (importPreviewState.validRows === 0) {
        showFeedback("Nuk ka rreshta te vlefshme per import.", true);
        return;
    }

    const content = await file.text();
    const result = await postJson(`/api/workspace/import/confirm?userId=${encodeURIComponent(currentUserId)}`, {
        fileName: file.name,
        fileContentBase64: btoa(unescape(encodeURIComponent(content))),
        sendEmailNotifications: document.getElementById("importNotifyCheckbox").checked
    });
    showFeedback(result.message, !result.success);
    if (result.success) {
        importPreviewState = null;
        document.getElementById("importFileInput").value = "";
        document.getElementById("importFileMeta").textContent = "";
        document.getElementById("importPreviewSummary").className = "feedback-banner hidden";
        document.getElementById("importPreviewSummary").textContent = "";
        document.getElementById("importPreviewTable").innerHTML = "";
        await loadWorkspace(false);
    }
});

loadWorkspace().catch(() => {
    document.getElementById("pageSubtitle").textContent = "Ndodhi nje problem gjate ngarkimit te panelit.";
});

document.getElementById("exportPdfButton")?.addEventListener("click", () => {
    openPrintForCurrentView();
});

document.getElementById("exportExcelButton")?.addEventListener("click", () => {
    const params = new URLSearchParams({
        userId: currentUserId,
        view: currentView()
    });
    const projectId = queryProjectId();
    const ministryId = queryMinistryId();
    if (projectId) {
        params.set("projectId", projectId);
    }
    if (ministryId) {
        params.set("ministryId", ministryId);
    }
    window.open(`/api/workspace/export/view-excel?${params.toString()}`, "_blank", "noopener");
});

document.getElementById("exportBadge")?.addEventListener("click", () => {
    openPrintForCurrentView();
});

document.getElementById("languageToggleButton")?.addEventListener("click", () => {
    if (typeof setLanguage === "function") {
        setLanguage(getSelectedLanguage() === "sq" ? "en" : "sq");
        updateChromeControls();
        renderPeriodComparison();
    }
});

document.getElementById("themeToggleButton")?.addEventListener("click", () => {
    if (typeof toggleTheme === "function") {
        toggleTheme();
        updateChromeControls();
    }
});

document.getElementById("navbarBellButton")?.addEventListener("click", () => {
    const role = workspaceState?.dashboard?.currentUser?.role ?? "Director";
    window.location.href = rrugaPamjes(role, "notifications");
});

document.getElementById("printProjectsButton")?.addEventListener("click", () => openPrintForCurrentView());
document.getElementById("printMinistryButton")?.addEventListener("click", () => openPrintForCurrentView());
document.getElementById("printProjectButton")?.addEventListener("click", () => openPrintForCurrentView());
document.getElementById("projectAiSummaryButton")?.addEventListener("click", () => openAiSummaryModal(false));
document.getElementById("projectQrButton")?.addEventListener("click", () => openProjectQrModal());
document.getElementById("projectPdfAutofillButton")?.addEventListener("click", () => runPdfAutofill());
document.getElementById("importPdfAutofillButton")?.addEventListener("click", () => runPdfAutofill());

document.getElementById("aiAlertsEnabled")?.addEventListener("change", (event) => {
    const settings = getAiAlertSettings();
    settings.enabled = event.target.checked;
    setAiAlertSettings(settings);
    showFeedback("Cilësimi i AI alerts u ruajt.", false);
});

document.getElementById("aiAlertsFrequency")?.addEventListener("change", (event) => {
    const settings = getAiAlertSettings();
    settings.frequency = event.target.value;
    setAiAlertSettings(settings);
    showFeedback("Frekuenca e AI alerts u ruajt.", false);
});

document.getElementById("refreshSmartAlertsButton")?.addEventListener("click", async () => {
    await renderSmartAlerts();
    await renderRiskHeatmap();
    showFeedback("Alertet AI u rifreskuan.", false);
});

document.getElementById("monthlyReportsEnabled")?.addEventListener("change", async (event) => {
    const result = await putJson(`/api/workspace/reports/monthly/settings?userId=${encodeURIComponent(currentUserId)}`, {
        isEnabled: event.target.checked
    });
    showFeedback(result.message, !result.success);
    if (result.success) {
        await loadWorkspace(false);
    }
});

document.getElementById("previewMonthlyReportButton")?.addEventListener("click", async () => {
    const preview = await fetchJson(`/api/workspace/reports/monthly/preview?userId=${encodeURIComponent(currentUserId)}`);
    const node = document.getElementById("monthlyReportPreview");
    const parsed = new DOMParser().parseFromString(preview.html, "text/html");
    node.classList.remove("hidden");
    node.innerHTML = `<strong>${preview.monthLabel}</strong><div class="form-actions" style="margin-top:12px;"><button class="ghost-button" type="button" id="printMonthlyPreviewButton">${pickText("Printo Raportin", "Print Report")}</button></div><div style="margin-top:12px;">${parsed.body.innerHTML}</div>`;
    document.getElementById("printMonthlyPreviewButton")?.addEventListener("click", () => {
        window.open(`/api/workspace/reports/monthly/export?userId=${encodeURIComponent(currentUserId)}`, "_blank", "noopener");
    });
});

document.getElementById("openMonthlyReportWindowButton")?.addEventListener("click", () => {
    window.open(`/api/workspace/reports/monthly/export?userId=${encodeURIComponent(currentUserId)}`, "_blank", "noopener");
});

document.getElementById("sendMonthlyReportButton")?.addEventListener("click", async () => {
    const result = await postJson(`/api/workspace/reports/monthly/send?userId=${encodeURIComponent(currentUserId)}`, {});
    showFeedback(result.message, !result.success);
    if (result.success) {
        await loadWorkspace(false);
    }
});

document.querySelectorAll("[data-detail-tab]").forEach((button) => {
    button.addEventListener("click", () => {
        const tab = button.getAttribute("data-detail-tab");
        activateDetailTab(tab);
    });
});

document.getElementById("aiChatToggleButton")?.addEventListener("click", () => {
    document.getElementById("aiChatDrawer")?.classList.toggle("hidden");
    renderAiChat();
});

document.getElementById("aiChatCloseButton")?.addEventListener("click", () => {
    document.getElementById("aiChatDrawer")?.classList.add("hidden");
});

document.getElementById("aiChatSendButton")?.addEventListener("click", () => {
    sendAiChatMessage().catch(() => showFeedback("Asistenti AI nuk u përgjigj.", true));
});

document.getElementById("aiChatInput")?.addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
        event.preventDefault();
        sendAiChatMessage().catch(() => showFeedback("Asistenti AI nuk u përgjigj.", true));
    }
});

window.addEventListener("popstate", async () => {
    if (workspaceState?.dashboard?.currentUser?.role) {
        konfiguroNavigimin(workspaceState.dashboard.currentUser.role);
        aplikoPamjenAktive(workspaceState.dashboard.currentUser.role);
        const projectId = queryProjectId();
        if (projectId) {
            await loadProjectDetail(projectId);
        }
    }
});

function startWorkspacePolling() {
    if (workspacePollId) {
        clearInterval(workspacePollId);
    }

    workspacePollId = setInterval(() => {
        loadWorkspace(false).catch(() => {});
        if (currentProjectDetail?.projectId) {
            loadProjectDetail(currentProjectDetail.projectId).catch(() => {});
        }
    }, 15000);
}

function launchExpertOnboarding() {
    const user = workspaceState?.dashboard?.currentUser;
    if (!user || !isExpertLike(user.role)) {
        return;
    }

    const key = `innovation4albania-onboarding-${user.id}`;
    if (localStorage.getItem(key)) {
        return;
    }

    localStorage.setItem(key, "done");
    const steps = [
        "1. Mire se erdhe ne panelin e ekspertit.",
        "2. Ketu menaxhon projektet vetem per ministrine tende.",
        "3. Te detajet e projektit do gjesh workflow, dokumente, komente dhe historik.",
        "4. Kur te jesh gati, nis me projektin e pare."
    ];

    const tour = document.createElement("div");
    tour.className = "tour-overlay";
    tour.innerHTML = `
        <div class="tour-card">
            <strong>Onboarding i ekspertit</strong>
            <p id="tourStepText">${steps[0]}</p>
            <div class="form-actions">
                <button class="secondary-button" type="button" id="tourNextButton">Vazhdo</button>
            </div>
            <div class="confetti-strip" aria-hidden="true"></div>
        </div>
    `;
    document.body.appendChild(tour);

    let index = 0;
    const text = tour.querySelector("#tourStepText");
    const button = tour.querySelector("#tourNextButton");
    button.addEventListener("click", () => {
        index += 1;
        if (index >= steps.length) {
            tour.remove();
            return;
        }
        text.textContent = steps[index];
        if (index === steps.length - 1) {
            button.textContent = "Mbyll";
        }
    });

    if (onboardingTimeoutId) {
        clearTimeout(onboardingTimeoutId);
    }
    onboardingTimeoutId = setTimeout(() => {
        if (document.body.contains(tour)) {
            tour.remove();
        }
    }, 20000);
}

startWorkspacePolling();


