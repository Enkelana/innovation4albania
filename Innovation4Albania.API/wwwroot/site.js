const storageKey = "innovation4albania-demo-user";
const expertVerificationPrefix = "innovation4albania-expert-verified-";
const expertAttemptsPrefix = "innovation4albania-expert-attempts-";
const expertLockPrefix = "innovation4albania-expert-lock-";
const themeKey = "i4a-theme";
const langKey = "i4a-lang";

const translations = {
    sq: {
        landingCta: "Hyr ne Platforme ->",
        backTop: "Kthehu Lart",
        common: {
            loading: "Po ngarkohet...",
            loadingData: "Po ngarkohen te dhenat e platformes...",
            yes: "Po",
            no: "Jo"
        },
        nav: {
            overview: "Permbledhje",
            projects: "Projektet",
            calendar: "Kalendar",
            projectDetail: "Detajet e projektit",
            import: "Importo",
            ministries: "Ministrite",
            members: "Anetaret",
            documents: "Dokumentet",
            workflow: "Rrjedha e punes",
            alerts: "Alertet",
            settings: "Cilesimet",
            notifications: "Njoftimet",
            logs: "Historiku"
        },
        actions: {
            logout: "Dil nga demo",
            exportPdf: "Shkarko PDF",
            exportExcel: "Shkarko Excel",
            reportExport: "Eksport raporte",
            refresh: "Rifresko",
            continueSession: "Vazhdo sesionin",
            stayHome: "Qendro ne faqen kryesore",
            openPublic: "Hap Dashboard-in Publik",
            giveFeedback: "Jep Mendimin Tend",
            voteNow: "Voto tani",
            viewProjects: "Shiko projektet",
            goFeedback: "Shko te feedback",
            backPublic: "Kthehu te dashboard publik",
            sendFeedback: "Dergo Mendimin"
        },
        stats: {
            activeProjects: "Projekte Aktive",
            ministries: "Ministri te Angazhuara",
            experts: "Eksperte Inovacioni",
            averageKpi: "KPI Mesatar Kombetar"
        },
        landing: {
            platformName: "Platforma Kombetare e Inovacionit Publik",
            kicker: "Inovacioni publik shqiptar",
            title: "Inovacioni Publik Shqiptar — i Monitoruar, i Matur, i Suksesshem",
            subtitle: "Platforma kombetare per menaxhimin e projekteve te inovacionit ne 16 ministrite e Republikes se Shqiperise.",
            summary1Kicker: "Monitorim Kombetar",
            summary1Title: "16 ministri ne nje pamje te vetme",
            summary1Text: "Vendimmarrje me KPI, afate, progres dhe risk ne nje panel te unifikuar.",
            summary2Kicker: "Pune Operative",
            summary2Title: "Workflow, dokumente dhe raportim",
            summary2Text: "Drejtori dhe ekspertet menaxhojne projektet ne menyre te strukturuar dhe te gjurmueshme.",
            quickKicker: "Pamje e shpejte",
            activeMinistries: "Ministri aktive",
            roleAccess: "Akses i ndare sipas pergjegjesise",
            alertsOnePlace: "Risk, afate dhe KPI ne nje vend",
            strategicDash: "Dashboard Strategjik",
            workflowOps: "Workflow Operativ",
            reportsExport: "Eksport Raportesh",
            lastSession: "Sesioni i fundit",
            returnPanel: "Rikthehu ne panel",
            savedSession: "Ke nje sesion demo te ruajtur ne kete shfletues.",
            liveStats: "Statistika Live",
            nationalPerformance: "Performanca Kombetare",
            citizenKicker: "Pjesemarrja qytetare",
            citizenTitle: "Vleresoni projektet publike",
            citizenText: "Nga kjo faqe mund te shihni projektet me prioritet dhe te kaloni direkt te dashboard-i publik per votim dhe feedback.",
            priorityProjects: "Projektet me prioritet",
            topVotedCitizens: "Me te votuarat nga qytetaret",
            whatYouCanDo: "Cfare mund te beni",
            simpleParticipation: "Pjesemarrje e thjeshte",
            supportImpactTitle: "Mbeshteje projektet me impakt",
            supportImpactText: "Shikoni listen publike dhe jepni sinjal te qarte per nismat qe duhen mbeshtetur.",
            prioritiesTitle: "Vendosni prioritete",
            prioritiesText: "Sinjalizoni cilat projekte duhet te ecin me me shpejtesi ose me fokus me te madh.",
            concernsTitle: "Raportoni shqetesime",
            concernsText: "Dergo sugjerime, pyetje ose shqetesime ne panelin publik te qytetareve.",
            advantages: "Avantazhet Kryesore",
            platformOffer: "Cfare ofron platforma",
            realtimeTitle: "Monitorim ne Kohe Reale",
            realtimeText: "Gjurmoni KPI-te dhe afatet e cdo projekti",
            ministriesTitle: "16 Ministri te Integruara",
            ministriesText: "Nje platforme per gjithe qeverine shqiptare",
            autoAlertsTitle: "Alerts Automatike",
            autoAlertsText: "Njoftohuni menjehere kur projektet jane ne risk",
            howItWorks: "Si funksionon",
            workflowSteps: "Rrjedha e punes ne 3 hapa",
            step1: "Drejtori shton projektet dhe cakton ekspertet",
            step2: "Ekspertet perditesojne progresin dhe ngarkojne dokumentet",
            step3: "Ministrja monitoron performancen ne kohe reale",
            footerText: "Platforma kombetare per menaxhimin e inovacionit publik.",
            about: "Rreth Nesh",
            privacy: "Privatesia",
            contact: "Kontakt",
            copyright: "© 2025 Innovation4Albania — Ministria e Inovacionit"
        },
        login: {
            title: "Platforma e Menaxhimit te Projekteve",
            subtitle: "Platforme e centralizuar per monitorimin e projekteve te inovacionit publik ne 16 ministrite e Republikes se Shqiperise.",
            mvpProjects: "Projekte MVP",
            demoRoles: "Rolet demo",
            demoAccess: "Hyrje demo",
            chooseRole: "Zgjidh rolin",
            demoText: "Ky MVP simulon login sipas rolit dhe hap panelin me akses te filtruar.",
            backHome: "Kthehu ne faqen kryesore",
            demoCredentials: "Kredencialet demo",
            strategicPanel: "Panel strategjik dhe eksporte",
            directorOps: "CRUD, audit, alerts, eksporte",
            demoAccessCode: "Kodi demo i aksesit: MIN014",
            dataSources: "Burimet e te dhenave",
            dataSourcesText: "Struktura eshte pergatitur qe me vone te lidhet me Google Sheets, Excel dhe API sync pa ndryshuar UI-ne baze."
        },
        publicPage: {
            title: "Transparenca e Inovacionit Publik Shqiptar",
            topbarSubtitle: "Transparenca e Inovacionit Publik Shqiptar",
            home: "Faqja Kryesore",
            dashboardPublic: "Dashboard publik",
            subtitle: "Shiko projektet aktive, KPI-te, afatet dhe jep mendimin tend per prioritetet e inovacionit publik.",
            updated: "Perditesuar",
            updatedText: "Te dhenat agregohen nga portofoli kombetar dhe publikohen ne menyre te permbledhur.",
            citizenParticipation: "Pjesemarrje qytetare",
            voteAndFeedback: "Votoni dhe dergoni feedback",
            voteAndFeedbackText: "Mbeshteetje, shqetesime dhe sugjerime per projektet me impakt publik.",
            nationalSummary: "Permbledhja kombetare",
            keyStats: "Statistikat Kryesore",
            yourParticipation: "Pjesemarrja juaj",
            howContribute: "Si mund te kontribuoni",
            support: "Mbeshteje",
            supportText: "Jep sinjal pozitiv per projektet qe sjellin vlere publike.",
            priority: "Prioritet",
            priorityText: "Sheno projektet qe duhen trajtuar me me shume vemendje.",
            concern: "Shqetesim",
            concernText: "Raporto rreziqe, vonesa ose paqartesi qe duhen adresuar.",
            quickGuide: "Udhezim i shpejte",
            whatInPage: "Cfare do te gjeni ne kete faqe",
            guide1: "Projektet me te votuara nga qytetaret",
            guide2: "Pamje e ministrive me KPI dhe projekte aktive",
            guide3: "Liste publike me filtra per status dhe KPI",
            guide4: "Forme feedback-u per sugjerime, pyetje dhe shqetesime",
            priorityProjects: "Projektet me prioritet",
            topVoted: "Me te Votuarat",
            ministries: "Ministrite",
            institutionalNetwork: "Rrjeti institucional",
            publicVoting: "Votimi publik",
            voteProjects: "Voto Projektet",
            voteIntro: "Ketu mund te votosh me Mbeshteje, Prioritet ose Kam shqetesim per secilin projekt.",
            publicList: "Lista publike",
            projects: "Projektet",
            ministry: "Ministria",
            status: "Statusi",
            allStatuses: "Te gjitha statuset",
            active: "Aktiv",
            inProgress: "Ne proces",
            completed: "Perfunduar",
            cancelled: "Anuluar",
            kpiMinimum: "KPI minimum",
            progressTrend: "Trendi i progresit",
            monthlyCompletions: "Perfundimet mujore",
            giveOpinion: "Jep mendimin tend",
            citizenFeedback: "Feedback qytetar",
            projectOptional: "Projekti (opsionale)",
            category: "Kategoria",
            suggestion: "Sugjerim",
            concernOption: "Shqetesim",
            supportOption: "Mbeshteetje",
            question: "Pyetje",
            message: "Mesazhi",
            anonymous: "Dergoje ne menyre anonime",
            engagement: "Perfshirja qytetare",
            votingWorks: "Si funksionon votimi",
            supportVoteTitle: "Mbeshteje",
            supportVoteText: "Trego qe projekti meriton te vazhdoje dhe ka vlere publike.",
            priorityVoteTitle: "Prioritet",
            priorityVoteText: "Sinjalizo projektet qe duhen trajtuar me perparesi.",
            concernVoteTitle: "Kam shqetesim",
            concernVoteText: "Trego rastet kur afati, cilesia ose drejtimi i projektit te shqeteson."
        },
        publicProject: {
            kicker: "Pamja publike e projektit",
            back: "Kthehu te dashboard publik",
            milestones: "Piketa",
            certifiedProgress: "Progress i certifikuar",
            gallery: "Galeria",
            projectPhotos: "Foto te projektit"
        },
        expertAccess: {
            platformSubtitle: "Platforma e Menaxhimit te Projekteve",
            secureAccess: "Akses i siguruar",
            title: "Zgjidhni Ministrine dhe Futni Kodin e Aksesit",
            subtitle: "Ju duhet te verifikoheni para se te vazhdoni",
            yourMinistry: "Ministria juaj",
            chooseMinistry: "Zgjidhni ministrine...",
            accessCode: "Kodi i aksesit",
            secretCode: "Futni kodin sekret te ministrise",
            locked: "Mbyllur",
            show: "Shfaq",
            enterDashboard: "Hyr ne Dashboard ->",
            demoCodes: "Demo kodet"
        },
        roles: { PrimeMinister: "Kryeministri", Minister: "Ministrja", Director: "Drejtori i Pergjithshem", NucleusDirector: "Drejtori i NUKLIS-it", Expert: "Eksperti" }
    },
    en: {
        landingCta: "Enter Platform ->",
        backTop: "Back to Top",
        common: {
            loading: "Loading...",
            loadingData: "Platform data is loading...",
            yes: "Yes",
            no: "No"
        },
        nav: {
            overview: "Overview",
            projects: "Projects",
            calendar: "Calendar",
            projectDetail: "Project Detail",
            import: "Import",
            ministries: "Ministries",
            members: "Members",
            documents: "Documents",
            workflow: "Workflow",
            alerts: "Alerts",
            settings: "Settings",
            notifications: "Notifications",
            logs: "Audit Log"
        },
        actions: {
            logout: "Log Out",
            exportPdf: "Download PDF",
            exportExcel: "Download Excel",
            reportExport: "Export reports",
            refresh: "Refresh",
            continueSession: "Continue session",
            stayHome: "Stay on home page",
            openPublic: "Open Public Dashboard",
            giveFeedback: "Share Your Feedback",
            voteNow: "Vote now",
            viewProjects: "View projects",
            goFeedback: "Go to feedback",
            backPublic: "Back to public dashboard",
            sendFeedback: "Send Feedback"
        },
        stats: {
            activeProjects: "Active Projects",
            ministries: "Ministries Engaged",
            experts: "Innovation Experts",
            averageKpi: "National Average KPI"
        },
        landing: {
            platformName: "National Public Innovation Platform",
            kicker: "Albanian public innovation",
            title: "Albanian Public Innovation — Monitored, Measured, Successful",
            subtitle: "The national platform for managing innovation projects across the 16 ministries of the Republic of Albania.",
            summary1Kicker: "National Monitoring",
            summary1Title: "16 ministries in a single view",
            summary1Text: "Decision-making with KPI, deadlines, progress and risk in one unified panel.",
            summary2Kicker: "Operational Work",
            summary2Title: "Workflow, documents and reporting",
            summary2Text: "The director and experts manage projects in a structured and traceable way.",
            quickKicker: "Quick view",
            activeMinistries: "Active ministries",
            roleAccess: "Role-based access",
            alertsOnePlace: "Risk, deadlines and KPI in one place",
            strategicDash: "Strategic Dashboard",
            workflowOps: "Operational Workflow",
            reportsExport: "Report Exports",
            lastSession: "Last session",
            returnPanel: "Return to dashboard",
            savedSession: "You have a saved demo session in this browser.",
            liveStats: "Live statistics",
            nationalPerformance: "National Performance",
            citizenKicker: "Citizen participation",
            citizenTitle: "Evaluate public projects",
            citizenText: "From this page you can review priority projects and go directly to the public dashboard for voting and feedback.",
            priorityProjects: "Priority projects",
            topVotedCitizens: "Most voted by citizens",
            whatYouCanDo: "What you can do",
            simpleParticipation: "Simple participation",
            supportImpactTitle: "Support impactful projects",
            supportImpactText: "Review the public list and send a clear signal for initiatives that deserve support.",
            prioritiesTitle: "Set priorities",
            prioritiesText: "Indicate which projects should move faster or receive greater focus.",
            concernsTitle: "Report concerns",
            concernsText: "Send suggestions, questions or concerns through the public citizen panel.",
            advantages: "Key Advantages",
            platformOffer: "What the platform offers",
            realtimeTitle: "Real-Time Monitoring",
            realtimeText: "Track KPI and deadlines for every project",
            ministriesTitle: "16 Integrated Ministries",
            ministriesText: "One platform for the entire Albanian government",
            autoAlertsTitle: "Automatic Alerts",
            autoAlertsText: "Be notified immediately when projects are at risk",
            howItWorks: "How it works",
            workflowSteps: "3-step workflow",
            step1: "The Director adds projects and assigns experts",
            step2: "Experts update progress and upload documents",
            step3: "The Minister monitors performance in real time",
            footerText: "The national platform for public innovation management.",
            about: "About Us",
            privacy: "Privacy",
            contact: "Contact",
            copyright: "© 2025 Innovation4Albania — Ministry of Innovation"
        },
        login: {
            title: "Project Management Platform",
            subtitle: "A centralized platform for monitoring public innovation projects across the 16 ministries of the Republic of Albania.",
            mvpProjects: "MVP Projects",
            demoRoles: "Demo roles",
            demoAccess: "Demo access",
            chooseRole: "Choose a role",
            demoText: "This MVP simulates role-based login and opens the dashboard with filtered access.",
            backHome: "Back to home page",
            demoCredentials: "Demo credentials",
            strategicPanel: "Strategic panel and exports",
            directorOps: "CRUD, audit, alerts, exports",
            demoAccessCode: "Demo access code: MIN014",
            dataSources: "Data sources",
            dataSourcesText: "The structure is prepared to connect later with Google Sheets, Excel and API sync without changing the core UI."
        },
        publicPage: {
            title: "Transparency of Albanian Public Innovation",
            topbarSubtitle: "Transparency of Albanian Public Innovation",
            home: "Home Page",
            dashboardPublic: "Public dashboard",
            subtitle: "View active projects, KPI, deadlines and share your opinion on public innovation priorities.",
            updated: "Updated",
            updatedText: "Data is aggregated from the national portfolio and published in summarized form.",
            citizenParticipation: "Citizen participation",
            voteAndFeedback: "Vote and share feedback",
            voteAndFeedbackText: "Support, concerns and suggestions for projects with public impact.",
            nationalSummary: "National summary",
            keyStats: "Key Statistics",
            yourParticipation: "Your participation",
            howContribute: "How you can contribute",
            support: "Support",
            supportText: "Give a positive signal for projects that bring public value.",
            priority: "Priority",
            priorityText: "Mark projects that should receive more attention.",
            concern: "Concern",
            concernText: "Report risks, delays or ambiguities that should be addressed.",
            quickGuide: "Quick guide",
            whatInPage: "What you will find on this page",
            guide1: "The projects most voted by citizens",
            guide2: "A ministry view with KPI and active projects",
            guide3: "A public list with filters by status and KPI",
            guide4: "A feedback form for suggestions, questions and concerns",
            priorityProjects: "Priority projects",
            topVoted: "Most Voted",
            ministries: "Ministries",
            institutionalNetwork: "Institutional network",
            publicVoting: "Public voting",
            voteProjects: "Vote on Projects",
            voteIntro: "Here you can vote with Support, Priority or I have a concern for each project.",
            publicList: "Public list",
            projects: "Projects",
            ministry: "Ministry",
            status: "Status",
            allStatuses: "All statuses",
            active: "Active",
            inProgress: "In progress",
            completed: "Completed",
            cancelled: "Cancelled",
            kpiMinimum: "Minimum KPI",
            progressTrend: "Progress trend",
            monthlyCompletions: "Monthly completions",
            giveOpinion: "Share your opinion",
            citizenFeedback: "Citizen feedback",
            projectOptional: "Project (optional)",
            category: "Category",
            suggestion: "Suggestion",
            concernOption: "Concern",
            supportOption: "Support",
            question: "Question",
            message: "Message",
            anonymous: "Send anonymously",
            engagement: "Citizen engagement",
            votingWorks: "How voting works",
            supportVoteTitle: "Support",
            supportVoteText: "Show that the project deserves to continue and has public value.",
            priorityVoteTitle: "Priority",
            priorityVoteText: "Signal which projects should be treated as priorities.",
            concernVoteTitle: "I have a concern",
            concernVoteText: "Point out cases where the timeline, quality or direction of the project concerns you."
        },
        publicProject: {
            kicker: "Public project view",
            back: "Back to public dashboard",
            milestones: "Milestones",
            certifiedProgress: "Certified progress",
            gallery: "Gallery",
            projectPhotos: "Project photos"
        },
        expertAccess: {
            platformSubtitle: "Project Management Platform",
            secureAccess: "Secure access",
            title: "Select Ministry and Enter Access Code",
            subtitle: "You must verify before continuing",
            yourMinistry: "Your ministry",
            chooseMinistry: "Select ministry...",
            accessCode: "Access code",
            secretCode: "Enter the ministry secret code",
            locked: "Locked",
            show: "Show",
            enterDashboard: "Enter Dashboard ->",
            demoCodes: "Demo codes"
        },
        roles: { PrimeMinister: "Prime Minister", Minister: "Minister", Director: "General Director", NucleusDirector: "NUKLIS Director", Expert: "Expert" }
    }
};

