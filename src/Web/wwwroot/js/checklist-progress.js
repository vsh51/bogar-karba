// {
//     "v": 1,
//      "checklists": {
//         "<checklistId>": { "completed": ["<taskId>", ...] }
//     }
// }

(function () {
    "use strict";

    let STORAGE_KEY = "bogarKarba.checklistProgress";
    let SCHEMA_VERSION = 1;

    function freshRoot() {
        return {
            v: SCHEMA_VERSION,
            checklists: {}
        };
    }

    function normalizeId(id) {
        return String(id);
    }

    function dedupeTaskIds(taskIds) {
        let seen = Object.create(null);
        let out = [];
        for (let i = 0; i < taskIds.length; i++) {
            let t = normalizeId(taskIds[i]);
            if (!seen[t]) {
                seen[t] = true;
                out.push(t);
            }
        }
        return out;
    }

    function normalizeChecklistsObject(checklists) {
        let out = Object.create(null);
        for (let cid in checklists) {
            if (!Object.prototype.hasOwnProperty.call(checklists, cid)) {
                continue;
            }

            let entry = checklists[cid];
            if (!entry || typeof entry !== "object") {
                continue;
            }

            if (!Array.isArray(entry.completed)) {
                continue;
            }

            let done = dedupeTaskIds(entry.completed);
            if (done.length > 0) {
                out[normalizeId(cid)] = { completed: done };
            }
        }
        return out;
    }

    function parseJsonObject(raw) {
        if (typeof raw !== "string" || raw === "") {
            return null;
        }

        let first = raw.charAt(0);
        let last = raw.charAt(raw.length - 1);
        if (!((first === "{" && last === "}") || (first === "[" && last === "]"))) {
            return null;
        }

        let parsed = JSON.parse(raw);
        return parsed && typeof parsed === "object" ? parsed : null;
    }

    function loadRoot() {
        let raw = localStorage.getItem(STORAGE_KEY);
        if (raw === null || raw === "") {
            return freshRoot();
        }

        let data = parseJsonObject(raw);
        if (!data) {
            return freshRoot();
        }

        if (
            data.v === SCHEMA_VERSION && data.checklists &&
            typeof data.checklists === "object"
        ) {
            return {
                v: SCHEMA_VERSION,
                checklists: normalizeChecklistsObject(data.checklists)
            };
        }

        return freshRoot();
    }

    function parseInitialCompletedTaskIds(page) {
        if (!page) {
            return [];
        }

        let raw = page.getAttribute("data-initial-completed-task-ids");
        if (!raw) {
            return [];
        }

        let parsed = parseJsonObject(raw);
        if (!Array.isArray(parsed)) {
            return [];
        }

        return dedupeTaskIds(parsed);
    }

    function persistRoot(root) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(root));
    }

    function getChecklistProgress(checklistId) {
        let cid = normalizeId(checklistId);
        let root = loadRoot();
        let entry = root.checklists[cid];
        if (!entry || !Array.isArray(entry.completed)) {
            return [];
        }
        return dedupeTaskIds(entry.completed);
    }

    function saveChecklistProgress(checklistId, completedTaskIds) {
        let cid = normalizeId(checklistId);
        let completed = dedupeTaskIds(completedTaskIds || []);
        let root = loadRoot();

        if (!root.checklists) {
            root.checklists = Object.create(null);
        }

        if (completed.length === 0) {
            delete root.checklists[cid];
        } else {
            root.checklists[cid] = { completed: completed };
        }

        root.v = SCHEMA_VERSION;
        persistRoot(root);
        return completed;
    }

    function toggleTask(checklistId, taskId) {
        let tid = normalizeId(taskId);
        let current = getChecklistProgress(checklistId);
        let idx = current.indexOf(tid);

        if (idx >= 0) {
            current = current.slice(0, idx).concat(current.slice(idx + 1));
        } else {
            current = current.concat([tid]);
        }

        return saveChecklistProgress(checklistId, current);
    }

    function renderChecklistProgress(page) {
        if (!page) {
            return;
        }

        let inputs = page.querySelectorAll(
            "input.checklist-item-input[data-task-id]");
        let total = inputs.length;
        let completed = 0;

        for (let i = 0; i < total; i++) {
            if (inputs[i].checked) {
                completed++;
            }
        }

        let percent = total === 0 ? 0 : Math.round((completed / total) * 100);
        let percentEl = page.querySelector("[data-progress-percent]");
        let countEl = page.querySelector("[data-progress-count]");
        let barEl = page.querySelector("[data-progress-bar]");
        let barWrapEl = page.querySelector("[data-progress-bar-wrap]");

        if (percentEl) {
            percentEl.textContent = percent + "%";
        }

        if (countEl) {
            countEl.textContent = completed + "/" + total;
        }

        if (barEl) {
            barEl.style.width = percent + "%";
        }

        if (barWrapEl) {
            barWrapEl.setAttribute("aria-valuenow", String(percent));
        }
    }

    function getServerProgressUrl(page) {
        if (!page) {
            return "";
        }

        let url = page.getAttribute("data-server-progress-url");
        return typeof url === "string" ? url : "";
    }

    function createServerSync(saveUrl, checklistId) {
        if (!saveUrl) {
            return function () {};
        }

        let pendingTimeoutId = null;

        return function () {
            if (pendingTimeoutId !== null) {
                window.clearTimeout(pendingTimeoutId);
            }

            pendingTimeoutId = window.setTimeout(function () {
                pendingTimeoutId = null;

                let completedTaskIds = getChecklistProgress(checklistId);
                fetch(saveUrl, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    credentials: "same-origin",
                    body: JSON.stringify({ completedTaskIds: completedTaskIds })
                }).catch(function () {
                    // Keep local progress even if server sync fails.
                });
            }, 250);
        };
    }

    function initChecklistPage() {
        let page = document.querySelector(".checklist-page[data-checklist-id]");
        if (!page) {
            return;
        }

        let checklistId = page.getAttribute("data-checklist-id");
        if (!checklistId) {
            return;
        }

        let serverProgressUrl = getServerProgressUrl(page);
        let syncProgressToServer = createServerSync(serverProgressUrl, checklistId);

        if (serverProgressUrl) {
            let initialCompleted = parseInitialCompletedTaskIds(page);
            saveChecklistProgress(checklistId, initialCompleted);
        }

        let completed = getChecklistProgress(checklistId);
        let doneSet = Object.create(null);
        for (let d = 0; d < completed.length; d++) {
            doneSet[completed[d]] = true;
        }

        let inputs = page.querySelectorAll(
            "input.checklist-item-input[data-task-id]");
        for (let i = 0; i < inputs.length; i++) {
            let input = inputs[i];
            let taskId = input.getAttribute("data-task-id");
            if (!taskId) {
                continue;
            }
            input.checked = !!doneSet[taskId]; // I love !!
            input.addEventListener("change", function (el, tid) {
                return function () {
                    toggleTask(checklistId, tid);
                    let list = getChecklistProgress(checklistId);
                    el.checked = list.indexOf(tid) >= 0;
                    renderChecklistProgress(page);
                    syncProgressToServer();
                };
            }(input, taskId));
        }

        let clearBtn = document.getElementById("clear-progress-btn");
        if (clearBtn) {
            clearBtn.addEventListener("click", function () {
                if (!confirm("Clear all progress for this checklist?")) {
                    return;
                }

                saveChecklistProgress(checklistId, []);
                for (let i = 0; i < inputs.length; i++) {
                    inputs[i].checked = false;
                }
                renderChecklistProgress(page);
                syncProgressToServer();
            });
        }

        renderChecklistProgress(page);
    }

    window.getChecklistProgress = getChecklistProgress;
    window.saveChecklistProgress = saveChecklistProgress;
    window.toggleTask = toggleTask;

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initChecklistPage);
    } else {
        initChecklistPage();
    }
})();
