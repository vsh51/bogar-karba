(function () {
    "use strict";

    const STORAGE_KEY = "bogarKarba.checklistProgress";

    async function init() {
        const container = document.getElementById("progress-checklists-container");
        if (!container) return;

        const rawData = localStorage.getItem(STORAGE_KEY);
        if (!rawData) return;

        let data;
        try {
            data = JSON.parse(rawData);
        } catch (e) {
            return;
        }

        if (!data || !data.checklists) return;

        const ids = Object.keys(data.checklists);
        if (ids.length === 0) return;

        try {
            const response = await fetch('/checklist/get-by-ids', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(ids)
            });

            if (!response.ok) return;

            const checklists = await response.json();
            if (checklists.length > 0) {
                render(container, checklists, data.checklists);
            }
        } catch (error) {
            console.error("Error loading progress:", error);
        }
    }

    function render(container, checklists, progressData) {
        let html = `
            <div class="row">
                <div class="col-12">
                    <h2 class="h4 mb-4 border-bottom pb-2">Your Progress</h2>
                </div>
            </div>
            <div class="row row-cols-1 row-cols-md-3 g-4 mb-5">
        `;

        checklists.forEach(item => {
            const title = escapeHtml(item.title);
            const desc = escapeHtml(item.description || "");
            const url = `/checklist/${item.id}`;
            
            html += `
                <div class="col">
                    <div class="card h-100 shadow-sm border-primary">
                        <div class="card-body d-flex flex-column">
                            <h5 class="card-title">${title}</h5>
                            <p class="card-text text-muted small flex-grow-1">${desc}</p>
                            <div class="d-grid mt-3">
                                <a href="${url}" class="btn btn-primary">Continue</a>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        });

        html += `</div>`;
        container.innerHTML = html;
    }

    function escapeHtml(str) {
        const p = document.createElement('p');
        p.textContent = str;
        return p.innerHTML;
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
