(function () {
    "use strict";

    const bkUi = window.bkUi = window.bkUi || {};

    bkUi.confirm = function (options) {
        options = options || {};
        const modalEl = document.getElementById("bk-confirm-modal");
        if (!modalEl || !window.bootstrap) {
            return Promise.resolve(window.confirm(options.message || ""));
        }

        const titleEl = modalEl.querySelector("#bk-confirm-title");
        const messageEl = modalEl.querySelector("#bk-confirm-message");
        const okBtn = modalEl.querySelector("#bk-confirm-ok-btn");

        titleEl.textContent = options.title || "Confirm";
        messageEl.textContent = options.message || "";
        okBtn.textContent = options.confirmLabel || "Confirm";
        okBtn.className = "btn " + (options.danger ? "btn-danger" : "btn-primary");

        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

        return new Promise(function (resolve) {
            let confirmed = false;

            function onOk() {
                confirmed = true;
                modal.hide();
            }

            function onHidden() {
                okBtn.removeEventListener("click", onOk);
                modalEl.removeEventListener("hidden.bs.modal", onHidden);
                resolve(confirmed);
            }

            okBtn.addEventListener("click", onOk);
            modalEl.addEventListener("hidden.bs.modal", onHidden);
            modal.show();
        });
    };

    bkUi.toast = function (message, kind) {
        const stack = document.getElementById("bk-toast-stack");
        if (!stack) {
            window.alert(message);
            return;
        }
        const item = document.createElement("div");
        item.className = "bk-toast bk-toast--" + (kind || "success");
        item.setAttribute("role", kind === "error" ? "alert" : "status");
        item.textContent = message;
        stack.appendChild(item);
        setTimeout(function () {
            item.classList.add("is-leaving");
            setTimeout(function () { item.remove(); }, 200);
        }, 3500);
    };

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