function getTheme() {
    return localStorage.getItem(themeKey) || "light";
}

function setTheme(theme) {
    localStorage.setItem(themeKey, theme);
    document.documentElement.classList.toggle("theme-dark", theme === "dark");
}

function toggleTheme() {
    setTheme(getTheme() === "dark" ? "light" : "dark");
}

function getLanguage() {
    return localStorage.getItem(langKey) || "sq";
}

function setLanguage(lang) {
    localStorage.setItem(langKey, lang);
    applyStaticTranslations();
    applyLanguageMetadata();
}

function t(path) {
    const lang = getLanguage();
    return path.split(".").reduce((value, key) => value?.[key], translations[lang]) ?? path;
}

function applyStaticTranslations() {
    document.querySelectorAll("[data-i18n]").forEach((node) => {
        node.textContent = t(node.dataset.i18n);
    });
    document.querySelectorAll("[data-i18n-placeholder]").forEach((node) => {
        node.setAttribute("placeholder", t(node.dataset.i18nPlaceholder));
    });
    document.querySelectorAll("[data-i18n-title]").forEach((node) => {
        node.setAttribute("title", t(node.dataset.i18nTitle));
    });
}

function applyLanguageMetadata() {
    const lang = getLanguage();
    document.documentElement.lang = lang;

    const page = document.body?.dataset?.page;
    if (page === "landing") {
        document.title = lang === "en" ? "Innovation4Albania | Home" : "Innovation4Albania | Faqja Kryesore";
    } else if (page === "login") {
        document.title = lang === "en" ? "Innovation4Albania | Login" : "Innovation4Albania | Hyrja";
    } else if (page === "public-dashboard") {
        document.title = lang === "en"
            ? "Innovation4Albania | Transparency of Public Innovation"
            : "Innovation4Albania | Transparenca e Inovacionit Publik";

        const description = document.querySelector('meta[name="description"]');
        if (description) {
            description.setAttribute("content", lang === "en"
                ? "Innovation4Albania - Transparency of public innovation projects across the 16 ministries of the Republic of Albania."
                : "Innovation4Albania - Transparenca e projekteve publike te inovacionit ne 16 ministrite e Republikes se Shqiperise.");
        }

        const ogTitle = document.querySelector('meta[property="og:title"]');
        if (ogTitle) {
            ogTitle.setAttribute("content", lang === "en"
                ? "Innovation4Albania - Transparency of Public Projects"
                : "Innovation4Albania - Transparenca e Projekteve Publike");
        }

        const ogDescription = document.querySelector('meta[property="og:description"]');
        if (ogDescription) {
            ogDescription.setAttribute("content", lang === "en"
                ? "View active projects, KPI and the progress of Albanian public innovation."
                : "Shikoni projektet aktive, KPI-te dhe progresin e inovacionit publik shqiptar.");
        }
    } else if (page === "public-project") {
        document.title = lang === "en" ? "Innovation4Albania | Public Project" : "Innovation4Albania | Projekti Publik";
    } else if (page === "expert-access") {
        document.title = lang === "en" ? "Innovation4Albania | Ministry Access" : "Innovation4Albania | Aksesi i Ministrise";
    }
}

