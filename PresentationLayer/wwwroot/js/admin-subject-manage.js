/**
 * admin-subject-manage.js
 * EduChatAI Admin Panel — Subject, Chapter & Membership AJAX + Modal orchestration
 */

'use strict';

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

/** Open a modal by ID with animation. */
function openModal(id) {
    document.getElementById(id)?.classList.add('open');
    document.body.style.overflow = 'hidden';
}

/** Close a modal by ID. */
function closeModal(id) {
    document.getElementById(id)?.classList.remove('open');
    document.body.style.overflow = '';
}

// Bind close event on backdrop click for all modals
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.modal-overlay').forEach(overlay => {
        overlay.addEventListener('click', e => {
            if (e.target === overlay) closeModal(overlay.id);
        });
    });
});

// Close modal on Escape key
document.addEventListener('keydown', e => {
    if (e.key === 'Escape') {
        document.querySelectorAll('.modal-overlay.open')
            .forEach(m => closeModal(m.id));
    }
});

// Hide autocomplete suggestions when clicking outside
document.addEventListener('click', e => {
    const suggestions = document.getElementById('searchSuggestions');
    const searchInput = document.getElementById('member-search-input');
    if (suggestions && searchInput && e.target !== searchInput && !suggestions.contains(e.target)) {
        suggestions.hidden = true;
    }
});

// ── Toast Helper ──────────────────────────────────────────────

/** Show a floating toast notification. */
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

/** Display an inline alert inside a modal. */
function showAlert(alertId, type, message) {
    const el = document.getElementById(alertId);
    if (!el) return;
    el.className = `modal-alert ${type}`;
    el.textContent = message;
    el.hidden = false;
}

// Hide alert inside modal
function hideAlert(alertId) {
    const el = document.getElementById(alertId);
    if (el) el.hidden = true;
}

// ── Loading state ─────────────────────────────────────────────

function setLoading(btnId, loading) {
    const btn = document.getElementById(btnId);
    if (!btn) return;
    btn.classList.toggle('btn-loading', loading);
    btn.disabled = loading;
}

// ── SUBJECTS CRUD ─────────────────────────────────────────────

function openCreateSubjectModal() {
    document.getElementById('create-sub-code').value = '';
    document.getElementById('create-sub-name').value = '';
    document.getElementById('create-sub-desc').value = '';
    hideAlert('createSubjectAlert');
    openModal('createSubjectModal');
}

async function submitCreateSubject() {
    const code = document.getElementById('create-sub-code')?.value?.trim();
    const name = document.getElementById('create-sub-name')?.value?.trim();
    const desc = document.getElementById('create-sub-desc')?.value?.trim();

    hideAlert('createSubjectAlert');

    if (!code || !name) {
        showAlert('createSubjectAlert', 'error', 'Subject code and subject name are required.');
        return;
    }

    setLoading('btnCreateSubjectSubmit', true);
    try {
        const res = await fetch('/admin/subjects/create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery()
            },
            body: JSON.stringify({ subjectCode: code, subjectName: name, description: desc })
        });
        const data = await res.json();

        if (data.success) {
            closeModal('createSubjectModal');
            showToast('success', 'Subject created successfully.');
            setTimeout(() => location.reload(), 1500);
        } else {
            showAlert('createSubjectAlert', 'error', data.error ?? 'Failed to create subject.');
        }
    } catch {
        showAlert('createSubjectAlert', 'error', 'Network error. Please try again.');
    } finally {
        setLoading('btnCreateSubjectSubmit', false);
    }
}

function openEditSubjectModal(id, code, name, description) {
    document.getElementById('edit-sub-id').value = id;
    document.getElementById('edit-sub-code').value = code;
    document.getElementById('edit-sub-name').value = name;
    document.getElementById('edit-sub-desc').value = description || '';
    hideAlert('editSubjectAlert');
    openModal('editSubjectModal');
}

