(function () {
    "use strict";

    var FEEDBACK_DURATION_MS = 2000;

    function getCompletedTaskIds(checklistId) {
        if (typeof window.getChecklistProgress === "function") {
            return window.getChecklistProgress(checklistId);
        }
        return [];
    }

    function showCopyFeedback(button, success) {
        var originalText = button.textContent;
        button.textContent = success ? "Copied!" : "Failed to copy";
        button.disabled = true;

        setTimeout(function () {
            button.textContent = originalText;
            button.disabled = false;
        }, FEEDBACK_DURATION_MS);
    }

    function handleCopyMarkdown(event) {
        event.preventDefault();

        var button = event.currentTarget;
        var page = document.querySelector(".checklist-page[data-checklist-id]");
        if (!page) {
            return;
        }

        var checklistId = page.getAttribute("data-checklist-id");
        if (!checklistId) {
            return;
        }

        var completedTaskIds = getCompletedTaskIds(checklistId);

        fetch("/checklist/" + checklistId + "/export/markdown", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ completedTaskIds: completedTaskIds })
        })
        .then(function (response) {
            if (!response.ok) {
                throw new Error("Export failed");
            }
            return response.json();
        })
        .then(function (data) {
            return navigator.clipboard.writeText(data.content);
        })
        .then(function () {
            showCopyFeedback(button, true);
        })
        .catch(function () {
            showCopyFeedback(button, false);
        });
    }

    function init() {
        var btn = document.getElementById("copy-md-btn");
        if (btn) {
            btn.addEventListener("click", handleCopyMarkdown);
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
