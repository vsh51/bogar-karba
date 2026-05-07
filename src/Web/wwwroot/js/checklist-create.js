document.addEventListener('DOMContentLoaded', () => {
    const editor = document.getElementById('checklist-editor');
    const content = editor.querySelector('.bk-editor-surface');
    const addItemBtn = document.getElementById('add-item-btn');
    const addSectionBtn = document.getElementById('add-section-btn');
    const createBtn = document.getElementById('create-checklist-btn');

    function createRow(type = 'item') {
        const row = document.createElement('div');
        row.className = 'bk-editor-row ' + (type === 'section' ? 'bk-editor-row-section' : '');

        const input = document.createElement('input');
        input.type = 'text';
        
        const deleteBtn = document.createElement('button');
        deleteBtn.type = 'button';
        deleteBtn.className = 'bk-btn-action bk-btn-delete';
        deleteBtn.innerHTML = '<i class="bi bi-x-lg"></i>';

        if (type === 'section') {
            input.className = 'bk-editor-input bk-editor-input-section section-input';
            input.placeholder = 'Section name...';
        } else {
            const checkbox = document.createElement('div');
            checkbox.className = 'bk-editor-checkbox';
            row.appendChild(checkbox);
            input.className = 'bk-editor-input item-input';
            input.placeholder = 'Add a task...';
        }

        row.appendChild(input);

        if (type !== 'section') {
            const linkBtn = document.createElement('button');
            linkBtn.type = 'button';
            linkBtn.className = 'bk-btn-action bk-btn-link';
            linkBtn.title = 'Attach link';
            linkBtn.innerHTML = '<i class="bi bi-link-45deg"></i>';
            row.appendChild(linkBtn);
        }

        row.appendChild(deleteBtn);

        deleteBtn.addEventListener('click', () => {
            const linkRow = row.nextElementSibling;
            if (linkRow && linkRow.classList.contains('bk-link-input-wrapper')) {
                linkRow.remove();
            }
            row.remove();
        });
        return row;
    }

    content.addEventListener('click', (e) => {
        const linkBtn = e.target.closest('.bk-btn-link');
        if (!linkBtn) return;

        const row = linkBtn.closest('.bk-editor-row');
        const existingLinkRow = row.nextElementSibling;

        if (existingLinkRow && existingLinkRow.classList.contains('bk-link-input-wrapper')) {
            const val = existingLinkRow.querySelector('input').value.trim();
            if (!val) {
                existingLinkRow.remove();
                linkBtn.classList.remove('has-link');
            }
            return;
        }

        const linkRow = document.createElement('div');
        linkRow.className = 'bk-link-input-wrapper';
        const linkInput = document.createElement('input');
        linkInput.type = 'url';
        linkInput.className = 'bk-link-input';
        linkInput.placeholder = 'https://...';
        linkRow.appendChild(linkInput);
        row.after(linkRow);
        linkInput.focus();

        linkInput.addEventListener('input', () => {
            linkBtn.classList.toggle('has-link', !!linkInput.value.trim());
        });
    });

    addItemBtn.addEventListener('click', () => {
        content.appendChild(createRow('item'));
    });

    addSectionBtn.addEventListener('click', () => {
        content.appendChild(createRow('section'));
    });

    const addBoredBtn = document.getElementById('add-bored-btn');
    addBoredBtn.addEventListener('click', async () => {
        addBoredBtn.disabled = true;
        const response = await fetch('/checklist/bored-activity');
        addBoredBtn.disabled = false;

        if (!response.ok) return;

        const data = await response.json();
        const row = createRow('item');
        row.querySelector('.item-input').value = data.activity;

        if (data.link) {
            const linkRow = document.createElement('div');
            linkRow.className = 'bk-link-input-wrapper';
            const linkInput = document.createElement('input');
            linkInput.type = 'url';
            linkInput.className = 'bk-link-input';
            linkInput.value = data.link;
            linkRow.appendChild(linkInput);
            row.querySelector('.bk-btn-link').classList.add('has-link');
            content.appendChild(row);
            content.appendChild(linkRow);
        } else {
            content.appendChild(row);
        }
    });

    createBtn.addEventListener('click', async () => {
        const title = editor.querySelector('.editable-title').value.trim();
        const description = editor.querySelector('.editable-desc').value.trim();
        const deadlineInput = editor.querySelector('.editable-deadline');
        const deadline = deadlineInput && deadlineInput.value ? deadlineInput.value : null;

        const sections = [];
        let currentSection = { name: "General", position: 0, tasks: [] };

        const rows = Array.from(content.children);
        for (let i = 0; i < rows.length; i++) {
            const row = rows[i];
            if (row.classList.contains('bk-link-input-wrapper')) continue;

            if (row.classList.contains('bk-editor-row-section')) {
                if (currentSection.tasks.length > 0 || currentSection.name !== "General") {
                    sections.push(currentSection);
                }
                currentSection = {
                    name: row.querySelector('.section-input').value.trim() || "Untitled Section",
                    position: sections.length,
                    tasks: []
                };
            } else {
                const taskInput = row.querySelector('.item-input');
                if (!taskInput) continue;
                
                const taskContent = taskInput.value.trim();
                if (taskContent) {
                    let link = null;
                    const nextRow = rows[i + 1];
                    if (nextRow && nextRow.classList.contains('bk-link-input-wrapper')) {
                        link = nextRow.querySelector('input').value.trim() || null;
                    }
                    currentSection.tasks.push({
                        content: taskContent,
                        position: currentSection.tasks.length,
                        link: link
                    });
                }
            }
        }
        sections.push(currentSection);

        const requestData = {
            title: title || "Untitled Checklist",
            description: description || "",
            deadline: deadline,
            sections: sections
        };

        try {
            const response = await fetch('/checklist/create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(requestData)
            });

            if (response.ok) {
                const result = await response.json();
                if (result.success && result.redirectUrl) {
                    window.location.href = result.redirectUrl;
                }
            } else {
                alert('Failed to create checklist. Please check your input and try again.');
            }
        } catch (error) {
            alert('Network error. Please check your connection and try again.');
        }
    });
});
