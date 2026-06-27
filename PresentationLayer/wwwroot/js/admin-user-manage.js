/**
 * admin-user-manage.js
 * EduChatAI Admin Panel — User Management AJAX + Modal orchestration
 *
 * All mutating AJAX calls include the ASP.NET Core anti-forgery token fetched
 * from the hidden input rendered by @Html.AntiForgeryToken().
 */

'use strict';

// ── State ───────────────────────────────────────────────────

const Page = {
    filters: {
        name: "",
        email: "",
        role: ""
    },

    paging: {
        limit: 20,
        offset: 0,
    },

    users: [],
    totalCount: 0
};

// ── Helpers ───────────────────────────────────────────────────

/** Read the anti-forgery token from the DOM (injected by Razor's @Html.AntiForgeryToken()). */
function getAntiForgery() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

/** HTML escaping helper to prevent XSS in dynamic content rendering. */
function escapeHtml(str) {
    if (!str) return '';
    return str
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

/** JS escaping helper to prevent script injection in dynamic content rendering. */
function escapeJs(value) {

    return String(value)
        .replaceAll("\\", "\\\\")
        .replaceAll("'", "\\'")
        .replaceAll('"', '\\"');
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

// ── Intercept form ────────────────────────────────────────────────────

document.addEventListener("DOMContentLoaded", () => {
    document.getElementById("filterForm")
        .addEventListener("submit", async e => {
            e.preventDefault();

            Page.filters.name = document.getElementById("filterName").value;
            Page.filters.email = document.getElementById("filterEmail").value;
            Page.filters.role = document.getElementById("filterRole").value;
            Page.paging.offset = 0;

            await loadUsers();
        });

    document.getElementById("btnReset")
        .addEventListener("click", async e => {
            e.preventDefault();

            Page.filters = {
                name: "",
                email: "",
                role: ""
            };

            filterForm.reset();
            Page.paging.offset = 0;

            await loadUsers();
        });

    document.getElementById("btnPrev").onclick = async () => {
        if (Page.paging.offset == 0) return;
        Page.paging.offset -= Page.paging.limit;
        await loadUsers();
    };

    document.getElementById("btnNext").onclick = async () => {
        Page.paging.offset += Page.paging.limit;
        await loadUsers();
    };
});

// ── RETRIEVE ────────────────────────────────────────────────────

async function loadUsers() {

    const params = new URLSearchParams({
        name: Page.filters.name,
        email: Page.filters.email,
        role: Page.filters.role,
        limit: Page.paging.limit,
        offset: Page.paging.offset
    });

    const res = await fetch(`/admin/user-manage?handler=GetUsers&${params}`);
    if (!res.ok) throw new Error();

    const dto = await res.json();

    Page.users = dto.users;
    Page.totalCount = dto.totalCount;

    renderUsers();
}

// ── Rendering ────────────────────────────────────────────────────

function renderUsers() {

    renderStats();

    renderTable();

    renderPagination();
}

function renderStats() {

    const statNumbers = document.querySelectorAll(".stat-number");
    if (statNumbers.length < 2) return;

    statNumbers[0].textContent = Page.users.length;

    if (Page.totalCount === 0) {
        statNumbers[1].textContent = "0";
        return;
    }

    const start = Page.paging.offset + 1;
    const end = Math.min(Page.paging.offset + Page.users.length, Page.totalCount);

    statNumbers[1].textContent = `${start}–${end}`;

    document.getElementById("totalUserCount").textContent = Page.totalCount;
}

function renderTable() {

    const tbody = document.getElementById("userTable").querySelector("tbody");

    if (Page.users.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="5" class="um-empty">
                    <i class="fas fa-users-slash"></i>
                    <p>No users found matching your filters.</p>
                </td>
            </tr>
        `;
        return;
    }

    tbody.innerHTML = Page.users.map(renderUserRow).join("");
}

function renderUserRow(user) {
    const rowClass = user.isDeleted ? "row-deleted" :
        user.isActive ? "" : "row-disabled";

    const avatar = user.fullName ? user.fullName[0].toUpperCase() : "?";
    const shortId = user.id.substring(0, 8);
    const updatedAt = user.updatedAt;

    let statusHtml;

    if (user.isDeleted) {
        statusHtml = `
<span class="status-badge status-deleted">
    <i class="fas fa-trash"></i>
    Deleted
</span>`;

    }
    else if (!user.isActive) {
        statusHtml = `
<span class="status-badge status-disabled">
    <i class="fas fa-ban"></i>
    Disabled
</span>`;

    }
    else {
        statusHtml = `
<span class="status-badge status-active">
    <i class="fas fa-check-circle"></i>
    Active
</span>`;
    }

    let actions;

    if (user.isDeleted) {
        actions = `<span class="no-actions">—</span>`;
    }
    else {
        actions = `
<button class="action-btn btn-edit"
        title="Edit"
        onclick="openEditModal('${user.id}','${escapeJs(user.fullName)}','${escapeJs(user.email)}','${user.role}','${updatedAt}')">
    <i class="fas fa-pen"></i>
</button>
${user.isActive
                ? `
<button class="action-btn btn-disable"
        title="Disable account"
        onclick="confirmDisable('${user.id}','${escapeJs(user.fullName)}','${updatedAt}')">
    <i class="fas fa-ban"></i>
</button>
`
                : `
<button class="action-btn btn-reactivate"
        title="Reactivate account"
        onclick="confirmReactivate('${user.id}','${escapeJs(user.fullName)}','${updatedAt}')">
    <i class="fas fa-play-circle"></i>
</button>
`
            }
<button class="action-btn btn-delete"
        title="Delete account"
        onclick="confirmDelete('${user.id}','${escapeJs(user.fullName)}')">
    <i class="fas fa-trash-alt"></i>
</button>`;
    }

    return `
<tr class="${rowClass}" data-user-id="${user.id}">
    <td class="col-user">
        <div class="user-cell">
            <div class="user-avatar">${avatar}</div>

            <div class="user-info">
                <span class="user-name">${escapeHtml(user.fullName)}</span>
                <span class="user-id">${shortId}…</span>
            </div>
        </div>
    </td>

    <td class="col-email">
        ${escapeHtml(user.email)}
    </td>

    <td class="col-role">
        <span class="role-badge role-${user.role.toLowerCase()}">
            ${escapeHtml(user.role)}
        </span>
    </td>

    <td class="col-status">
        ${statusHtml}
    </td>

    <td class="col-actions">
        ${actions}
    </td>
</tr>`;
}

function renderPagination() {
    const btnPrev = document.getElementById("btnPrev");
    const btnNext = document.getElementById("btnNext");
    const info = document.querySelector(".pg-info");

    const page = Math.floor(Page.paging.offset / Page.paging.limit) + 1;

    info.textContent = `Page ${page}`;

    btnPrev.disabled = Page.paging.offset === 0;
    btnPrev.classList.toggle("pg-disabled", btnPrev.disabled);

    btnNext.disabled = Page.paging.offset + Page.paging.limit >= Page.totalCount;
    btnNext.classList.toggle("pg-disabled", btnNext.disabled);
}

// ── CREATE ────────────────────────────────────────────────────

function openImportModal() {
    openModal('importModal');
}

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
        const res = await fetch('/admin/user-manage?handler=CreateUser', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery(),
                'CallerConnectionId': connId,
            },
            body: JSON.stringify({ fullName: name, email, password, role })
        });
        const data = await res.json();

        if (data.success) {
            closeModal('createModal');
            showToast('success', `Account created. Login credentials sent to ${email}.`);
            await loadUsers();
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
        const res = await fetch(`/admin/user-manage?handler=UpdateUser&id=${userId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery(),
                'CallerConnectionId': connId,
            },
            body: JSON.stringify({ userId, fullName, email, role, updatedAt })
        });
        const data = await res.json();

        if (data.success) {
            closeModal('editModal');
            showToast('success', 'User updated successfully.');
            await loadUsers();
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
        const res = await fetch(`/admin/user-manage?handler=DeleteUser&id=${userId}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': getAntiForgery(),
                'CallerConnectionId': connId,
            }
        });
        const data = await res.json();

        if (data.success) {
            closeModal('deleteModal');
            showToast('success', 'User deleted. Notification email sent.');
            await loadUsers();
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
        const res = await fetch(`/admin/user-manage?handler=DisableUser&id=${userId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery(),
                'CallerConnectionId': connId,
            },
            body: JSON.stringify({ updatedAt })
        });
        const data = await res.json();

        if (data.success) {
            closeModal('disableModal');
            showToast('success', 'Account disabled. User notified via email.');
            await loadUsers();
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
        const res = await fetch(`/admin/user-manage?handler=ReactivateUser&id=${userId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery(),
                'CallerConnectionId': connId,
            },
            body: JSON.stringify({ updatedAt })
        });
        const data = await res.json();

        if (data.success) {
            closeModal('reactivateModal');
            showToast('success', 'Account reactivated successfully.');
            await loadUsers();
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

// ── SIGNALR EVENT HANDLERS ───────────────────────────────

const resConn =
    new signalR.HubConnectionBuilder()
        .withUrl(`/resource`)
        .withAutomaticReconnect()
        .build();

resConn.on(
    "ResourceChanged",
    async function (resUpd) {
        switch (resUpd.resourceType) {
            case ResourceType.User:
                showToast('info', `${resUpd.resourceName ? "User [" + resUpd.resourceName + "] has" : "Users have"} been updated.`);
                await loadUsers();
                break;
        }
    }
);

resConn
    .start()
    .then(() => resConn.invoke(HubMethod.JoinResourceType, ResourceType.User))
    .then(() => window.connId = resConn.connectionId)
    .then(() =>
        $("<input>")
            .attr("type", "hidden")
            .attr("name", "CallerConnectionId")
            .val(connId)
            .appendTo($("form")))
    .catch(console.error);
