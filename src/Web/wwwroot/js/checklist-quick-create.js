document.addEventListener('DOMContentLoaded', () => {
    const textarea = document.getElementById('quick-create-text');
    const lineNumbers = document.getElementById('quick-create-line-numbers');
    const submitBtn = document.getElementById('quick-create-btn');
    const errorBox = document.getElementById('quick-create-error');
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

    function renderLineNumbers() {
        const count = textarea.value.split('\n').length;
        let out = '1';
        for (let i = 2; i <= count; i++) {
            out += '\n' + i;
        }
        lineNumbers.textContent = out;
    }

    function syncScroll() {
        lineNumbers.scrollTop = textarea.scrollTop;
    }

    textarea.addEventListener('input', renderLineNumbers);
    textarea.addEventListener('scroll', syncScroll);
    renderLineNumbers();

    function showError(message) {
        errorBox.textContent = message;
        errorBox.style.display = 'block';
    }

    function clearError() {
        errorBox.textContent = '';
        errorBox.style.display = 'none';
    }

    submitBtn.addEventListener('click', async () => {
        clearError();

        const rawText = textarea.value;
        if (!rawText.trim()) {
            showError('Please paste or type some checklist text first.');
            return;
        }

        submitBtn.disabled = true;
        try {
            const response = await fetch('/checklist/quick-create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': tokenInput.value
                },
                body: JSON.stringify({ rawText })
            });

            if (response.ok) {
                const result = await response.json();
                if (result.success && result.redirectUrl) {
                    window.location.href = result.redirectUrl;
                    return;
                }
                showError('Unexpected response from server.');
                return;
            }

            const message = await response.text();
            showError(message || 'Failed to create checklist. Please check the formatting and try again.');
        } catch (err) {
            showError('Network error. Please check your connection and try again.');
        } finally {
            submitBtn.disabled = false;
        }
    });
});
