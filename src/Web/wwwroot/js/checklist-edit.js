document.addEventListener('DOMContentLoaded', () => {
    const editor = document.getElementById('checklist-editor');
    const sectionsContainer = document.getElementById('sections-container');
    const saveBtn = document.getElementById('save-checklist-btn');
    const toastStack = document.getElementById('toast-stack');

    const antiForgeryToken = () =>
        document.querySelector('input[name="__RequestVerificationToken"]').value;

    const checklistUrl = (suffix) => `/checklist/${window.checklistId}${suffix}`;

    const showToast = (message, kind = 'success') => {
        const palette = {
            success: 'text-bg-success',
            error: 'text-bg-danger',
        };
        const toast = document.createElement('div');
        toast.className = `toast align-items-center ${palette[kind] || palette.success} border-0 show`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" aria-label="Close"></button>
            </div>`;
        toast.querySelector('.btn-close').addEventListener('click', () => toast.remove());
        toastStack.appendChild(toast);
        setTimeout(() => toast.remove(), 3000);
    };

    const postJson = async (url, body) => {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': antiForgeryToken(),
            },
            body: body === undefined ? undefined : JSON.stringify(body),
        });
        if (!response.ok) {
            const text = await response.text();
            throw new Error(text || `Request failed (${response.status}).`);
        }
        return response.status === 204 ? null : await response.json();
    };

    const renderTaskRow = (taskId, content) => {
        const row = document.createElement('div');
        row.className = 'checklist-row task-row';
        row.dataset.taskId = taskId;
        row.innerHTML = `
            <div class="item-checkbox"></div>
            <input type="text" class="item-input" value="" placeholder="Item description..." />
            <button type="button" class="btn-delete btn-delete-task" title="Remove item">&times;</button>`;
        row.querySelector('.item-input').value = content;
        return row;
    };

    // --- Inline Add Item ---
    const submitAddItem = async (sectionContainer) => {
        const input = sectionContainer.querySelector('.add-item-input');
        const content = input.value.trim();
        if (!content) {
            input.focus();
            return;
        }
        const sectionId = sectionContainer.dataset.sectionId;
        try {
            const result = await postJson(checklistUrl('/items/add'), { sectionId, content });
            const tasksContainer = sectionContainer.querySelector('.tasks-container');
            tasksContainer.appendChild(renderTaskRow(result.id, content));
            input.value = '';
            input.focus();
            showToast('Item added.', 'success');
        } catch (err) {
            showToast('Failed to add item: ' + err.message, 'error');
        }
    };

    sectionsContainer.addEventListener('click', (e) => {
        if (e.target.classList.contains('btn-add-confirm')) {
            submitAddItem(e.target.closest('.section-container'));
        }
    });

    sectionsContainer.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && e.target.classList.contains('add-item-input')) {
            e.preventDefault();
            submitAddItem(e.target.closest('.section-container'));
        }
    });

    // --- Remove item / Delete section ---
    sectionsContainer.addEventListener('click', async (e) => {
        if (e.target.classList.contains('btn-delete-section')) {
            e.target.closest('.section-container').remove();
            return;
        }

        if (e.target.classList.contains('btn-delete-task')) {
            const row = e.target.closest('.task-row');
            const taskId = row.dataset.taskId;
            try {
                await postJson(checklistUrl(`/items/${taskId}/remove`));
                row.remove();
                showToast('Item removed.', 'success');
            } catch (err) {
                showToast('Failed to remove item: ' + err.message, 'error');
            }
        }
    });

    // --- Bulk save (title / description / content edits / section deletions) ---
    saveBtn.addEventListener('click', async () => {
        const title = editor.querySelector('.editable-title').value.trim();
        const description = editor.querySelector('.editable-desc').value.trim();

        const sections = Array.from(sectionsContainer.querySelectorAll('.section-container')).map(sc => ({
            id: sc.dataset.sectionId,
            name: sc.querySelector('.section-input').value.trim() || 'Untitled Section',
            tasks: Array.from(sc.querySelectorAll('.task-row')).map(tr => ({
                id: tr.dataset.taskId,
                content: tr.querySelector('.item-input').value.trim()
            }))
        }));

        const requestData = {
            title: title || 'Untitled Checklist',
            description: description || '',
            sections: sections
        };

        try {
            const response = await fetch(`/checklist/edit/${window.checklistId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiForgeryToken()
                },
                body: JSON.stringify(requestData)
            });

            if (response.ok) {
                const result = await response.json();
                if (result.success && result.redirectUrl) {
                    window.location.href = result.redirectUrl;
                }
            } else {
                const errorText = await response.text();
                showToast('Failed to save: ' + (errorText || 'check your input.'), 'error');
            }
        } catch (error) {
            showToast('Network error — check your connection.', 'error');
        }
    });
});
