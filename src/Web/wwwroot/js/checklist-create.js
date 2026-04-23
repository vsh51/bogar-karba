document.addEventListener('DOMContentLoaded', () => {
    const editor = document.getElementById('checklist-editor');
    const content = editor.querySelector('.editor-content');
    const addItemBtn = document.getElementById('add-item-btn');
    const addSectionBtn = document.getElementById('add-section-btn');
    const createBtn = document.getElementById('create-checklist-btn');

    function createRow(type = 'item') {
        const row = document.createElement('div');
        row.className = 'checklist-row ' + (type === 'section' ? 'section-row' : '');

        const input = document.createElement('input');
        input.type = 'text';
        const deleteBtn = document.createElement('button');
        deleteBtn.type = 'button';
        deleteBtn.className = 'btn-delete';
        deleteBtn.textContent = '\u00D7';

        if (type === 'section') {
            input.className = 'section-input';
            input.placeholder = 'Section name...';
        } else {
            const checkbox = document.createElement('div');
            checkbox.className = 'item-checkbox';
            row.appendChild(checkbox);
            input.className = 'item-input';
            input.placeholder = 'Item description...';
        }

        row.appendChild(input);

        if (type !== 'section') {
            const linkBtn = document.createElement('button');
            linkBtn.type = 'button';
            linkBtn.className = 'btn-link-toggle';
            linkBtn.title = 'Attach link';
            linkBtn.innerHTML = '&#128279;';
            row.appendChild(linkBtn);
        }

        row.appendChild(deleteBtn);

        deleteBtn.addEventListener('click', () => {
            const linkRow = row.nextElementSibling;
            if (linkRow && linkRow.classList.contains('link-input-row')) {
                linkRow.remove();
            }
            row.remove();
        });
        return row;
    }

    content.addEventListener('click', (e) => {
        if (!e.target.classList.contains('btn-link-toggle')) return;
        const row = e.target.closest('.checklist-row');
        const existingLinkRow = row.nextElementSibling;

        if (existingLinkRow && existingLinkRow.classList.contains('link-input-row')) {
            const val = existingLinkRow.querySelector('input').value.trim();
            if (!val) {
                existingLinkRow.remove();
                e.target.classList.remove('has-link');
            }
            return;
        }

        const linkRow = document.createElement('div');
        linkRow.className = 'link-input-row';
        const linkInput = document.createElement('input');
        linkInput.type = 'url';
        linkInput.className = 'link-url-input';
        linkInput.placeholder = 'https://...';
        linkRow.appendChild(linkInput);
        row.after(linkRow);
        linkInput.focus();

        linkInput.addEventListener('input', () => {
            e.target.classList.toggle('has-link', !!linkInput.value.trim());
        });
    });

    addItemBtn.addEventListener('click', () => {
        content.appendChild(createRow('item'));
    });

    addSectionBtn.addEventListener('click', () => {
        content.appendChild(createRow('section'));
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
            if (row.classList.contains('link-input-row')) continue;

            if (row.classList.contains('section-row')) {
                if (currentSection.tasks.length > 0 || currentSection.name !== "General") {
                    sections.push(currentSection);
                }
                currentSection = {
                    name: row.querySelector('.section-input').value.trim() || "Untitled Section",
                    position: sections.length,
                    tasks: []
                };
            } else {
                const taskContent = row.querySelector('.item-input').value.trim();
                if (taskContent) {
                    let link = null;
                    const nextRow = rows[i + 1];
                    if (nextRow && nextRow.classList.contains('link-input-row')) {
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
