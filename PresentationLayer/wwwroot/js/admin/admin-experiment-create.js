/**
 * admin-experiment-create.js
 * EduChatAI Admin Panel — AI Experiment Creation & Preflight AJAX + UI orchestration
 */

'use strict';

// ── Global State ──────────────────────────────────────────────
let globalOptions = null;
let selectedSubjectId = null;
let currentPreflightResult = null;

// ── DOM Elements ──────────────────────────────────────────────
const subjectSelect = document.getElementById('subjectSelect');
const experimentFormContainer = document.getElementById('experimentFormContainer');
const btnLaunchExperiment = document.getElementById('btnLaunchExperiment');
const alertContainer = document.getElementById('alertContainer');
const preflightStatusBox = document.getElementById('preflightStatusBox');
const questionList = document.getElementById('questionList');
const selectedCountText = document.getElementById('selectedCountText');
const confirmLaunchModal = document.getElementById('confirmLaunchModal');

// ── Page Initialization ───────────────────────────────────────
document.addEventListener('DOMContentLoaded', async () => {
    setupModalEventHandlers();
    await loadSubjects();
});

function setupModalEventHandlers() {
    // Backdrop click close for modals
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

    // Subject select change handler
    subjectSelect.addEventListener('change', async (e) => {
        selectedSubjectId = parseInt(e.target.value, 10);
        if (selectedSubjectId) {
            await loadSubjectOptions(selectedSubjectId);
        } else {
            experimentFormContainer.style.display = 'none';
        }
    });

    // Preflight trigger changes
    document.addEventListener('change', e => {
        if (e.target.classList.contains('preflight-trigger')) {
            runPreflight();
        }
    });
    document.addEventListener('input', e => {
        if (e.target.classList.contains('preflight-trigger') && e.target.type === 'number') {
            runPreflight();
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
    alertContainer.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

function hideAlert() {
    alertContainer.hidden = true;
}

// ── Load Subjects ─────────────────────────────────────────────
async function loadSubjects() {
    try {
        const res = await fetch('/admin/ai-configuration?handler=Subjects');
        if (!res.ok) throw new Error('Failed to load subjects.');
        const subjects = await res.json();

        subjectSelect.innerHTML = '<option value="" disabled selected>-- Choose Subject --</option>';
        subjects.forEach(sub => {
            const opt = document.createElement('option');
            opt.value = sub.subjectId;
            opt.textContent = `${sub.subjectCode} - ${sub.subjectName}`;
            subjectSelect.appendChild(opt);
        });
    } catch (err) {
        showAlert('danger', `<i class="fas fa-exclamation-circle"></i> Error loading subjects: ${err.message}`);
    }
}

// ── Load Subject Options & Initial Configurations ──────────────
async function loadSubjectOptions(subjectId) {
    hideAlert();
    try {
        btnLaunchExperiment.disabled = true;

        const res = await fetch(`/admin/experiments/create?handler=Options&subjectId=${subjectId}`);
        if (!res.ok) throw new Error('Failed to load experiment options.');

        globalOptions = await res.json();

        // Populate Options lists
        populateDropdown('chunkingStrategy', globalOptions.aiOptions.chunkingStrategies);
        populateDropdown('embeddingModel', globalOptions.aiOptions.embeddingModels);
        populateDropdown('llmModel', globalOptions.aiOptions.chatModels);
        populateDropdown('evaluatorLlm', globalOptions.evaluatorCapabilities.llmOptions.map(m => ({ value: `${m.provider}|${m.model}`, label: m.label })));
        populateDropdown('evaluatorEmbedding', globalOptions.evaluatorCapabilities.embeddingOptions.map(m => ({ value: `${m.provider}|${m.model}`, label: m.label })));

        // Populate with current configuration values
        const current = globalOptions.currentConfiguration;
        document.getElementById('chunkingStrategy').value = current.indexing.chunkingStrategy.effectiveValue;
        document.getElementById('chunkSize').value = current.indexing.chunkSize.effectiveValue;
        document.getElementById('chunkOverlap').value = current.indexing.chunkOverlap.effectiveValue;
        document.getElementById('embeddingModel').value = current.indexing.embeddingModel.effectiveValue;

        document.getElementById('topK').value = current.retrieval.topK.effectiveValue;
        document.getElementById('similarityThreshold').value = current.retrieval.similarityThreshold.effectiveValue;
        document.getElementById('maxContextChunks').value = current.retrieval.maxContextChunks.effectiveValue;

        document.getElementById('llmModel').value = current.generation.llmModel.effectiveValue;
        document.getElementById('chatTemperature').value = current.generation.chatTemperature.effectiveValue;

        // Default name
        document.getElementById('experimentName').value = `${globalOptions.subjectCode} smoke run`;
        document.getElementById('notes').value = 'Three questions to verify flow before running all 50.';

        // Populate test questions
        renderQuestionList(globalOptions.testQuestions);

        experimentFormContainer.style.display = 'block';
        btnLaunchExperiment.disabled = false;

        // Run preflight checks immediately
        await runPreflight();
    } catch (err) {
        showAlert('danger', `<i class="fas fa-exclamation-circle"></i> Error loading configuration options: ${err.message}`);
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

function renderQuestionList(questions) {
    questionList.innerHTML = '';
    questions.forEach(q => {
        const item = document.createElement('div');
        item.className = 'question-item';
        item.dataset.id = q.testQuestionId;

        const cb = document.createElement('input');
        cb.type = 'checkbox';
        cb.className = 'question-checkbox';
        cb.value = q.testQuestionId;
        cb.id = `q-cb-${q.testQuestionId}`;

        const details = document.createElement('div');
        details.className = 'question-details';

        const idBadge = document.createElement('span');
        idBadge.className = 'question-id-badge';
        idBadge.textContent = q.externalId;

        const text = document.createElement('span');
        text.className = 'question-text';
        text.textContent = q.question;

        details.appendChild(idBadge);
        details.appendChild(text);

        item.appendChild(cb);
        item.appendChild(details);

        // Click row to toggle
        item.addEventListener('click', (e) => {
            if (e.target !== cb) {
                cb.checked = !cb.checked;
                updateSelectedQuestionsCount();
            }
        });
        cb.addEventListener('change', () => {
            updateSelectedQuestionsCount();
        });

        questionList.appendChild(item);
    });
    updateSelectedQuestionsCount();
}

function updateSelectedQuestionsCount() {
    const checked = questionList.querySelectorAll('.question-checkbox:checked').length;
    selectedCountText.textContent = `${checked} selected`;
}

// ── Shortcut Button Actions ──────────────────────────────────
function selectQuickRun() {
    const checkboxes = questionList.querySelectorAll('.question-checkbox');
    checkboxes.forEach((cb, idx) => {
        cb.checked = idx < 3;
    });
    updateSelectedQuestionsCount();
}

function selectAllQuestions() {
    const checkboxes = questionList.querySelectorAll('.question-checkbox');
    checkboxes.forEach(cb => {
        cb.checked = true;
    });
    updateSelectedQuestionsCount();
}

function clearAllQuestions() {
    const checkboxes = questionList.querySelectorAll('.question-checkbox');
    checkboxes.forEach(cb => {
        cb.checked = false;
    });
    updateSelectedQuestionsCount();
}

// ── Run Preflight Compatibility Checks ────────────────────────
let preflightDebounceTimeout = null;

async function runPreflight() {
    if (!selectedSubjectId) return;

    // Debounce to prevent immediate overlapping requests on input typing
    if (preflightDebounceTimeout) clearTimeout(preflightDebounceTimeout);

    preflightDebounceTimeout = setTimeout(async () => {
        setPreflightState('checking', 'Checking compatibility...', 'Contacting index preflight service...');
        btnLaunchExperiment.disabled = true;

        const request = {
            subjectId: selectedSubjectId,
            chunkingStrategy: document.getElementById('chunkingStrategy').value,
            chunkSize: parseInt(document.getElementById('chunkSize').value, 10),
            chunkOverlap: parseInt(document.getElementById('chunkOverlap').value, 10),
            embeddingModel: document.getElementById('embeddingModel').value
        };

        // Basic sanity checks before sending
        if (isNaN(request.chunkSize) || request.chunkSize < 100 || request.chunkSize > 8000 ||
            isNaN(request.chunkOverlap) || request.chunkOverlap < 0 || request.chunkOverlap >= request.chunkSize) {
            setPreflightState('blocked', 'Invalid settings', 'Please check Chunk Size (100 - 8000) and Chunk Overlap (must be non-negative and less than Chunk Size).');
            return;
        }

        try {
            const res = await fetch('/admin/experiments/create?handler=Preflight', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgery()
                },
                body: JSON.stringify(request)
            });

            if (!res.ok) {
                const data = await res.json();
                throw new Error(data.error || 'Preflight checker failed.');
            }

            const result = await res.json();
            currentPreflightResult = result;

            if (result.blockingReason) {
                setPreflightState('blocked', 'Preflight Blocked', result.blockingReason);
            } else if (result.requiresReindex) {
                setPreflightState('warning', 'Reindexing Required', `System will reindex ${result.affectedDocumentCount} document(s) before running. Chat will be locked.`);
                btnLaunchExperiment.disabled = false;
            } else {
                setPreflightState('compatible', 'Compatible', 'No reindexing required. The experiment can start immediately.');
                btnLaunchExperiment.disabled = false;
            }
        } catch (err) {
            setPreflightState('blocked', 'Preflight Error', err.message);
        }
    }, 250);
}

function setPreflightState(state, title, desc) {
    preflightStatusBox.className = `preflight-status-box preflight-${state}`;
    const iconEl = preflightStatusBox.querySelector('.preflight-status-icon i');

    if (state === 'checking') {
        iconEl.className = 'fas fa-spinner fa-spin';
    } else if (state === 'compatible') {
        iconEl.className = 'fas fa-check-circle';
    } else if (state === 'warning') {
        iconEl.className = 'fas fa-exclamation-triangle';
    } else if (state === 'blocked') {
        iconEl.className = 'fas fa-times-circle';
    } else {
        iconEl.className = 'fas fa-hourglass-start';
    }

    preflightStatusBox.querySelector('.preflight-title').textContent = title;
    preflightStatusBox.querySelector('.preflight-desc').textContent = desc;
}

// ── Launch / Submit Experiment ────────────────────────────────
function launchExperiment() {
    hideAlert();
    if (!selectedSubjectId || !globalOptions) return;

    // Validate name
    const name = document.getElementById('experimentName').value;
    if (!name || name.trim() === '') {
        showAlert('danger', '<i class="fas fa-exclamation-circle"></i> Experiment Name is required.');
        return;
    }

    // Validate overlap < size
    const size = parseInt(document.getElementById('chunkSize').value, 10);
    const overlap = parseInt(document.getElementById('chunkOverlap').value, 10);
    if (overlap >= size) {
        showAlert('danger', `<i class="fas fa-exclamation-circle"></i> Validation error: Chunk Overlap (${overlap}) must be less than Chunk Size (${size}).`);
        return;
    }

    // Validate at least one question
    const selectedQuestions = Array.from(questionList.querySelectorAll('.question-checkbox:checked')).map(cb => parseInt(cb.value, 10));
    if (selectedQuestions.length === 0) {
        showAlert('danger', '<i class="fas fa-exclamation-circle"></i> Select at least one test question to run the experiment.');
        return;
    }

    // Validate question count (max 50)
    if (selectedQuestions.length > 50) {
        showAlert('danger', '<i class="fas fa-exclamation-circle"></i> Cannot select more than 50 questions.');
        return;
    }

    // Check preflight results
    if (currentPreflightResult && currentPreflightResult.blockingReason) {
        showAlert('danger', `<i class="fas fa-exclamation-circle"></i> Cannot launch: ${currentPreflightResult.blockingReason}`);
        return;
    }

    if (currentPreflightResult && currentPreflightResult.requiresReindex) {
        // Must show confirmation modal
        document.getElementById('confirm-doc-count').textContent = currentPreflightResult.affectedDocumentCount;
        document.getElementById('confirm-subject-code').textContent = globalOptions.subjectCode;
        openModal('confirmLaunchModal');
    } else {
        // Launch immediately
        submitExperimentCreation(false);
    }
}

async function submitExperimentCreation(requiresReindex) {
    const btnSubmitModal = document.getElementById('btnConfirmLaunchSubmit');
    const selectedQuestions = Array.from(questionList.querySelectorAll('.question-checkbox:checked')).map(cb => parseInt(cb.value, 10));

    const [evaluatorLlmProvider, evaluatorLlmModel] = document.getElementById('evaluatorLlm').value.split('|', 2);
    const [evaluatorEmbeddingProvider, evaluatorEmbeddingModel] = document.getElementById('evaluatorEmbedding').value.split('|', 2);
    const request = {
        experimentName: document.getElementById('experimentName').value,
        subjectId: selectedSubjectId,
        testQuestionIds: selectedQuestions,
        chunkingStrategy: document.getElementById('chunkingStrategy').value,
        chunkSize: parseInt(document.getElementById('chunkSize').value, 10),
        chunkOverlap: parseInt(document.getElementById('chunkOverlap').value, 10),
        embeddingModel: document.getElementById('embeddingModel').value,
        topK: parseInt(document.getElementById('topK').value, 10),
        similarityThreshold: parseFloat(document.getElementById('similarityThreshold').value),
        maxContextChunks: parseInt(document.getElementById('maxContextChunks').value, 10),
        llmModel: document.getElementById('llmModel').value,
        chatTemperature: parseFloat(document.getElementById('chatTemperature').value),
        evaluatorLlmProvider,
        evaluatorLlmModel,
        evaluatorEmbeddingProvider,
        evaluatorEmbeddingModel,
        notes: document.getElementById('notes').value
    };

    try {
        btnLaunchExperiment.disabled = true;
        btnLaunchExperiment.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Launching...';
        if (requiresReindex) {
            btnSubmitModal.disabled = true;
            btnSubmitModal.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
        }

        const res = await fetch('/admin/experiments/create?handler=Experiment', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgery()
            },
            body: JSON.stringify(request)
        });

        const data = await res.json();

        if (res.ok) {
            if (requiresReindex) closeModal('confirmLaunchModal');
            showAlert('success', '<i class="fas fa-check-circle"></i> Experiment launched successfully! Redirecting...');
            // Redirect immediately
            window.location.href = `/admin/experiments/results?id=${data.experimentId}`;
        } else {
            if (requiresReindex) closeModal('confirmLaunchModal');
            let errorMsg = data.error || 'Failed to create experiment.';
            if (data && typeof data === 'object' && !data.error) {
                errorMsg = Object.values(data).flat().join('<br/>');
            }
            showAlert('danger', `<i class="fas fa-exclamation-circle"></i> Launch failed:<br/>${errorMsg}`);
        }
    } catch (err) {
        if (requiresReindex) closeModal('confirmLaunchModal');
        showAlert('danger', `<i class="fas fa-exclamation-circle"></i> Network error: ${err.message}`);
    } finally {
        btnLaunchExperiment.disabled = false;
        btnLaunchExperiment.innerHTML = '<i class="fas fa-play"></i> Run Experiment';
        if (requiresReindex) {
            btnSubmitModal.disabled = false;
            btnSubmitModal.innerHTML = '<i class="fas fa-play"></i> Agree and Launch';
        }
    }
}
