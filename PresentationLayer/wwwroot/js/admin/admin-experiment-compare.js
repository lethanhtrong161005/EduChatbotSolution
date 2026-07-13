/**
 * admin-experiment-compare.js
 * EduChatAI Admin — Experiment Comparison rendering
 *
 * Architecture: fetch → normalize → render. The same render functions
 * accept fixture JSON and live DTO responses without modification.
 *
 * Contract: GET /admin/experiments/compare?handler=Comparison&leftId={guid}&rightId={guid}
 *   → ExperimentComparisonDto
 *   Errors: 400 (invalid/incompatible), 404 (not found), 409 (incompatible state)
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

function fmtScore(v) {
    if (v == null) return '—';
    return v.toFixed(2);
}

function scoreColorClass(v) {
    if (v == null) return 'er-score-val--null';
    return v >= 0.8 ? 'er-score-val--high' :
           v >= 0.6 ? 'er-score-val--medium' : 'er-score-val--low';
}

function fmtDelta(delta) {
    if (delta == null) return '';
    const sign = delta >= 0 ? '+' : '';
    const cls = delta > 0.005 ? 'ec-delta--pos' :
                delta < -0.005 ? 'ec-delta--neg' : 'ec-delta--zero';
    return `<span class="ec-delta ${cls}">${sign}${delta.toFixed(2)}</span>`;
}

function fmtDatetime(iso) {
    if (!iso) return '—';
    const d = new Date(iso);
    return d.toLocaleString('vi-VN', { hour12: false });
}

// ── Data layer ────────────────────────────────────────────────

function fetchComparison(leftId, rightId) {
    return $.ajax({
        url: `?handler=Comparison&leftId=${encodeURIComponent(leftId)}&rightId=${encodeURIComponent(rightId)}`,
        method: 'GET',
        dataType: 'json',
    });
}

// ── Normalization layer ───────────────────────────────────────

function buildMetaHtml(s) {
    return [
        `<span class="ec-meta-item"><i class="fas fa-layer-group"></i> ${escHtml(s.chunkingStrategy)}</span>`,
        `<span class="ec-meta-item"><i class="fas fa-ruler-combined"></i> ${s.chunkSize}/${s.chunkOverlap}</span>`,
        `<span class="ec-meta-item"><i class="fas fa-robot"></i> ${escHtml(s.llmModel)}</span>`,
        `<span class="ec-meta-item"><i class="fas fa-calendar"></i> ${fmtDatetime(s.completedAt || s.createdAt)}</span>`,
    ].join('');
}

function normalizeComparison(dto) {
    return {
        left: dto.left,
        right: dto.right,
        metrics: dto.metrics,
        questions: dto.questions,
    };
}

// ── Render layer ──────────────────────────────────────────────

function renderComparisonHeader(left, right) {
    document.getElementById('ecLeftName').textContent = left.experimentName;
    document.getElementById('ecRightName').textContent = right.experimentName;
    document.getElementById('ecLeftMeta').innerHTML = buildMetaHtml(left);
    document.getElementById('ecRightMeta').innerHTML = buildMetaHtml(right);
}

function renderMetricComparison(metrics) {
    const grid = document.getElementById('ecMetricsGrid');
    if (!grid) return;

    grid.innerHTML = metrics.map(m => {
        const leftPct  = m.leftScore  != null ? (m.leftScore  * 100).toFixed(0) : 0;
        const rightPct = m.rightScore != null ? (m.rightScore * 100).toFixed(0) : 0;
        const leftCls  = scoreColorClass(m.leftScore);
        const rightCls = scoreColorClass(m.rightScore);

        return `<div class="ec-metric-card">
            <div class="ec-metric-name">${escHtml(m.metric)}</div>
            <div class="ec-metric-delta-badge">${fmtDelta(m.delta)}</div>
            <div class="ec-metric-bars">
                <div class="ec-metric-side">
                    <span class="ec-metric-side-label">A</span>
                    <div class="ec-bar-track">
                        <div class="ec-bar-fill ec-bar-fill--left" style="width:${leftPct}%"></div>
                    </div>
                    <span class="ec-metric-score ${leftCls}">${fmtScore(m.leftScore)}</span>
                </div>
                <div class="ec-metric-side">
                    <span class="ec-metric-side-label">B</span>
                    <div class="ec-bar-track">
                        <div class="ec-bar-fill ec-bar-fill--right" style="width:${rightPct}%"></div>
                    </div>
                    <span class="ec-metric-score ${rightCls}">${fmtScore(m.rightScore)}</span>
                </div>
            </div>
        </div>`;
    }).join('');
}

function fmtQScore(v, compared) {
    const score = fmtScore(v);
    const cls = scoreColorClass(v);
    let indicator = '';
    if (v != null && compared != null) {
        const diff = v - compared;
        if (diff > 0.005) indicator = ' <i class="fas fa-arrow-up ec-q-up" title="Higher than other run"></i>';
        else if (diff < -0.005) indicator = ' <i class="fas fa-arrow-down ec-q-down" title="Lower than other run"></i>';
    }
    return `<span class="${cls}">${score}</span>${indicator}`;
}

function renderQuestionComparison(questions) {
    const tbody = document.getElementById('ecQuestionsTableBody');
    if (!tbody) return;

    if (!questions || questions.length === 0) {
        tbody.innerHTML = '<tr><td colspan="10" style="text-align:center;padding:2rem;color:var(--color-text-secondary)">No question data available.</td></tr>';
        return;
    }

    tbody.innerHTML = questions.map(q => {
        const L = q.leftScores;
        const R = q.rightScores;
        return `<tr>
            <td>${escHtml(q.externalId)}</td>
            <td title="${escHtml(q.question)}">${escHtml(q.question.length > 80 ? q.question.substring(0, 80) + '…' : q.question)}</td>
            <td class="ec-td-score">${fmtQScore(L.faithfulness, R.faithfulness)}</td>
            <td class="ec-td-score">${fmtQScore(R.faithfulness, L.faithfulness)}</td>
            <td class="ec-td-score">${fmtQScore(L.answerRelevancy, R.answerRelevancy)}</td>
            <td class="ec-td-score">${fmtQScore(R.answerRelevancy, L.answerRelevancy)}</td>
            <td class="ec-td-score">${fmtQScore(L.contextPrecision, R.contextPrecision)}</td>
            <td class="ec-td-score">${fmtQScore(R.contextPrecision, L.contextPrecision)}</td>
            <td class="ec-td-score">${fmtQScore(L.contextRecall, R.contextRecall)}</td>
            <td class="ec-td-score">${fmtQScore(R.contextRecall, L.contextRecall)}</td>
        </tr>`;
    }).join('');
}

function showError(msg) {
    document.getElementById('ecLoading').setAttribute('hidden', true);
    const errEl = document.getElementById('ecError');
    document.getElementById('ecErrorText').textContent = msg;
    errEl.removeAttribute('hidden');
}

// ── Controller ────────────────────────────────────────────────

$(function () {
    const leftId  = window.ecLeftId  || '';
    const rightId = window.ecRightId || '';

    if (!leftId || !rightId) {
        showError('Missing experiment IDs. Please go back and select two experiments to compare.');
        return;
    }

    fetchComparison(leftId, rightId)
        .done(dto => {
            const norm = normalizeComparison(dto);
            renderComparisonHeader(norm.left, norm.right);
            renderMetricComparison(norm.metrics);
            renderQuestionComparison(norm.questions);

            document.getElementById('ecLoading').setAttribute('hidden', true);
            document.getElementById('ecContent').removeAttribute('hidden');
        })
        .fail((xhr) => {
            const status = xhr.status;
            let msg;
            if (status === 400) {
                msg = xhr.responseJSON?.error ?? 'The selected experiments cannot be compared (different subjects or question sets, or identical IDs).';
            } else if (status === 404) {
                msg = 'One or both experiments were not found.';
            } else if (status === 409) {
                msg = 'These experiments are in an incompatible state for comparison.';
            } else {
                msg = xhr.responseJSON?.error ?? `Unexpected error (HTTP ${status}).`;
            }
            showError(msg);
        });
});
