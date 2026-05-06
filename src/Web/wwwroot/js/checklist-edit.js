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
            success: 'alert-success',
            error: 'alert-danger',
        };
        const toast = document.createElement('div');
        toast.className = `alert ${palette[kind] || palette.success} alert-dismissible fade show shadow-sm mb-2`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="bi ${kind === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill'} me-2"></i>
                <div>${message}</div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>`;
        toastStack.appendChild(toast);
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 150);
        }, 3000);
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

    const renderTaskRow = (taskId, content, link) => {
        const row = document.createElement('div');
        row.className = 'bk-editor-row task-row';
        row.dataset.taskId = taskId;
        row.draggable = true;
        row.innerHTML = `
            <div class="bk-drag-handle me-1"><i class="bi bi-grip-vertical"></i></div>
            <div class="form-check mb-0 me-2"><input type="checkbox" class="form-check-input select-task" /></div>
            <input type="text" class="bk-editor-input item-input" value="" placeholder="Task description..." />
            <button type="button" class="bk-btn-action bk-btn-link" title="Attach link"><i class="bi bi-link-45deg"></i></button>
            <button type="button" class="bk-btn-action bk-btn-delete btn-delete-task"><i class="bi bi-x-lg"></i></button>`;
        row.querySelector('.item-input').value = content;
        if (link) {
            row.querySelector('.bk-btn-link').classList.add('has-link');
        }
        return row;
    };

    const renderSection = (sectionId, name) => {
        const container = document.createElement('div');
        container.className = 'section-container';
        container.dataset.sectionId = sectionId;
        container.innerHTML = `
            <div class="bk-editor-row bk-editor-row-section">
                <input type="text" class="bk-editor-input bk-editor-input-section section-input" placeholder="Section name..." />
                <button type="button" class="bk-btn-action bk-btn-delete btn-delete-section"><i class="bi bi-trash3"></i></button>
            </div>
            <div class="tasks-container"></div>
            <div class="bk-editor-row-add p-2 bg-white border-top border-bottom-0">
                <div class="input-group input-group-sm">
                    <input type="text" class="form-control border-0 bg-light add-item-input" placeholder="+ Add item to this section..." />
                    <button class="btn btn-outline-success border-0 btn-add-confirm" type="button">Add</button>
                </div>
            </div>`;
        container.querySelector('.section-input').value = name;
        return container;
    };

    const updateSelectedCount = () => {
        const count = sectionsContainer.querySelectorAll('.select-task:checked').length;
        selectedCountLabel.textContent = `${count} selected`;
        groupBtn.disabled = count === 0;
    };

    sectionsContainer.addEventListener('click', (e) => {
        const linkBtn = e.target.closest('.bk-btn-link');
        if (!linkBtn) return;

        const row = linkBtn.closest('.task-row');
        const next = row.nextElementSibling;

        if (next && next.classList.contains('bk-link-input-wrapper')) {
            const val = next.querySelector('input').value.trim();
            if (!val) {
                next.remove();
                linkBtn.classList.remove('has-link');
            }
            return;
        }

        const linkRow = document.createElement('div');
        linkRow.className = 'bk-link-input-wrapper';
        const linkInput = document.createElement('input');
        linkInput.type = 'url';
        linkInput.className = 'bk-link-input link-url-input';
        linkInput.placeholder = 'https://...';
        linkRow.appendChild(linkInput);
        row.after(linkRow);
        linkInput.focus();

        linkInput.addEventListener('input', () => {
            linkBtn.classList.toggle('has-link', !!linkInput.value.trim());
        });
    });

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

    sectionsContainer.addEventListener('change', (e) => {
        if (e.target.classList.contains('select-task')) {
            updateSelectedCount();
        }
    });

    let dragged = null;
    let draggedLinkRow = null;

    sectionsContainer.addEventListener('dragstart', (e) => {
        const row = e.target.closest('.task-row');
        if (!row) return;
        dragged = row;
        const next = row.nextElementSibling;
        draggedLinkRow = (next && next.classList.contains('bk-link-input-wrapper')) ? next : null;
        row.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', row.dataset.taskId);
    });

    sectionsContainer.addEventListener('dragend', () => {
        if (dragged) dragged.classList.remove('dragging');
        sectionsContainer.querySelectorAll('.drop-above, .drop-below').forEach(el => {
            el.classList.remove('drop-above', 'drop-below');
        });
        dragged = null;
        draggedLinkRow = null;
    });

    sectionsContainer.addEventListener('dragover', (e) => {
        if (!dragged) return;
        const row = e.target.closest('.task-row');
        const tasksContainer = e.target.closest('.tasks-container');
        if (!tasksContainer) return;
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
        if (!dragged) return;
        const tasksContainer = e.target.closest('.tasks-container');
        if (!tasksContainer) return;
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
            if (draggedLinkRow) dragged.after(draggedLinkRow);
        } else {
            tasksContainer.appendChild(dragged);
            if (draggedLinkRow) dragged.after(draggedLinkRow);
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
                    const next = row.nextElementSibling;
                    if (next && next.classList.contains('bk-link-input-wrapper')) {
                        newTasksContainer.appendChild(next);
                    }
                }
            });
            groupNameInput.value = '';
            updateSelectedCount();
            showToast('Items grouped into new section.', 'success');
        } catch (err) {
            showToast('Failed to group items: ' + err.message, 'error');
        }
    });

    sectionsContainer.addEventListener('click', async (e) => {
        const deleteBtn = e.target.closest('.bk-btn-delete');
        if (!deleteBtn) return;

        if (deleteBtn.classList.contains('btn-delete-section')) {
            if (confirm('Delete this section and all its tasks?')) {
                deleteBtn.closest('.section-container').remove();
            }
            return;
        }

        if (deleteBtn.classList.contains('btn-delete-task')) {
            const row = deleteBtn.closest('.task-row');
            const taskId = row.dataset.taskId;
            try {
                await postJson(checklistUrl(`/items/${taskId}/remove`));
                const nextEl = row.nextElementSibling;
                if (nextEl && nextEl.classList.contains('bk-link-input-wrapper')) {
                    nextEl.remove();
                }
                row.remove();
                showToast('Item removed.', 'success');
            } catch (err) {
                showToast('Failed to remove item: ' + err.message, 'error');
            }
        }
    });

    saveBtn.addEventListener('click', async () => {
        const title = editor.querySelector('.editable-title').value.trim();
        const description = editor.querySelector('.editable-desc').value.trim();
        const deadlineInput = editor.querySelector('.editable-deadline');
        const deadline = deadlineInput && deadlineInput.value ? deadlineInput.value : null;

        const sections = Array.from(sectionsContainer.querySelectorAll('.section-container')).map(sc => ({
            id: sc.dataset.sectionId,
            name: sc.querySelector('.section-input').value.trim() || 'Untitled Section',
            tasks: Array.from(sc.querySelectorAll('.task-row')).map(tr => {
                let link = null;
                const nextEl = tr.nextElementSibling;
                if (nextEl && nextEl.classList.contains('bk-link-input-wrapper')) {
                    link = nextEl.querySelector('input').value.trim() || null;
                }
                return {
                    id: tr.dataset.taskId,
                    content: tr.querySelector('.item-input').value.trim(),
                    link: link
                };
            })
        }));

        const requestData = {
            title: title || 'Untitled Checklist',
            description: description || '',
            deadline: deadline,
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
