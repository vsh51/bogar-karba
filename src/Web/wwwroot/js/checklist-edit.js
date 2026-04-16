document.addEventListener('DOMContentLoaded', () => {
    const editor = document.getElementById('checklist-editor');
    const sectionsContainer = document.getElementById('sections-container');
    const saveBtn = document.getElementById('save-checklist-btn');
    const groupBtn = document.getElementById('group-selected-btn');
    const groupNameInput = document.getElementById('group-section-name');
    const selectedCountLabel = document.getElementById('selected-count');
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
        row.draggable = true;
        row.innerHTML = `
            <span class="drag-handle" title="Drag to reorder">&#8942;&#8942;</span>
            <input type="checkbox" class="select-task" title="Select for grouping" />
            <input type="text" class="item-input" value="" placeholder="Item description..." />
            <button type="button" class="btn-delete btn-delete-task">&times;</button>`;
        row.querySelector('.item-input').value = content;
        return row;
    };

    const renderSection = (sectionId, name) => {
        const container = document.createElement('div');
        container.className = 'section-container';
        container.dataset.sectionId = sectionId;
        container.innerHTML = `
            <div class="checklist-row section-row">
                <input type="text" class="section-input" placeholder="Section name..." />
                <button type="button" class="btn-delete btn-delete-section">&times;</button>
            </div>
            <div class="tasks-container"></div>`;
        container.querySelector('.section-input').value = name;
        return container;
    };

    const updateSelectedCount = () => {
        const count = sectionsContainer.querySelectorAll('.select-task:checked').length;
        selectedCountLabel.textContent = `${count} selected`;
        groupBtn.disabled = count === 0;
    };

    // --- Remove / Section delete ---
    sectionsContainer.addEventListener('click', (e) => {
        if (e.target.classList.contains('btn-delete-section')) {
            e.target.closest('.section-container').remove();
        } else if (e.target.classList.contains('btn-delete-task')) {
            e.target.closest('.task-row').remove();
        }
    });

    // --- Selection tracking ---
    sectionsContainer.addEventListener('change', (e) => {
        if (e.target.classList.contains('select-task')) {
            updateSelectedCount();
        }
    });

    // --- Drag and drop reorder ---
    let dragged = null;

    sectionsContainer.addEventListener('dragstart', (e) => {
        const row = e.target.closest('.task-row');
        if (!row) {
            return;
        }
        dragged = row;
        row.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', row.dataset.taskId);
    });

    sectionsContainer.addEventListener('dragend', () => {
        if (dragged) {
            dragged.classList.remove('dragging');
        }
        sectionsContainer.querySelectorAll('.drop-above, .drop-below').forEach(el => {
            el.classList.remove('drop-above', 'drop-below');
        });
        dragged = null;
    });

    sectionsContainer.addEventListener('dragover', (e) => {
        if (!dragged) {
            return;
        }
        const row = e.target.closest('.task-row');
        const tasksContainer = e.target.closest('.tasks-container');
        if (!tasksContainer) {
            return;
        }
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';

        sectionsContainer.querySelectorAll('.drop-above, .drop-below').forEach(el => {
            el.classList.remove('drop-above', 'drop-below');
        });

        if (row && row !== dragged) {
            const rect = row.getBoundingClientRect();
            const before = e.clientY < rect.top + rect.height / 2;
            row.classList.add(before ? 'drop-above' : 'drop-below');
        }
    });

    sectionsContainer.addEventListener('drop', async (e) => {
        if (!dragged) {
            return;
        }
        const tasksContainer = e.target.closest('.tasks-container');
        if (!tasksContainer) {
            return;
        }
        e.preventDefault();

        const sectionContainer = tasksContainer.closest('.section-container');
        const targetSectionId = sectionContainer.dataset.sectionId;
        const targetRow = e.target.closest('.task-row');

        let newPosition;
        if (targetRow && targetRow !== dragged) {
            const rect = targetRow.getBoundingClientRect();
            const before = e.clientY < rect.top + rect.height / 2;
            const rows = Array.from(tasksContainer.querySelectorAll('.task-row'))
                .filter(r => r !== dragged);
            const targetIndex = rows.indexOf(targetRow);
            newPosition = before ? targetIndex : targetIndex + 1;
            tasksContainer.insertBefore(dragged, before ? targetRow : targetRow.nextSibling);
        } else {
            tasksContainer.appendChild(dragged);
            newPosition = tasksContainer.querySelectorAll('.task-row').length - 1;
        }

        try {
            await postJson(checklistUrl('/items/reorder'), {
                taskId: dragged.dataset.taskId,
                targetSectionId: targetSectionId,
                newPosition: newPosition,
            });
            showToast('Item reordered.', 'success');
        } catch (err) {
            showToast('Failed to reorder: ' + err.message, 'error');
        }
    });

    // --- Group selected tasks into a new section ---
    groupBtn.addEventListener('click', async () => {
        const sectionName = groupNameInput.value.trim();
        if (!sectionName) {
            showToast('Enter a section name first.', 'error');
            groupNameInput.focus();
            return;
        }
        const taskIds = Array.from(sectionsContainer.querySelectorAll('.select-task:checked'))
            .map(cb => cb.closest('.task-row').dataset.taskId);

        if (taskIds.length === 0) {
            showToast('Select at least one item.', 'error');
            return;
        }

        try {
            const result = await postJson(checklistUrl('/sections/group'), { sectionName, taskIds });
            const newSection = renderSection(result.id, sectionName);
            sectionsContainer.appendChild(newSection);
            const newTasksContainer = newSection.querySelector('.tasks-container');
            taskIds.forEach(id => {
                const row = sectionsContainer.querySelector(`.task-row[data-task-id="${id}"]`);
                if (row) {
                    row.querySelector('.select-task').checked = false;
                    newTasksContainer.appendChild(row);
                }
            });
            groupNameInput.value = '';
            updateSelectedCount();
            showToast('Items grouped into new section.', 'success');
        } catch (err) {
            showToast('Failed to group items: ' + err.message, 'error');
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
