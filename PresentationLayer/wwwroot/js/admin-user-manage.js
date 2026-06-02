/**
 * admin-user-manage.js
 * EduChatAI Admin Panel — User Management AJAX + Modal orchestration
 *
 * All mutating AJAX calls include the ASP.NET Core anti-forgery token fetched
 * from the hidden input rendered by @Html.AntiForgeryToken().
 */

'use strict';

// ── Helpers ───────────────────────────────────────────────────

/** Read the anti-forgery token from the DOM (injected by Razor's @Html.AntiForgeryToken()). */
function getAntiForgery() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

/**
 * Open a modal by ID with animation.
 * @param {string} id
 */
function openModal(id) {
    document.getElementById(id)?.classList.add('open');
    document.body.style.overflow = 'hidden';
}

/**
 * Close a modal by ID.
 * @param {string} id
 */
function closeModal(id) {
    document.getElementById(id)?.classList.remove('open');
    document.body.style.overflow = '';
}

/** Close any open modal when clicking on the overlay backdrop. */
document.querySelectorAll('.modal-overlay').forEach(overlay => {
    overlay.addEventListener('click', e => {
        if (e.target === overlay) closeModal(overlay.id);
    });
});

/** Close modal on Escape key. */
document.addEventListener('keydown', e => {
    if (e.key === 'Escape') {
        document.querySelectorAll('.modal-overlay.open')
            .forEach(m => closeModal(m.id));
    }
});

// ── Toast Helper ──────────────────────────────────────────────

/**
 * Show a floating toast notification.
 * @param {'success'|'error'|'info'} type
 * @param {string} message
 */
function showToast(type, message) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const icons = { success: 'fa-check-circle', error: 'fa-exclamation-circle', info: 'fa-info-circle' };
    const toast = document.createElement('div');
    toast.className = `toast-item ${type}`;
    toast.innerHTML = `<i class="fas ${icons[type] ?? icons.info}"></i><span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => { toast.style.opacity = '0'; toast.style.transform = 'translateX(40px)'; }, 3500);
    setTimeout(() => toast.remove(), 3800);
}

// ── Alert inside modal ────────────────────────────────────────

/**
 * Display an inline alert inside a modal.
 * @param {string} alertId - The alert element ID.
 * @param {'error'|'success'} type
 * @param {string} message
 */
function showAlert(alertId, type, message) {
    const el = document.getElementById(alertId);
    if (!el) return;
    el.className = `modal-alert ${type}`;
    el.textContent = message;
    el.hidden = false;
}

function hideAlert(alertId) {
    const el = document.getElementById(alertId);
    if (el) el.hidden = true;
}

// ── Toggle password visibility ────────────────────────────────
function togglePwd(inputId, btn) {
    const input = document.getElementById(inputId);
    if (!input) return;
    const isText = input.type === 'text';
    input.type = isText ? 'password' : 'text';
    btn.innerHTML = `<i class="fas fa-eye${isText ? '' : '-slash'}"></i>`;
}

// ── Loading state ─────────────────────────────────────────────

function setLoading(btnId, loading) {
    const btn = document.getElementById(btnId);
    if (!btn) return;
    btn.classList.toggle('btn-loading', loading);
    btn.disabled = loading;
}

// ── CREATE ────────────────────────────────────────────────────

function openCreateModal() {
    // Clear previous inputs
    ['create-name', 'create-email', 'create-password'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = '';
    });
    const roleEl = document.getElementById('create-role');
    if (roleEl) roleEl.selectedIndex = 0;
    hideAlert('createAlert');
    openModal('createModal');
}

async function submitCreate() {
    const name = document.getElementById('create-name')?.value?.trim();
    const email = document.getElementById('create-email')?.value?.trim();
    const password = document.getElementById('create-password')?.value;
    const role = document.getElementById('create-role')?.value;

    hideAlert('createAlert');

    if (!name || !email || !password || !role) {
        showAlert('createAlert', 'error', 'All fields are required.');
        return;
    }
    if (password.length < 8) {
        showAlert('createAlert', 'error', 'Password must be at least 8 characters.');
        return;
    }

    setLoading('btnCreateSubmit', true);
    try {
        const res = await fetch('/admin/users/create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery()
            },
            body: JSON.stringify({ fullName: name, email, password, role })
        });
        const data = await res.json();

        if (data.success) {
            closeModal('createModal');
            showToast('success', `Verification email sent to ${email}. Account will be active after user verifies.`);
            setTimeout(() => location.reload(), 2500);
        } else {
            showAlert('createAlert', 'error', data.error ?? 'Failed to create user.');
        }
    } catch {
        showAlert('createAlert', 'error', 'Network error. Please try again.');
    } finally {
        setLoading('btnCreateSubmit', false);
    }
}

// ── EDIT ──────────────────────────────────────────────────────

/**
 * Pre-fills and opens the edit modal.
 * @param {string} userId
 * @param {string} fullName
 * @param {string} email
 * @param {string} role
 * @param {string} updatedAt - ISO 8601 string
 */
function openEditModal(userId, fullName, email, role, updatedAt) {
    document.getElementById('edit-user-id').value = userId;
    document.getElementById('edit-name').value = fullName;
    document.getElementById('edit-email').value = email;
    document.getElementById('edit-updated-at').value = updatedAt;

    const roleEl = document.getElementById('edit-role');
    if (roleEl) {
        for (const opt of roleEl.options) {
            opt.selected = opt.value === role;
        }
    }
    hideAlert('editAlert');
    openModal('editModal');
}

async function submitUpdate() {
    const userId = document.getElementById('edit-user-id')?.value;
    const fullName = document.getElementById('edit-name')?.value?.trim();
    const email = document.getElementById('edit-email')?.value?.trim();
    const role = document.getElementById('edit-role')?.value;
    const updatedAt = document.getElementById('edit-updated-at')?.value;

    hideAlert('editAlert');

    if (!fullName || !email || !role) {
        showAlert('editAlert', 'error', 'All fields are required.');
        return;
    }

    setLoading('btnEditSubmit', true);
    try {
        const res = await fetch(`/admin/users/${userId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery()
            },
            body: JSON.stringify({ userId, fullName, email, role, updatedAt })
        });
        const data = await res.json();

        if (data.success) {
            closeModal('editModal');
            showToast('success', 'User updated successfully.');
            setTimeout(() => location.reload(), 1800);
        } else {
            showAlert('editAlert', 'error', data.error ?? 'Failed to update user.');
        }
    } catch {
        showAlert('editAlert', 'error', 'Network error. Please try again.');
    } finally {
        setLoading('btnEditSubmit', false);
    }
}