const perkthimRoleve = {
    PrimeMinister: "Kryeministri",
    Minister: "Ministrja",
    Director: "Drejtori i Pergjithshem",
    NucleusDirector: "Drejtori i NUKLIS-it",
    Expert: "Eksperti"
};

function needsMinistryAccess(role) {
    return role === "Expert" || role === "NucleusDirector";
}

function roliShqip(vlera) {
    return translations[getLanguage()].roles[vlera] ?? perkthimRoleve[vlera] ?? vlera;
}

async function requestJson(url, options = {}) {
    const response = await fetch(url, {
        cache: "no-store",
        headers: {
            "Content-Type": "application/json",
            ...(options.headers ?? {})
        },
        ...options
    });

    if (!response.ok) {
        throw new Error(`Kerkesa deshtoi: ${response.status}`);
    }

    if (response.status === 204) {
        return null;
    }

    return response.json();
}

function fetchJson(url) {
    return requestJson(url);
}

function postJson(url, body) {
    return requestJson(url, {
        method: "POST",
        body: JSON.stringify(body)
    });
}

function putJson(url, body) {
    return requestJson(url, {
        method: "PUT",
        body: JSON.stringify(body)
    });
}

function deleteJson(url) {
    return requestJson(url, { method: "DELETE" });
}