async function submitUpdateSubject() {
    const id = document.getElementById('edit-sub-id')?.value;
    const code = document.getElementById('edit-sub-code')?.value?.trim();
    const name = document.getElementById('edit-sub-name')?.value?.trim();
    const desc = document.getElementById('edit-sub-desc')?.value?.trim();

    hideAlert('editSubjectAlert');

    if (!code || !name) {
        showAlert('editSubjectAlert', 'error', 'Subject code and subject name are required.');
        return;
    }

    setLoading('btnUpdateSubjectSubmit', true);
    try {
        const res = await fetch(`/admin/subjects/update/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery()
            },
            body: JSON.stringify({ id, subjectCode: code, subjectName: name, description: desc })
        });
        const data = await res.json();

        if (data.success) {
            closeModal('editSubjectModal');
            showToast('success', 'Subject updated successfully.');
            setTimeout(() => location.reload(), 1500);
        } else {
            showAlert('editSubjectAlert', 'error', data.error ?? 'Failed to update subject.');
        }
    } catch {
        showAlert('editSubjectAlert', 'error', 'Network error. Please try again.');
    } finally {
        setLoading('btnUpdateSubjectSubmit', false);
    }
}

function confirmDeleteSubject(id, name) {
    document.getElementById('delete-sub-id').value = id;
    document.getElementById('delete-sub-name').textContent = name;
    openModal('deleteSubjectModal');
}

async function submitDeleteSubject() {
    const id = document.getElementById('delete-sub-id')?.value;

    setLoading('btnDeleteSubjectSubmit', true);
    try {
        const res = await fetch(`/admin/subjects/delete/${id}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': getAntiForgery()
            }
        });
        const data = await res.json();

        if (data.success) {
            closeModal('deleteSubjectModal');
            showToast('success', 'Subject deleted successfully.');
            setTimeout(() => location.reload(), 1500);
        } else {
            closeModal('deleteSubjectModal');
            showToast('error', data.error ?? 'Failed to delete subject.');
        }
    } catch {
        closeModal('deleteSubjectModal');
        showToast('error', 'Network error. Please try again.');
    } finally {
        setLoading('btnDeleteSubjectSubmit', false);
    }
}

// ── CHAPTERS CRUD ─────────────────────────────────────────────

async function openChapterModal(subjectId, subjectName) {
    document.getElementById('chapter-sub-id').value = subjectId;
    document.getElementById('chapter-subject-name').textContent = subjectName;
    document.getElementById('new-chapter-num').value = '';
    document.getElementById('new-chapter-name').value = '';
    hideAlert('chapterAlert');
    
    await loadChapters(subjectId);
    openModal('chapterModal');
}

async function loadChapters(subjectId) {
    const listEl = document.getElementById('chapterList');
    if (!listEl) return;

    listEl.innerHTML = '<div style="text-align: center; padding: 20px; color: #6b7280;"><i class="fas fa-spinner fa-spin"></i> Loading chapters list...</div>';

    try {
        const res = await fetch(`/admin/subjects/${subjectId}/chapters`);
        if (!res.ok) throw new Error('Failed to fetch chapters');
        const chapters = await res.json();

        if (chapters.length === 0) {
            listEl.innerHTML = '<div style="text-align: center; padding: 20px; color: #9ca3af; font-style: italic;">No chapters in this subject yet.</div>';
            return;
        }

        // Sort chapters by ChapterNumber
        chapters.sort((a, b) => a.chapterNumber - b.chapterNumber);

        listEl.innerHTML = chapters.map(c => `
            <div class="chapter-item" data-chapter-id="${c.id}">
                <div class="chapter-details">
                    <span class="chapter-num-badge">Chapter ${c.chapterNumber}</span>
                    <span class="chapter-name-text">${escapeHtml(c.chapterName)}</span>
                </div>
                <div class="chapter-item-actions">
                    <button class="chapter-action-btn edit-btn" onclick="startEditChapter('${c.id}', ${c.chapterNumber}, '${escapeHtml(c.chapterName)}')" title="Edit chapter">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="chapter-action-btn delete-btn" onclick="deleteChapter('${c.id}')" title="Delete chapter">
                        <i class="fas fa-trash-alt"></i>
                    </button>
                </div>
            </div>
        `).join('');
    } catch {
        listEl.innerHTML = '<div style="text-align: center; padding: 20px; color: #ef4444;"><i class="fas fa-exclamation-triangle"></i> Failed to load chapters.</div>';
    }
}

