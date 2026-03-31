document.addEventListener('DOMContentLoaded', () => {
    const editor = document.getElementById('checklist-editor');
    const sectionsContainer = document.getElementById('sections-container');
    const saveBtn = document.getElementById('save-checklist-btn');

    sectionsContainer.addEventListener('click', (e) => {
        if (e.target.classList.contains('btn-delete-section')) {
            e.target.closest('.section-container').remove();
        } else if (e.target.classList.contains('btn-delete-task')) {
            e.target.closest('.task-row').remove();
        }
    });

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
                const errorText = await response.text();
                alert('Failed to save changes: ' + (errorText || 'Please check your input and try again.'));
            }
        } catch (error) {
            alert('Network error. Please check your connection and try again.');
        }
    });
});