function showFeedback(message, isError = false) {
    const banner = document.getElementById("feedbackBanner");
    if (!banner) {
        window.alert(message);
        return;
    }

    banner.textContent = message;
    banner.className = `feedback-banner ${isError ? "error" : "success"}`;
    setTimeout(() => {
        banner.className = "feedback-banner hidden";
    }, 4000);
}

function setSelectedUser(userId) {
    localStorage.setItem(storageKey, userId);
}

function getSelectedUser() {
    return localStorage.getItem(storageKey);
}

function getExpertVerificationKey(userId) {
    return `${expertVerificationPrefix}${userId}`;
}

function getExpertAttemptsKey(userId) {
    return `${expertAttemptsPrefix}${userId}`;
}

function getExpertLockKey(userId) {
    return `${expertLockPrefix}${userId}`;
}

function getExpertVerification(userId) {
    const raw = sessionStorage.getItem(getExpertVerificationKey(userId));
    return raw ? JSON.parse(raw) : null;
}

function clearExpertSessionState(userId) {
    if (!userId) {
        return;
    }

    sessionStorage.removeItem(getExpertVerificationKey(userId));
    sessionStorage.removeItem(getExpertAttemptsKey(userId));
    sessionStorage.removeItem(getExpertLockKey(userId));
}

