/**
 * admin-ai-configuration.js
 * EduChatAI Admin Panel — Subject AI Configuration Page AJAX & UI orchestration
 */

'use strict';

// ── Global State ──────────────────────────────────────────────
let globalOptions = null;
let currentConfig = null;
let selectedSubjectId = null;

// ── DOM Elements ──────────────────────────────────────────────
const subjectSelect = document.getElementById('subjectSelect');
const configFormContainer = document.getElementById('configFormContainer');
const aiConfigForm = document.getElementById('aiConfigForm');
const btnSaveConfig = document.getElementById('btnSaveConfig');
const btnReindex = document.getElementById('btnReindex');
const alertContainer = document.getElementById('alertContainer');
const reindexConfirmModal = document.getElementById('reindexConfirmModal');
const viewPromptModal = document.getElementById('viewPromptModal');

// ── Page Initialization ───────────────────────────────────────
document.addEventListener('DOMContentLoaded', async () => {
    setupModalEventHandlers();
    await loadInitialData();
});

function setupModalEventHandlers() {
    // Backdrop click close
    document.querySelectorAll('.modal-overlay').forEach(overlay => {
        overlay.addEventListener('click', e => {
            if (e.target === overlay) closeModal(overlay.id);
        });
    });

    // Close on Escape key
    document.addEventListener('keydown', e => {
        if (e.key === 'Escape') {
            document.querySelectorAll('.modal-overlay.open').forEach(m => closeModal(m.id));
        }
    });

    // Subject select change
    subjectSelect.addEventListener('change', async (e) => {
        selectedSubjectId = parseInt(e.target.value, 10);
        if (selectedSubjectId) {
            await loadSubjectConfiguration(selectedSubjectId);
        } else {
            configFormContainer.style.display = 'none';
        }
    });
}

// ── Helpers ───────────────────────────────────────────────────
function getAntiForgery() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

function escapeHtml(value) {

    if (value === null || value === undefined)
        return "";

    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#39;");
}

