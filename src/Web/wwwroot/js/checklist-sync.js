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

    window.checklistProgressSync = {
        exportProgress: exportProgress,
        importProgress: importProgress
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initSync);
    } else {
        initSync();
    }
})();