// ── DELETE ────────────────────────────────────────────────────

function confirmDelete(userId, userName) {
    document.getElementById('delete-user-id').value = userId;
    document.getElementById('delete-user-name').textContent = userName;
    openModal('deleteModal');
}

async function submitDelete() {
    const userId = document.getElementById('delete-user-id')?.value;

    setLoading('btnDeleteSubmit', true);
    try {
        const res = await fetch(`/admin/users/${userId}`, {
            method: 'DELETE',
            headers: { 'RequestVerificationToken': getAntiForgery() }
        });
        const data = await res.json();

        if (data.success) {
            closeModal('deleteModal');
            showToast('success', 'User deleted. Notification email sent.');
            setTimeout(() => location.reload(), 1800);
        } else {
            closeModal('deleteModal');
            showToast('error', data.error ?? 'Failed to delete user.');
        }
    } catch {
        closeModal('deleteModal');
        showToast('error', 'Network error. Please try again.');
    } finally {
        setLoading('btnDeleteSubmit', false);
    }
}

// ── DISABLE ───────────────────────────────────────────────────

function confirmDisable(userId, userName, updatedAt) {
    document.getElementById('disable-user-id').value = userId;
    document.getElementById('disable-user-name').textContent = userName;
    document.getElementById('disable-updated-at').value = updatedAt;
    openModal('disableModal');
}

async function submitDisable() {
    const userId = document.getElementById('disable-user-id')?.value;
    const updatedAt = document.getElementById('disable-updated-at')?.value;

    setLoading('btnDisableSubmit', true);
    try {
        const res = await fetch(`/admin/users/${userId}/disable`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery()
            },
            body: JSON.stringify({ updatedAt })
        });
        const data = await res.json();

        if (data.success) {
            closeModal('disableModal');
            showToast('success', 'Account disabled. User notified via email.');
            setTimeout(() => location.reload(), 1800);
        } else {
            closeModal('disableModal');
            showToast('error', data.error ?? 'Failed to disable account.');
        }
    } catch {
        closeModal('disableModal');
        showToast('error', 'Network error. Please try again.');
    } finally {
        setLoading('btnDisableSubmit', false);
    }
}

// ── REACTIVATE ────────────────────────────────────────────────

function confirmReactivate(userId, userName, updatedAt) {
    document.getElementById('reactivate-user-id').value = userId;
    document.getElementById('reactivate-user-name').textContent = userName;
    document.getElementById('reactivate-updated-at').value = updatedAt;
    openModal('reactivateModal');
}

async function submitReactivate() {
    const userId = document.getElementById('reactivate-user-id')?.value;
    const updatedAt = document.getElementById('reactivate-updated-at')?.value;

    setLoading('btnReactivateSubmit', true);
    try {
        const res = await fetch(`/admin/users/${userId}/reactivate`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery()
            },
            body: JSON.stringify({ updatedAt })
        });
        const data = await res.json();

        if (data.success) {
            closeModal('reactivateModal');
            showToast('success', 'Account reactivated successfully.');
            setTimeout(() => location.reload(), 1800);
        } else {
            closeModal('reactivateModal');
            showToast('error', data.error ?? 'Failed to reactivate account.');
        }
    } catch {
        closeModal('reactivateModal');
        showToast('error', 'Network error. Please try again.');
    } finally {
        setLoading('btnReactivateSubmit', false);
    }
}
