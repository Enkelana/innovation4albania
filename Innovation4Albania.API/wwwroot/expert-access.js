let expertAccessState = null;
let lockoutTimerId = null;

function setAccessFeedback(message, isError = false) {
    const banner = document.getElementById("expertAccessFeedback");
    const card = document.getElementById("expertAccessCard");
    banner.textContent = message;
    banner.className = `feedback-banner ${isError ? "error" : "success"}`;

    if (isError) {
        card.classList.remove("shake-card");
        void card.offsetWidth;
        card.classList.add("shake-card");
    }
}

function clearAccessFeedback() {
    const banner = document.getElementById("expertAccessFeedback");
    banner.className = "feedback-banner hidden";
    banner.textContent = "";
}

function setSubmitBusy(isBusy, label) {
    const button = document.getElementById("expertAccessSubmit");
    const labelNode = button.querySelector(".button-label");
    const spinner = button.querySelector(".button-spinner");
    button.disabled = isBusy;
    labelNode.textContent = label;
    spinner.classList.toggle("hidden", !isBusy);
}

function fillMinistryOptions(ministries) {
    const dataList = document.getElementById("ministriesDatalist");
    dataList.innerHTML = ministries.map((ministry) => `<option value="${ministry.name}"></option>`).join("");

    const demoCodes = document.getElementById("demoCodesList");
    demoCodes.innerHTML = ministries.map((ministry) => `
        <article class="demo-code-row">
            <strong>${ministry.name}</strong>
            <span>${ministry.demoCode}</span>
        </article>
    `).join("");
}

function resolveSelectedMinistryId() {
    const typedName = document.getElementById("expertMinistrySearch").value.trim().toLowerCase();
    const selected = expertAccessState?.ministries?.find((item) => item.name.toLowerCase() === typedName);
    document.getElementById("expertMinistryId").value = selected?.id ?? "";
    return selected?.id ?? "";
}

function getExpertAttempts(userId) {
    return Number(sessionStorage.getItem(getExpertAttemptsKey(userId)) || "0");
}

function setExpertAttempts(userId, attempts) {
    sessionStorage.setItem(getExpertAttemptsKey(userId), String(attempts));
}

function setExpertLock(userId, lockedUntilUtc) {
    sessionStorage.setItem(getExpertLockKey(userId), lockedUntilUtc);
}

function getExpertLock(userId) {
    return sessionStorage.getItem(getExpertLockKey(userId));
}

function setVerifiedExpertMinistry(userId, ministryId) {
    sessionStorage.setItem(getExpertVerificationKey(userId), JSON.stringify({
        verified: true,
        ministryId,
        verifiedAt: new Date().toISOString()
    }));
}

function renderLockout(userId) {
    const notice = document.getElementById("lockoutNotice");
    const lockedUntil = getExpertLock(userId);
    if (!lockedUntil) {
        notice.className = "lockout-notice hidden";
        notice.textContent = "";
        if (lockoutTimerId) {
            clearInterval(lockoutTimerId);
            lockoutTimerId = null;
        }
        return false;
    }

    const update = () => {
        const seconds = Math.max(0, Math.ceil((new Date(lockedUntil).getTime() - Date.now()) / 1000));
        if (seconds <= 0) {
            clearExpertSessionState(userId);
            renderLockout(userId);
            clearAccessFeedback();
            setSubmitBusy(false, "Hyr ne Dashboard ->");
            return;
        }

        notice.className = "lockout-notice";
        notice.textContent = `Provoni perseri pas ${seconds} sekondave`;
        document.getElementById("expertAccessSubmit").disabled = true;
    };

    update();
    if (lockoutTimerId) {
        clearInterval(lockoutTimerId);
    }
    lockoutTimerId = setInterval(update, 1000);
    return true;
}

function needsMinistryAccess(role) {
    return role === "Expert" || role === "NucleusDirector";
}

async function bootExpertAccessPage() {
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

    expertAccessState = await fetchJson(`/api/auth/expert-access?userId=${encodeURIComponent(userId)}`);
    fillMinistryOptions(expertAccessState.ministries);

    const assigned = expertAccessState.ministries.find((item) => item.id === expertAccessState.assignedMinistryId);
    if (assigned) {
        document.getElementById("expertMinistrySearch").value = assigned.name;
        document.getElementById("expertMinistryId").value = assigned.id;
    }

    if (renderLockout(userId)) {
        setAccessFeedback("Verifikimi eshte perkohesisht i bllokuar.", true);
    }

    document.getElementById("expertMinistrySearch").addEventListener("input", () => {
        resolveSelectedMinistryId();
        clearAccessFeedback();
    });

    document.getElementById("toggleAccessCodeButton").addEventListener("click", () => {
        const input = document.getElementById("expertAccessCode");
        const hidden = input.type === "password";
        input.type = hidden ? "text" : "password";
        document.getElementById("toggleAccessCodeButton").textContent = hidden ? "Fshih" : "Shfaq";
    });

    document.getElementById("expertAccessForm").addEventListener("submit", async (event) => {
        event.preventDefault();
        clearAccessFeedback();

        if (renderLockout(userId)) {
            return;
        }

        const ministryId = resolveSelectedMinistryId();
        if (!ministryId) {
            setAccessFeedback("Zgjidhni nje ministri te vlefshme nga lista.", true);
            return;
        }

        setSubmitBusy(true, "Po verifikohet...");

        try {
            const result = await postJson("/api/auth/expert-access/verify", {
                userId,
                ministryId,
                accessCode: document.getElementById("expertAccessCode").value
            });

            if (!result.success) {
                const attempts = getExpertAttempts(userId) + 1;
                setExpertAttempts(userId, attempts);
                setAccessFeedback(result.message, true);

                if (attempts >= 3) {
                    setExpertLock(userId, new Date(Date.now() + 60000).toISOString());
                    renderLockout(userId);
                } else {
                    setSubmitBusy(false, "Hyr ne Dashboard ->");
                }

                return;
            }

            clearExpertSessionState(userId);
            setVerifiedExpertMinistry(userId, result.verifiedMinistryId);
            setAccessFeedback("Verifikuar! Duke hapur...", false);
            setSubmitBusy(true, "Verifikuar!");

            setTimeout(() => {
                window.location.href = session.role === "Expert" ? "/expert/dashboard" : "/dashboard.html";
            }, 850);
        } catch {
            setAccessFeedback("Ndodhi nje problem gjate verifikimit.", true);
            setSubmitBusy(false, "Hyr ne Dashboard ->");
        }
    });
}

if (document.body.dataset.page === "expert-access") {
    bootExpertAccessPage().catch(() => {
        setAccessFeedback("Faqja e verifikimit nuk u ngarkua.", true);
    });
}
