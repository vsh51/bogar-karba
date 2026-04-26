(function () {
    "use strict";

    let STORAGE_KEY_PREFIX = "bogarKarba.checklistTimer.";

    function nowMs() {
        return Date.now();
    }

    function normalizeChecklistId(checklistId) {
        return String(checklistId || "");
    }

    function getStorageKey(checklistId) {
        return STORAGE_KEY_PREFIX + normalizeChecklistId(checklistId);
    }

    function normalizeEntry(entry) {
        if (!entry || typeof entry !== "object") {
            return { elapsedMs: 0, isRunning: false, startedAtMs: null };
        }

        let elapsed = Number.isFinite(entry.elapsedMs) ? entry.elapsedMs : 0;
        if (elapsed < 0) {
            elapsed = 0;
        }

        let isRunning = entry.isRunning === true;
        let startedAtMs = Number.isFinite(entry.startedAtMs) ? entry.startedAtMs : null;

        if (!isRunning) {
            startedAtMs = null;
        }

        return {
            elapsedMs: Math.floor(elapsed),
            isRunning: isRunning,
            startedAtMs: startedAtMs
        };
    }

    function serializeEntry(entry) {
        let normalized = normalizeEntry(entry);
        let startedAt = normalized.startedAtMs === null ? "" : String(normalized.startedAtMs);
        return String(normalized.elapsedMs) + ";" + (normalized.isRunning ? "1" : "0") + ";" + startedAt;
    }

    function deserializeEntry(raw) {
        if (!raw || typeof raw !== "string") {
            return { elapsedMs: 0, isRunning: false, startedAtMs: null };
        }

        let parts = raw.split(";");
        if (parts.length !== 3) {
            return { elapsedMs: 0, isRunning: false, startedAtMs: null };
        }

        let elapsedMs = Number(parts[0]);
        let isRunning = parts[1] === "1";
        let startedAtMs = parts[2] === "" ? null : Number(parts[2]);

        return normalizeEntry({
            elapsedMs: elapsedMs,
            isRunning: isRunning,
            startedAtMs: startedAtMs
        });
    }

    function getEntry(checklistId) {
        let raw = localStorage.getItem(getStorageKey(checklistId));
        return deserializeEntry(raw);
    }

    function setEntry(checklistId, entry) {
        let normalized = normalizeEntry(entry);
        localStorage.setItem(getStorageKey(checklistId), serializeEntry(normalized));
        return normalized;
    }

    function formatElapsed(ms) {
        let totalSeconds = Math.floor(Math.max(0, ms) / 1000);
        let hours = Math.floor(totalSeconds / 3600);
        let minutes = Math.floor((totalSeconds % 3600) / 60);
        let seconds = totalSeconds % 60;

        return String(hours).padStart(2, "0") + ":"
            + String(minutes).padStart(2, "0") + ":"
            + String(seconds).padStart(2, "0");
    }

    function getEffectiveElapsedMs(entry) {
        if (!entry.isRunning || !Number.isFinite(entry.startedAtMs)) {
            return entry.elapsedMs;
        }

        let activePart = nowMs() - entry.startedAtMs;
        if (!Number.isFinite(activePart) || activePart < 0) {
            activePart = 0;
        }

        return entry.elapsedMs + activePart;
    }

    function initChecklistTimer() {
        let page = document.querySelector(".checklist-page[data-checklist-id]");
        if (!page) {
            return;
        }

        let checklistId = page.getAttribute("data-checklist-id");
        if (!checklistId) {
            return;
        }

        let display = page.querySelector("[data-timer-display]");
        let stateEl = page.querySelector("[data-timer-state]");
        let startBtn = page.querySelector("[data-timer-start]");
        let stopBtn = page.querySelector("[data-timer-stop]");
        let restartBtn = page.querySelector("[data-timer-restart]");

        if (!display || !stateEl || !startBtn || !stopBtn || !restartBtn) {
            return;
        }

        let intervalId = null;
        let state = getEntry(checklistId);

        function render() {
            let elapsed = getEffectiveElapsedMs(state);
            display.textContent = formatElapsed(elapsed);
            stateEl.textContent = state.isRunning ? "Running" : "Stopped";

            startBtn.disabled = state.isRunning;
            stopBtn.disabled = !state.isRunning;
        }

        function startTicking() {
            if (intervalId !== null) {
                return;
            }

            intervalId = window.setInterval(function () {
                render();
            }, 250);
        }

        function stopTicking() {
            if (intervalId === null) {
                return;
            }

            window.clearInterval(intervalId);
            intervalId = null;
        }

        function startTimer() {
            if (state.isRunning) {
                return;
            }

            state = {
                elapsedMs: Math.max(0, state.elapsedMs),
                isRunning: true,
                startedAtMs: nowMs()
            };
            setEntry(checklistId, state);
            startTicking();
            render();
        }

        function stopTimer() {
            if (!state.isRunning) {
                return;
            }

            let elapsed = getEffectiveElapsedMs(state);
            state = {
                elapsedMs: elapsed,
                isRunning: false,
                startedAtMs: null
            };
            setEntry(checklistId, state);
            stopTicking();
            render();
        }

        function restartTimer() {
            state = {
                elapsedMs: 0,
                isRunning: false,
                startedAtMs: null
            };
            setEntry(checklistId, state);
            stopTicking();
            render();
        }

        startBtn.addEventListener("click", startTimer);
        stopBtn.addEventListener("click", stopTimer);
        restartBtn.addEventListener("click", restartTimer);

        if (state.isRunning) {
            startTicking();
        }

        render();

        window.addEventListener("beforeunload", function () {
            if (!state.isRunning) {
                return;
            }

            setEntry(checklistId, state);
        });
    }

    window.checklistTimer = {
        test_formatElapsed: formatElapsed,
        test_getEntry: getEntry,
        test_setEntry: setEntry
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initChecklistTimer);
    } else {
        initChecklistTimer();
    }
})();
