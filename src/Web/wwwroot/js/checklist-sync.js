(function () {
    "use strict";

    let STORAGE_KEY = "bogarKarba.checklistProgress";
    let SCHEMA_VERSION = 1;

    function exportProgress() {
        let raw = localStorage.getItem(STORAGE_KEY);
        if (!raw) {
            raw = JSON.stringify({ v: SCHEMA_VERSION, checklists: {} });
        }

        let blob = new Blob([raw], { type: "application/json" });
        let url = URL.createObjectURL(blob);
        let a = document.createElement("a");
        a.href = url;

        let dateStr = new Date().toISOString().split('T')[0];
        a.download = `bogarkarba-progress-${dateStr}.json`;

        document.body.appendChild(a);
        a.click();

        setTimeout(function() {
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        }, 100);
    }

    function importProgress(fileContent) {
        let data;
        try {
            data = JSON.parse(fileContent);
        } catch (e) {
            alert("Invalid file format. Please upload a valid JSON file.");
            return false;
        }

        if (data.v !== SCHEMA_VERSION || !data.checklists || typeof data.checklists !== "object") {
            alert("Invalid progress file schema. It might be corrupted or from an incompatible version.");
            return false;
        }


        let currentRaw = localStorage.getItem(STORAGE_KEY);
        let currentData = null;
        if (currentRaw) {
            try {
                currentData = JSON.parse(currentRaw);
            } catch (e) {}
        }

        if (!currentData || typeof currentData !== "object" || !currentData.checklists) {
            currentData = { v: SCHEMA_VERSION, checklists: {} };
        }

        for (let cid in data.checklists) {
            if (Object.prototype.hasOwnProperty.call(data.checklists, cid)) {
                let entry = data.checklists[cid];
                if (entry && Array.isArray(entry.completed)) {
                    currentData.checklists[cid] = entry;
                }
            }
        }

        currentData.v = SCHEMA_VERSION;
        localStorage.setItem(STORAGE_KEY, JSON.stringify(currentData));

        return true;
    }

    function initSync() {
        let exportBtn = document.getElementById("export-progress-btn");
        let importBtn = document.getElementById("import-progress-btn");
        let fileInput = document.getElementById("import-progress-file");

        if (exportBtn) {
            exportBtn.addEventListener("click", function(e) {
                e.preventDefault();
                exportProgress();
            });
        }

        if (importBtn && fileInput) {
            importBtn.addEventListener("click", function(e) {
                e.preventDefault();
                fileInput.click();
            });

            fileInput.addEventListener("change", function(e) {
                if (!e.target.files || e.target.files.length === 0) {
                    return;
                }

                let file = e.target.files[0];
                let reader = new FileReader();

                reader.onload = function(evt) {
                    let content = evt.target.result;
                    let success = importProgress(content);
                    if (success) {
                        alert("Progress imported successfully!");
                        window.location.reload();
                    }
                    fileInput.value = "";
                };

                reader.onerror = function() {
                    alert("Failed to read the file.");
                    fileInput.value = "";
                };

                reader.readAsText(file);
            });
        }
    }

    function copyShareLinkToClipboard(checklistId) {
        let currentRaw = localStorage.getItem(STORAGE_KEY);
        let completedTasks = [];

        if (currentRaw) {
            try {
                let currentData = JSON.parse(currentRaw);
                if (currentData && currentData.checklists && currentData.checklists[checklistId] && Array.isArray(currentData.checklists[checklistId].completed)) {
                    completedTasks = currentData.checklists[checklistId].completed;
                }
            } catch (e) {}
        }

        let baseUrl = window.location.protocol + "//" + window.location.host + "/checklist/" + checklistId;

        if (completedTasks.length > 0) {
            let encodedState = encodeURIComponent(btoa(JSON.stringify(completedTasks)));
            baseUrl += "?p=" + encodedState;
        }

        navigator.clipboard.writeText(baseUrl).then(function() {
            alert("Shareable link copied to clipboard!");
        }).catch(function() {
            alert("Failed to copy link automatically. Here it is:\n\n" + baseUrl);
        });
    }

    function checkForSharedProgress() {
        let params = new URLSearchParams(window.location.search);
        let encodedProgress = params.get('p');

        if (!encodedProgress) {
            return;
        }

        let pathParts = window.location.pathname.split('/');
        let checklistId = null;
        for (let i = 0; i < pathParts.length; i++) {
            if (pathParts[i].toLowerCase() === 'checklist' && (i + 1) < pathParts.length) {
                checklistId = pathParts[i + 1];
                break;
            }
        }

        if (!checklistId) {
            return;
        }

        try {
            let decodedJson = atob(decodeURIComponent(encodedProgress));
            let parsedTasks = JSON.parse(decodedJson);

            if (Array.isArray(parsedTasks)) {
                if (typeof bootstrap === 'undefined') { // test env
                    if (window.checklistProgressSync && window.checklistProgressSync.test_forceReplaceMode) {
                        applySharedProgress(checklistId, parsedTasks, 'replace');
                    } else {
                        applySharedProgress(checklistId, parsedTasks, 'merge');
                    }
                    cleanUrlAndReload();
                    return;
                }

                showImportDialog(
                    function() {
                        applySharedProgress(checklistId, parsedTasks, 'merge');
                        cleanUrlAndReload();
                    },
                    function() {
                        applySharedProgress(checklistId, parsedTasks, 'replace');
                        cleanUrlAndReload();
                    },
                    function() {
                        cleanUrlAndReload();
                    }
                );
            }
        } catch (e) {
            console.error("Failed to parse shared progress:", e);
            cleanUrlAndReload();
        }
    }

    function cleanUrlAndReload() {
        let newUrl = window.location.protocol + "//" + window.location.host + window.location.pathname;
        window.history.replaceState({ path: newUrl }, '', newUrl);
        window.location.reload();
    }

    function applySharedProgress(checklistId, parsedTasks, mode) {
        let currentRaw = localStorage.getItem(STORAGE_KEY);
        let currentData = null;
        if (currentRaw) {
            try { currentData = JSON.parse(currentRaw); } catch (e) {}
        }

        if (!currentData || typeof currentData !== "object" || !currentData.checklists) {
            currentData = { v: SCHEMA_VERSION, checklists: {} };
        }

        if (!currentData.checklists[checklistId]) {
            currentData.checklists[checklistId] = { completed: [] };
        }

        let resultTasks = [];
        if (mode === 'merge') {
            let existingSet = new Set(currentData.checklists[checklistId].completed);
            parsedTasks.forEach(taskId => {
                if (typeof taskId === 'string') {
                    existingSet.add(taskId);
                }
            });
            resultTasks = Array.from(existingSet);
        } else if (mode === 'replace') {
            resultTasks = parsedTasks.filter(taskId => typeof taskId === 'string');
        }

        currentData.checklists[checklistId].completed = resultTasks;
        currentData.v = SCHEMA_VERSION;
        localStorage.setItem(STORAGE_KEY, JSON.stringify(currentData));
    }

    function showImportDialog(onMerge, onReplace, onCancel) {
        let modalHtml = `
        <div class="modal fade" id="sharedProgressModal" tabindex="-1" aria-labelledby="sharedProgressLabel" aria-hidden="true" data-bs-backdrop="static">
          <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content shadow border-0" style="border-radius: 1rem;">
              <div class="modal-header border-bottom-0 pb-0">
                <h4 class="modal-title fw-bold text-dark" id="sharedProgressLabel">
                  <i class="bi bi-cloud-arrow-down text-primary me-2"></i>Import Progress
                </h4>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close" id="btn-cancel-top"></button>
              </div>
              <div class="modal-body text-center pt-3">
                <p class="text-muted mb-4 fs-6">This link contains saved progress for this checklist. How would you like to apply it?</p>
                <div class="d-grid gap-3 px-4">
                    <button type="button" class="btn btn-primary btn-lg rounded-pill shadow-sm" id="btn-merge-progress">
                        <strong>Merge</strong> with my progress
                    </button>
                    <button type="button" class="btn btn-outline-danger shadow-sm rounded-pill" id="btn-replace-progress">
                        <strong>Replace</strong> my progress entirely
                    </button>
                </div>
              </div>
              <div class="modal-footer border-top-0 justify-content-center pt-0 pb-4">
                <button type="button" class="btn btn-link text-muted text-decoration-none" data-bs-dismiss="modal" id="btn-cancel-bottom">Ignore</button>
              </div>
            </div>
          </div>
        </div>
        `;

        let modalContainer = document.createElement("div");
        modalContainer.innerHTML = modalHtml;
        document.body.appendChild(modalContainer);

        let modalElement = document.getElementById('sharedProgressModal');
        let modalInstance = new bootstrap.Modal(modalElement, {
            backdrop: 'static',
            keyboard: false
        });

        let isHandled = false;

        document.getElementById('btn-merge-progress').addEventListener('click', function() {
            isHandled = true;
            modalInstance.hide();
            onMerge();
        });

        document.getElementById('btn-replace-progress').addEventListener('click', function() {
            isHandled = true;
            modalInstance.hide();
            onReplace();
        });

        modalElement.addEventListener('hidden.bs.modal', function () {
            if (!isHandled) {
                onCancel();
            }
            modalContainer.remove();
        });

        modalInstance.show();
    }

    window.checklistProgressSync = {
        exportProgress: exportProgress,
        importProgress: importProgress,
        copyShareLinkToClipboard: copyShareLinkToClipboard,
        test_checkForSharedProgress: checkForSharedProgress
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initSync);
        document.addEventListener("DOMContentLoaded", checkForSharedProgress);
    } else {
        initSync();
        checkForSharedProgress();
    }
})();
