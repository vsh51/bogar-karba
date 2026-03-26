document.addEventListener('DOMContentLoaded', () => {
    const editor = document.getElementById('checklist-editor');
    const content = editor.querySelector('.editor-content');
    const addItemBtn = document.getElementById('add-item-btn');
    const addSectionBtn = document.getElementById('add-section-btn');
    const createBtn = document.getElementById('create-checklist-btn');
    const checklistForm = document.getElementById('checklist-form');

    function createRow(type = 'item') {
        const row = document.createElement('div');
        row.className = 'checklist-row ' + (type === 'section' ? 'section-row' : '');
        
        if (type === 'section') {
            row.innerHTML = `
                <span class="drag-handle">::</span>
                <span class="section-prefix">>></span>
                <input type="text" class="section-input" placeholder="Section name..." />
                <button type="button" class="btn-delete">×</button>
            `;
        } else {
            row.innerHTML = `
                <span class="drag-handle">::</span>
                <div class="custom-cb"></div>
                <input type="text" class="item-input" placeholder="Item description..." />
                <button type="button" class="btn-delete">×</button>
            `;
        }

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
        const title = editor.querySelector('.editable-title').innerText.trim();
        const description = editor.querySelector('.editable-desc').innerText.trim();
        
        const sections = [];
        let currentSection = { name: "General", position: 0, tasks: [] };
        
        let globalPosition = 0;
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
            sections: sections
        };

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
            const error = await response.text();
            alert('Failed to create checklist: ' + error);
        }
    });
});