function logout() {
    clearExpertSessionState(getSelectedUser());
    localStorage.removeItem(storageKey);
    window.location.href = "/login";
}

document.querySelectorAll('[data-action="logout"]').forEach((button) => {
    button.addEventListener("click", logout);
});

async function bootLoginPage() {
    const container = document.getElementById("demoUsers");
    if (!container) {
        return;
    }

    const users = await fetchJson("/api/auth/users");
    container.innerHTML = users.map((user) => `
        <button class="login-card" type="button" data-user-id="${user.id}">
            <span class="section-kicker">${roliShqip(user.role)}</span>
            <strong>${user.fullName}</strong>
            <span>${user.roleLabel}</span>
            <small>${user.email}</small>
        </button>
    `).join("");

    container.querySelectorAll("[data-user-id]").forEach((button) => {
        button.addEventListener("click", () => {
            const userId = button.getAttribute("data-user-id");
            const user = users.find((item) => item.id === userId);
            setSelectedUser(userId);
            clearExpertSessionState(userId);
            window.location.href = needsMinistryAccess(user?.role) ? "/expert/select-ministry" : "/dashboard.html";
        });
    });
}

async function bootLandingPage() {
    const selectedUser = getSelectedUser();
    const heroMetrics = document.getElementById("landingHeroMetrics");
    if (heroMetrics) {
        heroMetrics.innerHTML = `
            <article>
                <strong>16</strong>
                <span>${getLanguage() === "en" ? "ministries in one national view" : "ministri ne nje pamje kombetare"}</span>
            </article>
            <article>
                <strong>32</strong>
                <span>${getLanguage() === "en" ? "active projects under monitoring" : "projekte aktive ne monitorim"}</span>
            </article>
            <article>
                <strong>73%</strong>
                <span>${getLanguage() === "en" ? "average national KPI" : "KPI mesatar kombetar"}</span>
            </article>
            <article>
                <strong>9</strong>
                <span>${getLanguage() === "en" ? "completed this year" : "te perfunduara kete vit"}</span>
            </article>
            <article>
                <strong>3</strong>
                <span>${getLanguage() === "en" ? "roles with clear accountability" : "role me pergjegjesi te qarta"}</span>
            </article>
        `;
    }

    const resumeCard = document.getElementById("resumeSessionCard");
    const resumeText = document.getElementById("resumeSessionText");
    const resumeButton = document.getElementById("resumeSessionButton");
    const clearButton = document.getElementById("clearSessionButton");

    if (selectedUser && resumeCard && resumeText && resumeButton) {
        try {
            const session = await fetchJson(`/api/auth/session?userId=${encodeURIComponent(selectedUser)}`);
            resumeCard.classList.remove("hidden");
            resumeText.textContent = getLanguage() === "en"
                ? `The last demo session is saved for ${session.fullName} (${roliShqip(session.role)}).`
                : `Sesioni demo i fundit eshte ruajtur per ${session.fullName} (${roliShqip(session.role)}).`;
            resumeButton.addEventListener("click", () => {
                window.location.href = needsMinistryAccess(session.role) ? "/expert/select-ministry" : "/dashboard.html";
            });
            clearButton?.addEventListener("click", () => {
                clearExpertSessionState(selectedUser);
                localStorage.removeItem(storageKey);
                resumeCard.classList.add("hidden");
            });
        } catch {
            clearExpertSessionState(selectedUser);
            localStorage.removeItem(storageKey);
        }
    }

    const backTop = document.getElementById("backToTopButton");
    if (backTop) {
        backTop.textContent = t("backTop");
        window.addEventListener("scroll", () => {
            backTop.classList.toggle("hidden", window.scrollY < 300);
        });
        backTop.addEventListener("click", () => window.scrollTo({ top: 0, behavior: "smooth" }));
    }
}

