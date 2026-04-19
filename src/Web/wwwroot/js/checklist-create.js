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
        row.appendChild(deleteBtn);

        row.querySelector('.btn-delete').addEventListener('click', () => row.remove());
        return row;
    }

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
        
        Array.from(content.children).forEach((row, index) => {
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
                    currentSection.tasks.push({
                        content: taskContent,
                        position: currentSection.tasks.length
                    });
                }
            }
        });
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
