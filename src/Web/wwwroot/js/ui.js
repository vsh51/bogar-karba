(function () {
    "use strict";

    function closeAlert(alertEl) {
        if (!alertEl) return;
        alertEl.classList.remove("show");
        setTimeout(function () { alertEl.remove(); }, 200);
    }

    function wireAlertDismiss() {
        document.querySelectorAll(".alert [data-bs-alert='alert'], .alert .btn-close").forEach(function (btn) {
            btn.addEventListener("click", function () {
                closeAlert(btn.closest(".alert"));
            });
        });
    }

    function wireSuccessAutoDismiss() {
        document.querySelectorAll(".alert-success.alert-dismissible").forEach(function (alertEl) {
            setTimeout(function () { closeAlert(alertEl); }, 5000);
        });
    }

    function wireDataActions() {
        document.querySelectorAll("[data-bk-action]").forEach(function (el) {
            var action = el.getAttribute("data-bk-action");
            if (action === "reload") {
                el.addEventListener("click", function () { window.location.reload(); });
            }
        });
    }

    function wireMobileNavClose() {
        var collapseEl = document.querySelector(".bk-navbar .navbar-collapse");
        if (!collapseEl) return;
        collapseEl.querySelectorAll(".nav-link").forEach(function (link) {
            link.addEventListener("click", function () {
                if (window.innerWidth >= 992) return;
                if (collapseEl.classList.contains("show")) {
                    collapseEl.classList.remove("show");
                }
            });
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        wireAlertDismiss();
        wireSuccessAutoDismiss();
        wireDataActions();
        wireMobileNavClose();
    });
})();