async function guardExpertRoutes() {
    const path = window.location.pathname.toLowerCase();
    if (!path.startsWith("/expert/")) {
        return;
    }

    const userId = getSelectedUser();
    if (!userId) {
        window.location.href = "/";
        return;
    }

    const session = await fetchJson(`/api/auth/session?userId=${encodeURIComponent(userId)}`);
    if (!needsMinistryAccess(session.role)) {
        window.location.href = "/dashboard.html";
        return;
    }

    const verification = getExpertVerification(userId);
    const isVerified = verification?.verified === true && verification?.ministryId === session.ministryId;
    const isGatePage = path === "/expert/select-ministry";

    if (isGatePage && isVerified) {
        window.location.href = session.role === "Expert" ? "/expert/dashboard" : "/dashboard.html";
        return;
    }

    if (!isGatePage && !isVerified) {
        window.location.href = "/expert/select-ministry";
    }
}

if (document.body.dataset.page === "login") {
    bootLoginPage().catch(() => {
        const container = document.getElementById("demoUsers");
        if (container) {
            container.innerHTML = '<div class="empty-state">Nuk u ngarkuan dot perdoruesit demo.</div>';
        }
    });
}

if (document.body.dataset.page === "landing") {
    bootLandingPage().catch(() => {});
}

setTheme(getTheme());
applyStaticTranslations();
applyLanguageMetadata();

guardExpertRoutes().catch(() => {
    if (window.location.pathname.toLowerCase().startsWith("/expert/")) {
        window.location.href = "/";
    }
});
