document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('access-modal');
    if (!modal) return;

    const subtitle = document.getElementById('access-modal-subtitle');
    const usernameInput = document.getElementById('access-modal-username');
    const grantBtn = document.getElementById('access-modal-grant-btn');
    const errorEl = document.getElementById('access-modal-error');
    const userList = document.getElementById('access-modal-user-list');

    let currentChecklistId = null;

    const antiForgeryToken = () =>
        document.querySelector('input[name="__RequestVerificationToken"]').value;

    const escapeHtml = (str) =>
        str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

    const showError = (msg) => {
        errorEl.textContent = msg;
        errorEl.hidden = false;
    };

    const clearError = () => {
        errorEl.textContent = '';
        errorEl.hidden = true;
    };

    const renderUsers = (users) => {
        userList.innerHTML = '';
        if (users.length === 0) {
            userList.innerHTML = '<p class="text-muted small mb-0">No users have been granted access yet.</p>';
            return;
        }

        users.forEach(({ userId, userName }) => {
            const item = document.createElement('div');
            item.className = 'd-flex align-items-center justify-content-between py-1 px-2 mb-1 border rounded';
            item.innerHTML = `
                <span class="small">${escapeHtml(userName)}</span>
                <button type="button" class="btn btn-sm btn-link text-danger p-0 lh-1"
                        data-user-id="${escapeHtml(userId)}" title="Revoke access">&times;</button>`;
            item.querySelector('button').addEventListener('click', () => revokeAccess(userId));
            userList.appendChild(item);
        });
    };

    const loadAccessList = async () => {
        if (!currentChecklistId) return;
        const response = await fetch(`/checklist/${currentChecklistId}/access`, { method: 'GET' });
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

        const response = await fetch(`/checklist/${currentChecklistId}/access/grant`, {
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
        const response = await fetch(
            `/checklist/${currentChecklistId}/access/${encodeURIComponent(targetUserId)}/revoke`,
            {
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

    modal.addEventListener('show.bs.modal', (event) => {
        const trigger = event.relatedTarget;
        currentChecklistId = trigger.dataset.checklistId;
        const title = trigger.dataset.checklistTitle;

        subtitle.textContent = `Grant or revoke access to "${title}"`;
        usernameInput.value = '';
        clearError();
        userList.innerHTML = '';

        loadAccessList();
    });

    modal.addEventListener('hidden.bs.modal', () => {
        currentChecklistId = null;
    });

    grantBtn.addEventListener('click', grantAccess);
    usernameInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') grantAccess();
    });
});
