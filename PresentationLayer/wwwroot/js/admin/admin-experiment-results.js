/**
 * admin-experiment-results.js
 * EduChatAI Admin — Experiment Results AJAX + rendering
 *
 * Architecture: fetch → normalize → render. The same render functions
 * accept fixture JSON and live DTO responses without modification.
 *
 * Contracts:
 *   GET /admin/experiments/results?handler=Summaries
 *       → IReadOnlyList<ExperimentSummaryDto>
 *   GET /admin/experiments/results?handler=Result&id={guid}
 *       → ExperimentResultDto
 */

'use strict';

// ── Helpers ───────────────────────────────────────────────────

function escHtml(str) {
    if (str == null) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

/**
 * ExperimentStatus numeric enum values (must match C# ExperimentStatus).
 * 0=Queued, 1=PreparingIndex, 2=Running, 3=Evaluating, 4=Completed, 5=Failed
 */
const ExperimentStatus = {
    Queued: 0,
    PreparingIndex: 1,
    Running: 2,
    Evaluating: 3,
    Completed: 4,
    Failed: 5,
};

/**
 * ExperimentQuestionStatus numeric enum values.
 * 0=Pending, 1=Running, 2=Completed, 3=Failed
 */
const QuestionStatus = {
    Pending: 0,
    Running: 1,
    Completed: 2,
    Failed: 3,
};

function statusLabel(status) {
    switch (status) {
        case ExperimentStatus.Queued: return 'Queued';
        case ExperimentStatus.PreparingIndex: return 'Reindexing';
        case ExperimentStatus.Running: return 'Running';
        case ExperimentStatus.Evaluating: return 'Evaluating';
        case ExperimentStatus.Completed: return 'Completed';
        case ExperimentStatus.Failed: return 'Failed';
        default: return 'Unknown';
    }
}

function statusCssKey(status) {
    switch (status) {
        case ExperimentStatus.Queued: return 'queued';
        case ExperimentStatus.PreparingIndex: return 'preparingindex';
        case ExperimentStatus.Running: return 'running';
        case ExperimentStatus.Evaluating: return 'evaluating';
        case ExperimentStatus.Completed: return 'completed';
        case ExperimentStatus.Failed: return 'failed';
        default: return 'queued';
    }
}

function statusIcon(status) {
    switch (status) {
        case ExperimentStatus.Queued: return 'fas fa-clock';
        case ExperimentStatus.PreparingIndex: return 'fas fa-sync-alt fa-spin';
        case ExperimentStatus.Running: return 'fas fa-play-circle';
        case ExperimentStatus.Evaluating: return 'fas fa-brain';
        case ExperimentStatus.Completed: return 'fas fa-check-circle';
        case ExperimentStatus.Failed: return 'fas fa-exclamation-circle';
        default: return 'fas fa-question-circle';
    }
}

function qStatusLabel(status) {
    switch (status) {
        case QuestionStatus.Pending: return 'Pending';
        case QuestionStatus.Running: return 'Running';
        case QuestionStatus.Completed: return 'Done';
        case QuestionStatus.Failed: return 'Failed';
        default: return '—';
    }
}

function qStatusCss(status) {
    switch (status) {
        case QuestionStatus.Pending: return 'er-q-badge--pending';
        case QuestionStatus.Running: return 'er-q-badge--running';
        case QuestionStatus.Completed: return 'er-q-badge--completed';
        case QuestionStatus.Failed: return 'er-q-badge--failed';
        default: return '';
    }
}

function isTerminal(status) {
    return status === ExperimentStatus.Completed || status === ExperimentStatus.Failed;
}

function fmtScore(v) {
    if (v == null) return '<span class="er-score-val--null">—</span>';
    const pct = (v * 100).toFixed(0);
    const cls = v >= 0.8 ? 'er-score-val--high' :
        v >= 0.6 ? 'er-score-val--medium' : 'er-score-val--low';
    return `<span class="${cls}">${(v).toFixed(2)}</span>`;
}

function fmtScoreCard(v) {
    if (v == null) return { text: '—', cls: 'er-score-val--null' };
    const cls = v >= 0.8 ? 'er-score-val--high' :
        v >= 0.6 ? 'er-score-val--medium' : 'er-score-val--low';
    return { text: v.toFixed(2), cls };
}

function fmtMs(v) {
    if (v == null) return '—';
    return v.toLocaleString();
}

function fmtDatetime(iso) {
    if (!iso) return '—';
    const d = new Date(iso);
    return d.toLocaleString('vi-VN', { hour12: false });
}

// ── Data layer ────────────────────────────────────────────────

function fetchSummaries() {
    return $.ajax({ url: '?handler=Summaries', method: 'GET', dataType: 'json' });
}

function fetchResult(id) {
    return $.ajax({
        url: '?handler=Result&id=' + encodeURIComponent(id),
        method: 'GET',
        dataType: 'json',
    });
}

// ── Normalization layer ───────────────────────────────────────

function normalizeSummary(s) {
    return {
        id: s.experimentId,
        name: s.experimentName,
        subjectCode: s.subjectCode,
        status: s.status,
        strategy: s.chunkingStrategy,
        chunkLabel: `${s.chunkSize}/${s.chunkOverlap}`,
        model: s.llmModel,
        progress: `${s.completedQuestionCount}/${s.totalQuestionCount}`,
        // Numeric progress fields consumed by the detail stepper. Set here (not only in
        // openDetail) so they survive polling, which re-normalizes via normalizeResult.
        completedQ: s.completedQuestionCount,
        totalQ: s.totalQuestionCount,
        indexedDocs: s.indexedDocumentCount,
        affectedDocs: s.affectedDocumentCount,
        scores: s.aggregateScores,
        createdAt: s.createdAt,
        completedAt: s.completedAt,
        questionSetKey: s.questionSetKey,
        failureReason: s.failureReason,
    };
}

function normalizeSummaries(dtos) {
    return dtos.map(normalizeSummary);
}

function normalizeResult(dto) {
    return {
        summary: normalizeSummary(dto.summary),
        config: dto.configuration,
        questions: dto.questions,
    };
}

// ── Render: summary table ─────────────────────────────────────

function renderSummaryTable(summaries) {
    const tbody = document.getElementById('erSummaryTableBody');
    if (!tbody) return;

    const countEl = document.getElementById('erRunCount');
    if (countEl) countEl.textContent = `${summaries.length} run${summaries.length !== 1 ? 's' : ''}`;

    if (summaries.length === 0) {
        tbody.innerHTML = '<tr class="er-table-empty"><td colspan="14">No experiment runs yet.</td></tr>';
        return;
    }

    tbody.innerHTML = summaries.map(s => {
        const isCompleted = s.status === ExperimentStatus.Completed;
        const badgeKey = statusCssKey(s.status);
        return `<tr class="er-summary-row" data-id="${escHtml(s.id)}" data-subject="${escHtml(s.subjectCode)}" data-qkey="${escHtml(s.questionSetKey)}" data-status="${s.status}">
            <td class="er-th-check">
                <input type="checkbox" class="er-row-check" id="chk-${escHtml(s.id)}"
                    ${!isCompleted ? 'disabled title="Only completed runs can be compared"' : ''}
                    aria-label="Select ${escHtml(s.name)}" />
            </td>
            <td title="${escHtml(s.name)}">${escHtml(s.name)}</td>
            <td>${escHtml(s.subjectCode)}</td>
            <td>${escHtml(s.strategy)}</td>
            <td>${escHtml(s.chunkLabel)}</td>
            <td title="${escHtml(s.model)}" class="er-model-cell">${escHtml(s.model)}</td>
            <td><span class="er-badge er-badge--${badgeKey}"><i class="${statusIcon(s.status)}"></i> ${statusLabel(s.status)}</span></td>
            <td class="er-td-num">${escHtml(s.progress)}</td>
            <td class="er-td-num">${fmtScore(s.scores.faithfulness)}</td>
            <td class="er-td-num">${fmtScore(s.scores.answerRelevancy)}</td>
            <td class="er-td-num">${fmtScore(s.scores.contextPrecision)}</td>
            <td class="er-td-num">${fmtScore(s.scores.contextRecall)}</td>
            <td>${fmtDatetime(s.createdAt)}</td>
            <td>
                <button class="er-btn er-btn--refresh er-view-btn" data-id="${escHtml(s.id)}" title="View details">
                    <i class="fas fa-eye"></i>
                </button>
            </td>
        </tr>`;
    }).join('');
}

// ── Render: detail panel ──────────────────────────────────────

/** Map status to stepper step indices (0-based). */
function stepIndex(status) {
    switch (status) {
        case ExperimentStatus.Queued: return 0;
        case ExperimentStatus.PreparingIndex: return 1;
        case ExperimentStatus.Running: return 2;
        case ExperimentStatus.Evaluating: return 3;
        case ExperimentStatus.Completed: return 4;
        case ExperimentStatus.Failed: return -1; // special
        default: return 0;
    }
}

function renderProgressStepper(status, completedQ, totalQ, indexedDocs, affectedDocs, failedQ) {
    const steps = ['erStepQueued', 'erStepPreparingIndex', 'erStepRunning', 'erStepEvaluating', 'erStepCompleted'];
    const current = stepIndex(status);
    const isFailed = status === ExperimentStatus.Failed;

    steps.forEach((id, i) => {
        const el = document.getElementById(id);
        if (!el) return;
        el.classList.remove('er-step--active', 'er-step--done', 'er-step--failed');
        if (isFailed) {
            if (i < current) el.classList.add('er-step--done');
            else el.classList.add('er-step--failed');
        } else {
            if (i < current) el.classList.add('er-step--done');
            else if (i === current) el.classList.add('er-step--active');
        }
    });

    // Update step lines
    const lines = document.querySelectorAll('.er-step-line');
    lines.forEach((line, i) => {
        line.classList.toggle('er-step-line--done', i < current && !isFailed);
    });

    // Progress counts
    const countEl = document.getElementById('erProgressCounts');
    if (countEl) {
        const parts = [];
        if (status === ExperimentStatus.PreparingIndex) {
            parts.push(`Documents indexed: ${indexedDocs}/${affectedDocs}`);
        }
        if ([ExperimentStatus.Running, ExperimentStatus.Evaluating, ExperimentStatus.Completed, ExperimentStatus.Failed].includes(status)) {
            parts.push(`Questions processed: ${completedQ}/${totalQ}`);
            parts.push(`Questions failed: ${failedQ}`);
        }
        if (parts.length > 0) {
            countEl.textContent = parts.join(' · ');
            countEl.removeAttribute('hidden');
        } else {
            countEl.setAttribute('hidden', true);
        }
    }
}

function renderConfigGrid(config) {
    const grid = document.getElementById('erConfigGrid');
    if (!grid) return;

    const entries = [
        ['Subject', `${config.subjectCode} — ${config.subjectName}`],
        ['Chunking Strategy', config.chunkingStrategy],
        ['Chunk Size', config.chunkSize],
        ['Chunk Overlap', config.chunkOverlap],
        ['Embedding Model', config.embeddingModel],
        ['Top K', config.topK],
        ['Similarity Threshold', config.similarityThreshold],
        ['Max Context Chunks', config.maxContextChunks],
        ['LLM Model', config.llmModel],
        ['Temperature', config.chatTemperature],
        ['Max History Messages', config.maxHistoryMessages],
        ['Judge Model', config.judgeModel],
        ['Evaluator Version', config.evaluatorPromptVersion],
    ];

    grid.innerHTML = entries.map(([k, v]) => `
        <div class="er-config-item">
            <span class="er-config-key">${escHtml(k)}</span>
            <span class="er-config-val">${escHtml(String(v ?? '—'))}</span>
        </div>
    `).join('');
}

function renderAggregateScores(scores) {
    const scoreRow = document.getElementById('erScoreRow');
    if (!scoreRow) return;

    const metricMap = [
        ['scoreF', scores.faithfulness],
        ['scoreAR', scores.answerRelevancy],
        ['scoreCP', scores.contextPrecision],
        ['scoreCR', scores.contextRecall],
    ];

    metricMap.forEach(([id, val]) => {
        const el = document.getElementById(id);
        if (!el) return;
        const { text, cls } = fmtScoreCard(val);
        el.textContent = text;
        el.className = 'er-score-value ' + cls;
    });

    scoreRow.removeAttribute('hidden');
}

function renderQuestionResults(questions) {
    const tbody = document.getElementById('erQuestionsTableBody');
    const section = document.getElementById('erQuestionsSection');
    if (!tbody || !section) return;

    section.removeAttribute('hidden');

    if (!questions || questions.length === 0) {
        tbody.innerHTML = '<tr class="er-table-empty"><td colspan="10">No question results yet.</td></tr>';
        return;
    }

    tbody.innerHTML = questions.map((q, idx) => {
        const hasAnswer = q.generatedAnswer && q.generatedAnswer.trim().length > 0;
        const answerId = `er-answer-${idx}`;
        return `<tr>
            <td>${escHtml(q.externalId)}</td>
            <td title="${escHtml(q.question)}">${escHtml(q.question.length > 60 ? q.question.substring(0, 60) + '…' : q.question)}</td>
            <td><span class="${qStatusCss(q.status)}">${qStatusLabel(q.status)}</span></td>
            <td>
                ${hasAnswer
                ? `<div class="er-answer-preview" onclick="toggleAnswer('${answerId}')" title="Click to expand">${escHtml(q.generatedAnswer.substring(0, 80))}…</div>
                       <div class="er-answer-full" id="${answerId}" hidden>${escHtml(q.generatedAnswer)}</div>`
                : '<span style="color:var(--color-text-secondary);font-style:italic">—</span>'
            }
            </td>
            <td class="er-td-num">${q.retrievedContexts ? q.retrievedContexts.length : 0}</td>
            <td class="er-td-num">${fmtScore(q.scores.faithfulness)}</td>
            <td class="er-td-num">${fmtScore(q.scores.answerRelevancy)}</td>
            <td class="er-td-num">${fmtScore(q.scores.contextPrecision)}</td>
            <td class="er-td-num">${fmtScore(q.scores.contextRecall)}</td>
            <td class="er-td-num">${fmtMs(q.totalResponseTimeMs)}</td>
        </tr>`;
    }).join('');
}

/** Toggle expanded answer text. */
function toggleAnswer(id) {
    const el = document.getElementById(id);
    if (!el) return;
    if (el.hasAttribute('hidden')) el.removeAttribute('hidden');
    else el.setAttribute('hidden', true);
}

function renderExperimentDetail(normalized) {
    const s = normalized.summary;

    // Header
    document.getElementById('erDetailName').textContent = s.name;
    const badgeEl = document.getElementById('erDetailStatus');
    badgeEl.className = `er-badge er-badge--${statusCssKey(s.status)}`;
    badgeEl.innerHTML = `<i class="${statusIcon(s.status)}"></i> ${statusLabel(s.status)}`;

    // Progress stepper (shown while not terminal) and counts (shown whenever available)
    const stepperEl = document.getElementById('erProgressStepper');
    const failedQ = normalized.questions.filter(question => question.status === QuestionStatus.Failed).length;
    renderProgressStepper(s.status, normalized.summary.completedQ, normalized.summary.totalQ,
        normalized.summary.indexedDocs, normalized.summary.affectedDocs, failedQ);
    if (isTerminal(s.status)) {
        stepperEl.setAttribute('hidden', true);
    } else {
        stepperEl.removeAttribute('hidden');
    }

    // Config
    if (normalized.config) renderConfigGrid(normalized.config);

    // Aggregate scores — only meaningful once Complete. Re-hide otherwise so a
    // previously-viewed completed run's scores don't linger when switching to an
    // Active/Failed run (or when the same panel polls through non-terminal states).
    if (s.status === ExperimentStatus.Completed) {
        renderAggregateScores(s.scores);
    } else {
        const scoreRow = document.getElementById('erScoreRow');
        if (scoreRow) scoreRow.setAttribute('hidden', true);
    }

    // Failure reason
    const failBanner = document.getElementById('erFailureBanner');
    if (s.status === ExperimentStatus.Failed && s.failureReason) {
        document.getElementById('erFailureText').textContent = s.failureReason;
        failBanner.removeAttribute('hidden');
    } else {
        failBanner.setAttribute('hidden', true);
    }

    // Questions
    renderQuestionResults(normalized.questions);

    // Show panel
    const panel = document.getElementById('erDetailPanel');
    $(panel).slideDown();
    panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

// ── Compare selection validation ──────────────────────────────

function getSelectedSummaries() {
    const result = [];
    document.querySelectorAll('.er-row-check:checked').forEach(chk => {
        const row = chk.closest('tr');
        if (!row) return;
        result.push({
            id: row.dataset.id,
            subject: row.dataset.subject,
            qkey: row.dataset.qkey,
            status: parseInt(row.dataset.status, 10),
        });
    });
    return result;
}

function updateCompareButton() {
    const selected = getSelectedSummaries();
    const btn = document.getElementById('btnCompare');
    if (!btn) return;

    const eligible = selected.length === 2
        && selected.every(s => s.status === ExperimentStatus.Completed)
        && selected[0].subject === selected[1].subject
        && selected[0].qkey === selected[1].qkey;

    btn.disabled = !eligible;

    if (eligible) {
        $('#erCompareLeft').val(selected[0].id);
        $('#erCompareRight').val(selected[1].id);
    } else {
        $('#erCompareLeft').val('');
        $('#erCompareRight').val('');
    }

    // Update tooltip
    let tip = 'Select exactly two completed runs with the same subject and question set';
    if (selected.length === 2 && !eligible) {
        if (!selected.every(s => s.status === ExperimentStatus.Completed)) {
            tip = 'Both selected runs must be completed';
        } else if (selected[0].subject !== selected[1].subject) {
            tip = 'Selected runs must be from the same subject';
        } else if (selected[0].qkey !== selected[1].qkey) {
            tip = 'Selected runs must use the same question set';
        }
    }
    btn.title = tip;
}

// ── Polling controller ────────────────────────────────────────

let _pollTimer = null;
let _pollingId = null;

// Concurrency generations: guard against out-of-order AJAX responses so a stale
// response cannot overwrite a newer view (rapid row switches / repeated refreshes).
let _detailReqId = 0;
let _summariesReqId = 0;

function stopPolling() {
    if (_pollTimer) {
        clearInterval(_pollTimer);
        _pollTimer = null;
    }
    _pollingId = null;
}

function startPolling(id) {
    stopPolling();
    _pollingId = id;
    _pollTimer = setInterval(() => {
        if (_pollingId !== id) { stopPolling(); return; }
        fetchResult(id)
            .done(dto => {
                if (_pollingId !== id) return;
                const norm = normalizeResult(dto);
                // Update detail panel in-place
                renderExperimentDetail(norm);
                // If completed or failed, stop polling and refresh summary table
                if (isTerminal(norm.summary.status)) {
                    stopPolling();
                    loadSummaries();
                }
            })
            .fail(() => {
                // Don't stop polling on transient failures
            });
    }, 5000);
}

// ── Load & open detail ────────────────────────────────────────

function openDetail(id) {
    const reqId = ++_detailReqId;
    fetchResult(id)
        .done(dto => {
            if (reqId !== _detailReqId) return; // a newer detail request superseded this one
            // normalizeSummary already maps completedQ/totalQ/indexedDocs/affectedDocs,
            // so no manual flatten is needed here.
            const norm = normalizeResult(dto);
            renderExperimentDetail(norm);

            if (!isTerminal(norm.summary.status)) {
                startPolling(id);
            } else {
                stopPolling();
            }
        })
        .fail((xhr) => {
            if (reqId !== _detailReqId) return; // stale failure; a newer request owns the panel
            const msg = xhr.responseJSON?.error ?? 'Failed to load experiment details.';
            alert(msg);
        });
}

let _lastSummaries = [];

function loadSummaries() {
    const reqId = ++_summariesReqId;
    fetchSummaries()
        .done(dtos => {
            if (reqId !== _summariesReqId) return; // superseded by a newer refresh
            _lastSummaries = normalizeSummaries(dtos);
            renderSummaryTable(_lastSummaries);
        })
        .fail(() => {
            if (reqId !== _summariesReqId) return; // stale failure; a newer refresh owns the table
            const tbody = document.getElementById('erSummaryTableBody');
            if (tbody) {
                tbody.innerHTML = '<tr class="er-table-empty"><td colspan="14">Failed to load experiments. <button onclick="loadSummaries()" class="er-btn er-btn--refresh">Retry</button></td></tr>';
            }
        });
}

// ── Controller ────────────────────────────────────────────────

$(function () {
    // Load summaries on page load
    loadSummaries();

    // Refresh button
    $('#btnRefresh').on('click', loadSummaries);

    // Close detail panel
    $('#btnCloseDetail').on('click', () => {
        stopPolling();
        $('#erDetailPanel').slideUp();
    });

    // View button (delegated)
    $('#erSummaryTableBody').on('click', '.er-view-btn', function () {
        const id = $(this).data('id');
        openDetail(id);
    });

    // Row click to open detail
    $('#erSummaryTableBody').on('click', '.er-summary-row td:not(.er-th-check):not(:last-child)', function () {
        const id = $(this).closest('tr').data('id');
        if (id) openDetail(id);
    });

    // Checkbox change → update compare button
    $('#erSummaryTableBody').on('change', '.er-row-check', updateCompareButton);

    // Compare button navigates to compare page with both IDs
    $('#btnCompare').on('click', function () {
        const left = $('#erCompareLeft').val();
        const right = $('#erCompareRight').val();
        if (left && right) {
            window.location.href = `/admin/experiments/compare?leftId=${encodeURIComponent(left)}&rightId=${encodeURIComponent(right)}`;
        }
    });

    // Deep-link: open experiment if id was passed via server-rendered variable
    const initialId = window.erInitialId;
    if (initialId && initialId.trim().length > 0) {
        // Wait for summaries to load, then open
        setTimeout(() => openDetail(initialId), 800);
    }
});
