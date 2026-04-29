(function () {
    "use strict";

    var FEEDBACK_DURATION_MS = 2000;

    function showCopyFeedback(button, success) {
        var originalText = button.textContent;
        button.textContent = success ? "Copied!" : "Failed to copy";
        button.disabled = true;

        setTimeout(function () {
            button.textContent = originalText;
            button.disabled = false;
        }, FEEDBACK_DURATION_MS);
    }

    function openEmbedModal(event) {
        event.preventDefault();

        var modalElement = document.getElementById("embedIframeModal");
        if (!modalElement || typeof bootstrap === "undefined") {
            return;
        }

        var modalInstance = bootstrap.Modal.getOrCreateInstance(modalElement);
        modalInstance.show();
    }

    function copyEmbedSnippet(event) {
        event.preventDefault();

        var button = event.currentTarget;
        var textarea = document.getElementById("embed-iframe-snippet");
        if (!textarea) {
            return;
        }

        var snippet = textarea.value;

        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(snippet)
                .then(function () { showCopyFeedback(button, true); })
                .catch(function () { showCopyFeedback(button, false); });
            return;
        }

        try {
            textarea.select();
            var ok = document.execCommand && document.execCommand("copy");
            showCopyFeedback(button, !!ok);
        } catch (e) {
            showCopyFeedback(button, false);
        }
    }

    function init() {
        var openBtn = document.getElementById("export-iframe-btn");
        if (openBtn) {
            openBtn.addEventListener("click", openEmbedModal);
        }

        var copyBtn = document.getElementById("embed-iframe-copy-btn");
        if (copyBtn) {
            copyBtn.addEventListener("click", copyEmbedSnippet);
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