async function submitCreateChapter() {
    const subjectId = document.getElementById('chapter-sub-id').value;
    const numInput = document.getElementById('new-chapter-num');
    const nameInput = document.getElementById('new-chapter-name');
    const number = parseInt(numInput.value);
    const name = nameInput.value.trim();

    hideAlert('chapterAlert');

    if (isNaN(number) || number <= 0) {
        showAlert('chapterAlert', 'error', 'Chapter number must be a positive integer.');
        return;
    }
    if (!name) {
        showAlert('chapterAlert', 'error', 'Chapter name is required.');
        return;
    }

    setLoading('btnCreateChapter', true);
    try {
        const res = await fetch('/admin/chapters/create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery()
            },
            body: JSON.stringify({ subjectId, chapterName: name, chapterNumber: number })
        });
        const data = await res.json();

        if (data.success) {
            numInput.value = '';
            nameInput.value = '';
            showToast('success', 'Chapter added successfully.');
            await loadChapters(subjectId);
        } else {
            showAlert('chapterAlert', 'error', data.error ?? 'Failed to add chapter.');
        }
    } catch {
        showAlert('chapterAlert', 'error', 'Network error. Please try again.');
    } finally {
        setLoading('btnCreateChapter', false);
    }
}

function startEditChapter(id, currentNum, currentName) {
    const row = document.querySelector(`.chapter-item[data-chapter-id="${id}"]`);
    if (!row) return;

    row.innerHTML = `
        <div class="chapter-edit-row" style="display: flex; align-items: center; width: 100%;">
            <input type="number" class="form-control edit-num-input" value="${currentNum}" style="width: 70px; margin-right: 8px;" min="1" />
            <input type="text" class="form-control edit-name-input" value="${escapeHtml(currentName)}" style="flex: 1; margin-right: 8px;" />
            <button class="chapter-action-btn save-btn" onclick="saveEditChapter('${id}')" style="color: #10b981; font-size: 16px; margin-right: 8px;" title="Save"><i class="fas fa-check"></i></button>
            <button class="chapter-action-btn cancel-btn" onclick="cancelEditChapter('${id}', ${currentNum}, '${escapeHtml(currentName)}')" style="color: #ef4444; font-size: 16px;" title="Cancel"><i class="fas fa-times"></i></button>
        </div>
    `;
}

function cancelEditChapter(id, num, name) {
    const subjectId = document.getElementById('chapter-sub-id').value;
    loadChapters(subjectId);
}

async function saveEditChapter(id) {
    const row = document.querySelector(`.chapter-item[data-chapter-id="${id}"]`);
    if (!row) return;

    const numInput = row.querySelector('.edit-num-input');
    const nameInput = row.querySelector('.edit-name-input');
    const number = parseInt(numInput.value);
    const name = nameInput.value.trim();
    const subjectId = document.getElementById('chapter-sub-id').value;

    hideAlert('chapterAlert');

    if (isNaN(number) || number <= 0) {
        showAlert('chapterAlert', 'error', 'Chapter number must be a positive integer.');
        return;
    }
    if (!name) {
        showAlert('chapterAlert', 'error', 'Chapter name is required.');
        return;
    }

    try {
        const res = await fetch(`/admin/chapters/update/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery()
            },
            body: JSON.stringify({ id, chapterName: name, chapterNumber: number })
        });
        const data = await res.json();

        if (data.success) {
            showToast('success', 'Chapter updated successfully.');
            await loadChapters(subjectId);
        } else {
            showAlert('chapterAlert', 'error', data.error ?? 'Failed to update chapter.');
        }
    } catch {
        showAlert('chapterAlert', 'error', 'Network error. Please try again.');
    }
}

async function deleteChapter(id) {
    if (!confirm('Are you sure you want to delete this chapter?')) return;
    const subjectId = document.getElementById('chapter-sub-id').value;

    try {
        const res = await fetch(`/admin/chapters/delete/${id}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': getAntiForgery()
            }
        });
        const data = await res.json();

        if (data.success) {
            showToast('success', 'Chapter deleted successfully.');
            await loadChapters(subjectId);
        } else {
            showToast('error', data.error ?? 'Failed to delete chapter.');
        }
    } catch {
        showToast('error', 'Network error. Please try again.');
    }
}