function openModal(id) {
    document.getElementById(id)?.classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeModal(id) {
    document.getElementById(id)?.classList.remove('open');
    document.body.style.overflow = '';
}

function showAlert(type, message) {
    alertContainer.className = `alert-container alert-${type}`;
    // FIXME: Accept content + icon type (enum|string); Escape HTML in content
    alertContainer.innerHTML = message;
    alertContainer.hidden = false;
    // Scroll to alert
    alertContainer.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

function hideAlert() {
    alertContainer.hidden = true;
}

function showModalAlert(modalId, type, message) {
    const modal = document.getElementById(modalId);
    let alertEl = modal?.querySelector('.modal-alert');
    if (!alertEl) {
        alertEl = document.createElement('div');
        alertEl.className = 'modal-alert';
        modal?.querySelector('.modal-body')?.appendChild(alertEl);
    }
    alertEl.className = `modal-alert alert-${type}`;
    alertEl.textContent = message;
    alertEl.style.marginTop = '10px';
    alertEl.style.padding = '8px 12px';
    alertEl.style.borderRadius = '6px';
    alertEl.style.fontSize = '12px';
    alertEl.style.fontWeight = '500';
    if (type === 'danger') {
        alertEl.style.background = '#fef2f2';
        alertEl.style.color = '#991b1b';
        alertEl.style.border = '1px solid #fecaca';
    } else {
        alertEl.style.background = '#f0fdf4';
        alertEl.style.color = '#166534';
        alertEl.style.border = '1px solid #bbf7d0';
    }
    alertEl.hidden = false;
}

function hideModalAlert(modalId) {
    const alertEl = document.getElementById(modalId)?.querySelector('.modal-alert');
    if (alertEl) {
        alertEl.style.display = 'none';
    }
}

// ── Fetch Operations ──────────────────────────────────────────
async function loadInitialData() {
    try {
        // Load Options first
        const optionsRes = await fetch('/admin/ai-configuration?handler=Options');
        if (!optionsRes.ok) throw new Error('Failed to load configuration options.');
        globalOptions = await optionsRes.json();

        // Populate dropdowns
        populateDropdown('input-ChunkingStrategy', globalOptions.chunkingStrategies);
        populateDropdown('input-EmbeddingModel', globalOptions.embeddingModels);
        populateDropdown('input-LlmModel', globalOptions.chatModels);

        // Load Subjects
        const subjectsRes = await fetch('/admin/ai-configuration?handler=Subjects');
        if (!subjectsRes.ok) throw new Error('Failed to load subjects.');
        const subjects = await subjectsRes.json();

        subjectSelect.innerHTML = '<option value="" disabled selected>-- Choose a Subject --</option>';
        subjects.forEach(sub => {
            const opt = document.createElement('option');
            opt.value = sub.subjectId;
            opt.textContent = `${sub.subjectCode} - ${sub.subjectName}`;
            subjectSelect.appendChild(opt);
        });
    } catch (err) {
        showAlert('danger', `<i class="fas fa-exclamation-circle"></i> Error initializing page: ${err.message}`);
    }
}

function populateDropdown(selectId, options) {
    const select = document.getElementById(selectId);
    if (!select) return;
    select.innerHTML = '';
    options.forEach(opt => {
        const o = document.createElement('option');
        o.value = opt.value;
        o.textContent = opt.label;
        select.appendChild(o);
    });
}

async function loadSubjectConfiguration(subjectId) {
    hideAlert();
    try {
        btnSaveConfig.disabled = true;
        btnReindex.disabled = true;

        const res = await fetch(`/admin/ai-configuration?handler=Configuration&subjectId=${subjectId}`);
        if (!res.ok) throw new Error('Failed to load subject AI configuration.');

        currentConfig = await res.json();
        renderConfiguration(currentConfig);

        configFormContainer.style.display = 'block';
        btnSaveConfig.disabled = false;
        btnReindex.disabled = false;
    } catch (err) {
        showAlert('danger', `<i class="fas fa-exclamation-circle"></i> Error loading configuration: ${err.message}`);
    }
}

// ── Rendering Configuration ───────────────────────────────────
function renderConfiguration(config) {
    // List of flat settings we map
    const mappings = [
        { key: 'ChunkingStrategy', path: config.indexing.chunkingStrategy, type: 'select' },
        { key: 'ChunkSize', path: config.indexing.chunkSize, type: 'number' },
        { key: 'ChunkOverlap', path: config.indexing.chunkOverlap, type: 'number' },
        { key: 'EmbeddingModel', path: config.indexing.embeddingModel, type: 'select' },

        { key: 'TopK', path: config.retrieval.topK, type: 'number' },
        { key: 'SimilarityThreshold', path: config.retrieval.similarityThreshold, type: 'number_float' },
        { key: 'MaxContextChunks', path: config.retrieval.maxContextChunks, type: 'number' },

        { key: 'LlmModel', path: config.generation.llmModel, type: 'select' },
        { key: 'ChatTemperature', path: config.generation.chatTemperature, type: 'number_float' },
        { key: 'MaxHistoryMessages', path: config.generation.maxHistoryMessages, type: 'number' },
        { key: 'TitleTemperature', path: config.generation.titleTemperature, type: 'number_float' },
        { key: 'CitationExtractionTemperature', path: config.generation.citationExtractionTemperature, type: 'number_float' },

        { key: 'ChatPrompt', path: config.prompts.chatPrompt, type: 'textarea' },
        { key: 'ContextPrompt', path: config.prompts.contextPrompt, type: 'textarea' },
        { key: 'NoContextRetrievedPrompt', path: config.prompts.noContextRetrievedPrompt, type: 'textarea' },
        { key: 'TitlePrompt', path: config.prompts.titlePrompt, type: 'textarea' },
        { key: 'CitationExtractionPrompt', path: config.prompts.citationExtractionPrompt, type: 'textarea' }
    ];

    mappings.forEach(m => {
        const { key, path, type } = m;
        const defVal = path.globalDefault;
        const storeVal = path.storedOverride;
        const effVal = path.effectiveValue;

        // Render default badge
        const defBadge = document.getElementById(`def-${key}`);
        if (defBadge) {
            defBadge.textContent = type === 'select' ? getOptionLabel(key, defVal) : truncateText(defVal, 30);
            defBadge.title = defVal;
        }

        // Render effective badge
        const effBadge = document.getElementById(`eff-${key}`);
        if (effBadge) {
            effBadge.textContent = type === 'select' ? getOptionLabel(key, effVal) : truncateText(effVal, 30);
            effBadge.title = effVal;
        }

        // Setup Override checkbox and input
        const checkbox = document.getElementById(`override-toggle-${key}`);
        const input = document.getElementById(`input-${key}`);

        if (checkbox && input) {
            // Unbind previous listeners to prevent multiple registrations
            const newCheckbox = checkbox.cloneNode(true);
            checkbox.parentNode.replaceChild(newCheckbox, checkbox);

            const isOverridden = storeVal !== null && storeVal !== undefined;
            newCheckbox.checked = isOverridden;
            input.disabled = !isOverridden;

            if (isOverridden) {
                input.value = storeVal;
            } else {
                input.value = type === 'select' ? defVal : defVal;
            }

            // Setup input change event
            const newInput = input.cloneNode(true);
            input.parentNode.replaceChild(newInput, input);

            newCheckbox.addEventListener('change', (e) => {
                const checked = e.target.checked;
                newInput.disabled = !checked;
                if (!checked) {
                    newInput.value = type === 'select' ? defVal : defVal;
                }
                updateEffectiveBadge(key, checked ? newInput.value : defVal, type);
            });

            newInput.addEventListener('input', (e) => {
                updateEffectiveBadge(key, e.target.value, type);
            });
            newInput.addEventListener('change', (e) => {
                updateEffectiveBadge(key, e.target.value, type);
            });
        }
    });
}

function updateEffectiveBadge(key, value, type) {
    const effBadge = document.getElementById(`eff-${key}`);
    if (effBadge) {
        effBadge.textContent = type === 'select' ? getOptionLabel(key, value) : truncateText(value, 30);
        effBadge.title = value;
    }
}

function getOptionLabel(key, val) {
    if (!globalOptions) return val;
    let list = [];
    if (key === 'ChunkingStrategy') list = globalOptions.chunkingStrategies;
    else if (key === 'EmbeddingModel') list = globalOptions.embeddingModels;
    else if (key === 'LlmModel') list = globalOptions.chatModels;

    const matched = list.find(o => o.value === val);
    return matched ? matched.label : val;
}

function truncateText(str, max) {
    if (!str) return '';
    const stringVal = String(str);
    if (stringVal.length <= max) return stringVal;
    return stringVal.substring(0, max) + '...';
}

// ── Save Configuration ────────────────────────────────────────
async function saveConfiguration() {
    hideAlert();
    if (!selectedSubjectId || !currentConfig) return;

    // Collect data
    const request = {};
    const keys = [
        { key: 'ChunkingStrategy', type: 'string' },
        { key: 'ChunkSize', type: 'int' },
        { key: 'ChunkOverlap', type: 'int' },
        { key: 'EmbeddingModel', type: 'string' },
        { key: 'TopK', type: 'int' },
        { key: 'SimilarityThreshold', type: 'double' },
        { key: 'MaxContextChunks', type: 'int' },
        { key: 'LlmModel', type: 'string' },
        { key: 'ChatTemperature', type: 'float' },
        { key: 'MaxHistoryMessages', type: 'int' },
        { key: 'TitleTemperature', type: 'float' },
        { key: 'CitationExtractionTemperature', type: 'float' },
        { key: 'ChatPrompt', type: 'string' },
        { key: 'ContextPrompt', type: 'string' },
        { key: 'NoContextRetrievedPrompt', type: 'string' },
        { key: 'TitlePrompt', type: 'string' },
        { key: 'CitationExtractionPrompt', type: 'string' }
    ];

    let hasClientValidationError = false;

    keys.forEach(k => {
        const toggle = document.getElementById(`override-toggle-${k.key}`);
        const input = document.getElementById(`input-${k.key}`);

        if (toggle && toggle.checked) {
            let val = input.value;
            if (k.type === 'int') {
                const parsed = parseInt(val, 10);
                if (isNaN(parsed)) {
                    showAlert('danger', `<i class="fas fa-exclamation-circle"></i> ${k.key} must be a valid integer.`);
                    hasClientValidationError = true;
                }
                request[k.key] = parsed;
            } else if (k.type === 'double' || k.type === 'float') {
                const parsed = parseFloat(val);
                if (isNaN(parsed)) {
                    showAlert('danger', `<i class="fas fa-exclamation-circle"></i> ${k.key} must be a valid number.`);
                    hasClientValidationError = true;
                }
                request[k.key] = parsed;
            } else {
                if (!val || val.trim() === '') {
                    showAlert('danger', `<i class="fas fa-exclamation-circle"></i> ${k.key} cannot be empty.`);
                    hasClientValidationError = true;
                }
                request[k.key] = val;
            }
        } else {
            request[k.key] = null; // Inherit
        }
    });

    if (hasClientValidationError) return;

    // Cross-field validation: overlap < size
    const size = request.ChunkSize !== null ? request.ChunkSize : currentConfig.indexing.chunkSize.globalDefault;
    const overlap = request.ChunkOverlap !== null ? request.ChunkOverlap : currentConfig.indexing.chunkOverlap.globalDefault;
    if (overlap >= size) {
        showAlert('danger', `<i class="fas fa-exclamation-circle"></i> Validation error: Chunk Overlap (${overlap}) must be strictly less than Chunk Size (${size}).`);
        return;
    }

    try {
        btnSaveConfig.disabled = true;
        btnSaveConfig.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';

        const res = await fetch(`/admin/ai-configuration?handler=Configuration&subjectId=${selectedSubjectId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery()
            },
            body: JSON.stringify(request)
        });

        const data = await res.json();

        if (res.ok) {
            showAlert('success', '<i class="fas fa-check-circle"></i> Configuration saved successfully!');
            currentConfig = data;
            renderConfiguration(currentConfig);
        } else {
            // Handle validation error responses
            let errorMsg = data.error || 'Failed to save configuration settings.';
            if (data && typeof data === 'object' && !data.error) {
                // ModelState error dictionary
                errorMsg = Object.values(data).flat().join('<br/>');
            }
            showAlert('danger', `<i class="fas fa-exclamation-circle"></i> Error saving configuration:<br/>${errorMsg}`);
        }
    } catch (err) {
        showAlert('danger', `<i class="fas fa-exclamation-circle"></i> Network error: ${err.message}`);
    } finally {
        btnSaveConfig.disabled = false;
        btnSaveConfig.innerHTML = '<i class="fas fa-save"></i> Save Configuration';
    }
}

function resetForm() {
    if (currentConfig) {
        renderConfiguration(currentConfig);
        hideAlert();
    }
}

// ── Reindexing Operations ─────────────────────────────────────
function confirmReindex() {
    if (!selectedSubjectId || !currentConfig) return;
    document.getElementById('reindex-subject-code').textContent = currentConfig.subjectCode;
    hideModalAlert('reindexConfirmModal');
    openModal('reindexConfirmModal');
}

async function submitReindex() {
    const btnSubmit = document.getElementById('btnReindexConfirmSubmit');
    try {
        btnSubmit.disabled = true;
        btnSubmit.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Reindexing...';

        const res = await fetch(`/admin/ai-configuration?handler=Reindex&subjectId=${selectedSubjectId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getAntiForgery()
            }
        });

        const data = await res.json();

        if (res.ok) {
            closeModal('reindexConfirmModal');
            showAlert('success', `<i class="fas fa-check-circle"></i> Reindexing queued successfully! Enqueued ${data.queuedDocumentCount} document(s).`);
        } else {
            showModalAlert('reindexConfirmModal', 'danger', data.error || 'Failed to enqueue reindexing.');
        }
    } catch (err) {
        showModalAlert('reindexConfirmModal', 'danger', `Network error: ${err.message}`);
    } finally {
        btnSubmit.disabled = false;
        btnSubmit.innerHTML = '<i class="fas fa-sync"></i> Confirm Reindex';
    }
}

// ── Prompt Modals ─────────────────────────────────────────────
function viewDefaultPrompt(settingName) {
    if (!currentConfig) return;
    const path = getPromptPath(settingName);
    if (!path) return;

    document.getElementById('viewPromptTitle').textContent = `Default: ${settingName}`;
    document.getElementById('promptPreviewText').textContent = path.globalDefault;
    openModal('viewPromptModal');
}

function viewEffectivePrompt(settingName) {
    if (!currentConfig) return;
    const path = getPromptPath(settingName);
    if (!path) return;

    document.getElementById('viewPromptTitle').textContent = `Effective: ${settingName}`;
    document.getElementById('promptPreviewText').textContent = path.effectiveValue;
    openModal('viewPromptModal');
}

function getPromptPath(settingName) {
    if (settingName === 'ChatPrompt') return currentConfig.prompts.chatPrompt;
    if (settingName === 'ContextPrompt') return currentConfig.prompts.contextPrompt;
    if (settingName === 'NoContextRetrievedPrompt') return currentConfig.prompts.noContextRetrievedPrompt;
    if (settingName === 'TitlePrompt') return currentConfig.prompts.titlePrompt;
    if (settingName === 'CitationExtractionPrompt') return currentConfig.prompts.citationExtractionPrompt;
    return null;
}
