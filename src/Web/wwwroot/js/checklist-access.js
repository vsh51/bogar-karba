document.addEventListener('DOMContentLoaded', () => {
    const panel = document.getElementById('access-management-panel');
    if (!panel) return;

    const checklistId = panel.dataset.checklistId;
    const antiForgeryToken = () =>
        document.querySelector('input[name="__RequestVerificationToken"]').value;

    const userListEl = document.getElementById('access-user-list');
    const usernameInput = document.getElementById('access-username-input');
    const grantBtn = document.getElementById('access-grant-btn');
    const errorEl = document.getElementById('access-error');

    const showError = (msg) => {
        errorEl.textContent = msg;
        errorEl.hidden = false;
    };

    const clearError = () => {
        errorEl.textContent = '';
        errorEl.hidden = true;
    };

    const renderUsers = (users) => {
        userListEl.innerHTML = '';
        if (users.length === 0) {
            const empty = document.createElement('p');
            empty.className = 'access-empty';
            empty.textContent = 'No users have been granted access yet.';
            userListEl.appendChild(empty);
            return;
        }

        users.forEach(({ userId, userName }) => {
            const item = document.createElement('div');
            item.className = 'access-user-item';
            item.innerHTML = `
                <span class="access-username">${escapeHtml(userName)}</span>
                <button type="button" class="access-revoke-btn" data-user-id="${escapeHtml(userId)}" title="Revoke access">&times;</button>`;
            userListEl.appendChild(item);
        });

        userListEl.querySelectorAll('.access-revoke-btn').forEach(btn => {
            btn.addEventListener('click', () => revokeAccess(btn.dataset.userId));
        });
    };

    const escapeHtml = (str) =>
        str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

    const loadAccessList = async () => {
        const response = await fetch(`/checklist/${checklistId}/access`, { method: 'GET' });
        if (!response.ok) return;
        const users = await response.json();
        renderUsers(users);
    };

    const grantAccess = async () => {
        const username = usernameInput.value.trim();
        if (!username) {
            showError('Please enter a username.');
            return;
        }

        clearError();
        grantBtn.disabled = true;

        const response = await fetch(`/checklist/${checklistId}/access/grant`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': antiForgeryToken(),
            },
            body: JSON.stringify({ username }),
        });

        grantBtn.disabled = false;

        if (!response.ok) {
            const text = await response.text();
            showError(text || 'Failed to grant access.');
            return;
        }

        usernameInput.value = '';
        await loadAccessList();
    };

    const revokeAccess = async (targetUserId) => {
        const response = await fetch(`/checklist/${checklistId}/access/${encodeURIComponent(targetUserId)}/revoke`, {
            method: 'POST',
            headers: { 'RequestVerificationToken': antiForgeryToken() },
        });

        if (!response.ok) {
            const text = await response.text();
            showError(text || 'Failed to revoke access.');
            return;
        }

        await loadAccessList();
    };

    grantBtn.addEventListener('click', grantAccess);
    usernameInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') grantAccess();
    });

    loadAccessList();
});