// ── MEMBERS MANAGEMENT ────────────────────────────────────────

async function openMemberModal(subjectId, subjectName) {
    document.getElementById('member-sub-id').value = subjectId;
    document.getElementById('member-subject-name').textContent = subjectName;
    
    // Clear assign form
    const searchInput = document.getElementById('member-search-input');
    if (searchInput) {
        searchInput.value = '';
        searchInput.removeAttribute('data-selected-user-id');
    }
    document.getElementById('member-role-select').selectedIndex = 0;
    document.getElementById('searchSuggestions').hidden = true;
    hideAlert('memberAlert');

    await loadMembers(subjectId);
    openModal('memberModal');
}

async function loadMembers(subjectId) {
    const listEl = document.getElementById('memberList');
    if (!listEl) return;

    listEl.innerHTML = '<div style="text-align: center; padding: 20px; color: #6b7280;"><i class="fas fa-spinner fa-spin"></i> Loading members list...</div>';

    try {
        const res = await fetch(`/admin/subjects/${subjectId}/members`);
        if (!res.ok) throw new Error('Failed to fetch members');
        const members = await res.json();

        if (members.length === 0) {
            listEl.innerHTML = '<div style="text-align: center; padding: 20px; color: #9ca3af; font-style: italic;">No members assigned yet.</div>';
            return;
        }

        // Sort: Chief -> Lecturer -> Student
        const roleWeights = { Chief: 1, Lecturer: 2, Student: 3 };
        members.sort((a, b) => (roleWeights[a.role] || 99) - (roleWeights[b.role] || 99));

        listEl.innerHTML = members.map(m => {
            let badgeClass = 'badge-student';
            let roleText = 'Student';
            if (m.role === 'Chief') {
                badgeClass = 'badge-chief';
                roleText = 'Subject-Lead';
            } else if (m.role === 'Lecturer') {
                badgeClass = 'badge-lecturer';
                roleText = 'Lecturer';
            }

            return `
                <div class="member-item">
                    <div class="member-info">
                        <span class="member-name">${escapeHtml(m.fullName)}</span>
                        <span class="member-email">${escapeHtml(m.email)}</span>
                    </div>
                    <span class="member-role-badge ${badgeClass}">${roleText}</span>
                    <button class="member-action-btn delete-btn" onclick="removeMember('${m.userId}')" title="Remove from subject">
                        <i class="fas fa-user-minus"></i>
                    </button>
                </div>
            `;
        }).join('');
    } catch {
        listEl.innerHTML = '<div style="text-align: center; padding: 20px; color: #ef4444;"><i class="fas fa-exclamation-triangle"></i> Failed to load members.</div>';
    }
}

async function submitAssignMember() {
    const subjectId = document.getElementById('member-sub-id').value;
    const searchInput = document.getElementById('member-search-input');
    const userId = searchInput?.getAttribute('data-selected-user-id');
    const role = document.getElementById('member-role-select').value;

    hideAlert('memberAlert');

    if (!userId) {
        showAlert('memberAlert', 'error', 'Please select a user from the search suggestions.');
        return;
    }

    setLoading('btnAssignMember', true);
    try {
        const res = await fetch(`/admin/subjects/${subjectId}/members/assign`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery()
            },
            body: JSON.stringify({ userId, role })
        });
        const data = await res.json();

        if (data.success) {
            searchInput.value = '';
            searchInput.removeAttribute('data-selected-user-id');
            showToast('success', 'Member assigned successfully.');
            await loadMembers(subjectId);
        } else {
            showAlert('memberAlert', 'error', data.error ?? 'Failed to assign member.');
        }
    } catch {
        showAlert('memberAlert', 'error', 'Network error. Please try again.');
    } finally {
        setLoading('btnAssignMember', false);
    }
}

async function removeMember(userId) {
    if (!confirm('Are you sure you want to remove this member from the subject?')) return;
    const subjectId = document.getElementById('member-sub-id').value;

    try {
        const res = await fetch(`/admin/subjects/${subjectId}/members/remove/${userId}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': getAntiForgery()
            }
        });
        const data = await res.json();

        if (data.success) {
            showToast('success', 'Member removed from subject.');
            await loadMembers(subjectId);
        } else {
            showToast('error', data.error ?? 'Failed to remove member.');
        }
    } catch {
        showToast('error', 'Network error. Please try again.');
    }
}

// ── AUTOCOMPLETE EVENT HANDLERS ───────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    let searchDebounceTimeout = null;
    const searchInput = document.getElementById('member-search-input');
    const suggestionsEl = document.getElementById('searchSuggestions');

    if (searchInput && suggestionsEl) {
        searchInput.addEventListener('input', function() {
            clearTimeout(searchDebounceTimeout);
            const search = this.value.trim();
            const subjectId = document.getElementById('member-sub-id').value;
            const role = document.getElementById('member-role-select').value;

            if (search.length < 2) {
                suggestionsEl.innerHTML = '';
                suggestionsEl.hidden = true;
                this.removeAttribute('data-selected-user-id');
                return;
            }

            searchDebounceTimeout = setTimeout(async () => {
                try {
                    const res = await fetch(`/admin/subjects/${subjectId}/eligible-users?role=${role}&search=${encodeURIComponent(search)}`);
                    if (!res.ok) throw new Error('Search failed');
                    const users = await res.json();
                    
                    if (users.length === 0) {
                        suggestionsEl.innerHTML = '<div class="autocomplete-item empty">No matching users found</div>';
                        suggestionsEl.hidden = false;
                        return;
                    }

                    suggestionsEl.innerHTML = users.map(u => `
                        <div class="autocomplete-item" data-user-id="${u.userId}" data-user-name="${escapeHtml(u.fullName)}" data-user-email="${escapeHtml(u.email)}">
                            <strong>${escapeHtml(u.fullName)}</strong> (${escapeHtml(u.email)})
                        </div>
                    `).join('');
                    suggestionsEl.hidden = false;

                    // Click event for suggestions
                    suggestionsEl.querySelectorAll('.autocomplete-item').forEach(item => {
                        if (item.classList.contains('empty')) return;
                        item.addEventListener('click', function() {
                            searchInput.value = `${this.getAttribute('data-user-name')} (${this.getAttribute('data-user-email')})`;
                            searchInput.setAttribute('data-selected-user-id', this.getAttribute('data-user-id'));
                            suggestionsEl.hidden = true;
                        });
                    });
                } catch (err) {
                    console.error('Autocomplete error:', err);
                }
            }, 300);
        });

        // Clear selection and results when changing roles to avoid mismatched lists
        const roleSelect = document.getElementById('member-role-select');
        if (roleSelect) {
            roleSelect.addEventListener('change', () => {
                searchInput.value = '';
                searchInput.removeAttribute('data-selected-user-id');
                suggestionsEl.innerHTML = '';
                suggestionsEl.hidden = true;
            });
        }
    }
});
