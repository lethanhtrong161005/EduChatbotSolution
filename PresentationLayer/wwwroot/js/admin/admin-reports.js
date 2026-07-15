/**
 * admin-reports.js
 * EduChatAI Admin — Reports Dashboard AJAX + Chart.js rendering
 *
 * Architecture: fetch → normalize → render. The same render functions
 * accept fixture JSON and live DTO responses without modification.
 *
 * Contract: GET /admin/reports?handler=Dashboard&range={0|1|2}&role={0|1|2|3}&trendSubjectId={int?}
 * Response type: AdminReportDashboardDto (camelCase)
 */

'use strict';

// ── Helpers ───────────────────────────────────────────────────

/** Escape user/server text for safe DOM insertion. */
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
 * Format a nullable number value for a KPI card.
 * Returns the formatted string or '—' when value is null.
 */
function fmtKpi(value, suffix = '') {
    if (value == null) return '—';
    return Number(value).toLocaleString() + suffix;
}

/**
 * Format a nullable percent.
 * Returns "N/A" for null, formatted percent otherwise.
 */
function fmtPct(value) {
    if (value == null) return 'N/A';
    return value.toFixed(1) + '%';
}

/** Format milliseconds to a human-readable latency string. */
function fmtMs(value) {
    if (value == null) return '—';
    return Number(value).toLocaleString() + ' ms';
}

/**
 * Format token measurement for display.
 * Returns "Unmeasured" when eligible > 0 but measured = 0 (all null).
 * Returns "N/A" for coverage when tokens are null.
 */
function fmtTokenCoverage(tm) {
    if (!tm) return '—';
    if (tm.eligibleMessageCount === 0) return '100% (no activity)';
    if (tm.measuredMessageCount === 0) return 'Unmeasured';
    return tm.coveragePercent.toFixed(1) + '%';
}

/** Build a readable token detail line. */
function buildTokenDetailText(tm) {
    if (!tm || tm.eligibleMessageCount === 0) return null;
    if (tm.measuredMessageCount === 0) {
        return `Token usage not measured for any of the ${tm.eligibleMessageCount} eligible generations. Coverage: 0%.`;
    }
    const prompt = tm.measuredPromptTokens != null ? tm.measuredPromptTokens.toLocaleString() : 'N/A';
    const completion = tm.measuredCompletionTokens != null ? tm.measuredCompletionTokens.toLocaleString() : 'N/A';
    return `Prompt: ${prompt} | Completion: ${completion} | Measured: ${tm.measuredMessageCount}/${tm.eligibleMessageCount} generations (${tm.coveragePercent.toFixed(1)}%).`;
}

// ── Chart.js instances (module-level) ────────────────────────

let chartGenerations = null;
let chartTokens = null;
let chartLatency = null;
let chartSubjectBar = null;
let chartIndexingDonut = null;

/**
 * True when the Chart.js library is available on the page.
 * Chart.js is loaded from a CDN; if that request is blocked or offline,
 * `Chart` is undefined. Render functions must bail gracefully so the page
 * (KPIs, tables) still works instead of throwing a ReferenceError.
 */
let _chartMissingWarned = false;
function ensureChart() {
    if (typeof Chart !== 'undefined') return true;
    if (!_chartMissingWarned) {
        console.warn('[admin-reports] Chart.js is not loaded; charts will be skipped.');
        _chartMissingWarned = true;
    }
    return false;
}

// Shared chart default overrides
function applyChartDefaults() {
    if (typeof Chart === 'undefined') return;
    Chart.defaults.font.family = 'Inter, -apple-system, sans-serif';
    Chart.defaults.font.size = 12;
    Chart.defaults.color = '#6b7280';
    Chart.defaults.plugins.legend.display = false;
    Chart.defaults.plugins.tooltip.backgroundColor = '#1f2937';
    Chart.defaults.plugins.tooltip.titleColor = '#f9fafb';
    Chart.defaults.plugins.tooltip.bodyColor = '#d1d5db';
    Chart.defaults.plugins.tooltip.borderColor = '#374151';
    Chart.defaults.plugins.tooltip.borderWidth = 1;
    Chart.defaults.plugins.tooltip.cornerRadius = 6;
    Chart.defaults.plugins.tooltip.padding = 10;
}

function makeLineChartConfig(labels, datasets, opts = {}) {
    return {
        type: 'line',
        data: { labels, datasets },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            scales: {
                x: {
                    grid: { display: false },
                    ticks: { maxRotation: 30 },
                },
                y: {
                    beginAtZero: true,
                    grid: { color: 'rgba(0,0,0,0.05)' },
                    ...opts.yAxis,
                },
            },
            plugins: {
                legend: { display: opts.showLegend ?? false },
                tooltip: {
                    callbacks: opts.tooltipCallbacks ?? {},
                },
            },
        },
    };
}

// ── Data layer ────────────────────────────────────────────────

/**
 * Fetch the dashboard DTO from the Razor Page handler.
 * @returns {Promise<object>} Raw AdminReportDashboardDto (camelCase).
 */
function fetchDashboard(range, role, trendSubjectId) {
    const params = new URLSearchParams({ handler: 'Dashboard', range, role });
    if (trendSubjectId !== null && trendSubjectId !== '') {
        params.set('trendSubjectId', trendSubjectId);
    }
    return $.ajax({
        url: '?' + params.toString(),
        method: 'GET',
        dataType: 'json',
    });
}

// ── Normalization layer ───────────────────────────────────────

/**
 * Transform raw DTO into chart-ready structures.
 * Token series use null where unmeasured (Chart.js spanGaps:false renders a gap).
 */
function normalizeDashboard(dto) {
    // Daily labels (date strings "YYYY-MM-DD")
    const genLabels = dto.assistantGenerations.map(d => d.date);
    const genSeries = dto.assistantGenerations.map(d => d.completedAssistantGenerationCount);

    const tokenLabels = dto.tokenUsage.map(d => d.date);
    const promptSeries = dto.tokenUsage.map(d => {
        const tm = d.tokenMeasurement;
        return tm.eligibleMessageCount === 0 ? 0 :
            tm.measuredMessageCount === 0 ? null :
                tm.measuredPromptTokens;
    });
    const completionSeries = dto.tokenUsage.map(d => {
        const tm = d.tokenMeasurement;
        return tm.eligibleMessageCount === 0 ? 0 :
            tm.measuredMessageCount === 0 ? null :
                tm.measuredCompletionTokens;
    });

    const latencyLabels = dto.responseLatency.map(d => d.date);
    const latencySeries = dto.responseLatency.map(d => d.p95TotalResponseTimeMs); // null = no data

    return {
        kpis: dto.kpis,
        tokenMeasurement: dto.kpis.tokenMeasurement,
        trendSubjectOptions: dto.trendSubjectOptions,
        trendSubjectId: dto.trendSubjectId,
        range: dto.range,
        generationsTrend: { labels: genLabels, values: genSeries },
        tokensTrend: { labels: tokenLabels, prompt: promptSeries, completion: completionSeries },
        latencyTrend: { labels: latencyLabels, values: latencySeries },
        subjectUsage: dto.subjectUsage,
        subjectsNeedingAttention: dto.subjectsNeedingAttention,
        indexingStatus: dto.indexingStatus,
        hotDocuments: dto.hotDocuments,
    };
}

// ── Render layer ──────────────────────────────────────────────

function renderKpis(kpis) {
    const tm = kpis.tokenMeasurement;

    $('#kpiActiveUsersVal').text(fmtKpi(kpis.uniqueActiveUserCount));
    $('#kpiActiveSessionsVal').text(fmtKpi(kpis.activeSessionCount));
    $('#kpiGenerationsVal').text(fmtKpi(kpis.completedAssistantGenerationCount));
    $('#kpiTokenCoverageVal').text(fmtTokenCoverage(tm));
    $('#kpiSuccessRateVal').text(fmtPct(kpis.generationSuccessRatePercent));
    $('#kpiCitationCoverageVal').text(fmtPct(kpis.citationCoveragePercent));
    $('#kpiLatencyVal').text(fmtMs(kpis.p95TotalResponseTimeMs));

    // Token detail banner
    const detail = buildTokenDetailText(tm);
    if (detail) {
        $('#rpTokenDetailText').text(detail);
        $('#rpTokenDetail').removeAttr('hidden');
    } else {
        $('#rpTokenDetail').attr('hidden', true);
    }
}

function renderTrendSubjectOptions(options, currentTrendSubjectId) {
    const sel = $('#trendSubjectSelect');
    const currentVal = sel.val(); // preserve current selection if already set
    sel.find('option:not(:first)').remove();
    sel.append($('<option>').val(0).text('Flexible subjects'));
    options.forEach(opt => {
        sel.append($('<option>').val(opt.subjectId).text(`${opt.subjectCode} — ${opt.subjectName}`));
    });
    // Restore current selection or use what the DTO reports
    const target = currentVal || (currentTrendSubjectId != null ? String(currentTrendSubjectId) : '');
    sel.val(target);
}

function updateTrendSubtitle(trendSubjectId, options) {
    let subtitle = '(all subjects)';
    if (trendSubjectId === 0) {
        subtitle = '(flexible subjects)';
    } else if (trendSubjectId != null) {
        const opt = options.find(o => o.subjectId === trendSubjectId);
        if (opt) subtitle = `(${opt.subjectCode} — ${opt.subjectName})`;
    }
    $('#rpTrendSubtitle').text(subtitle);
}

function renderGenerationsChart(trend) {
    const ctx = document.getElementById('chartGenerations');
    if (!ctx || !ensureChart()) return;
    const config = makeLineChartConfig(
        trend.labels,
        [{
            label: 'Generations',
            data: trend.values,
            borderColor: '#3498db',
            backgroundColor: 'rgba(52,152,219,0.08)',
            fill: true,
            tension: 0.35,
            pointRadius: 3,
            pointHoverRadius: 5,
        }],
        { yAxis: { ticks: { precision: 0 } } }
    );
    if (chartGenerations) {
        chartGenerations.data.labels = trend.labels;
        chartGenerations.data.datasets[0].data = trend.values;
        chartGenerations.update('none');
    } else {
        chartGenerations = new Chart(ctx, config);
    }
}

function renderTokensChart(trend) {
    const ctx = document.getElementById('chartTokens');
    if (!ctx || !ensureChart()) return;
    const config = makeLineChartConfig(
        trend.labels,
        [
            {
                label: 'Prompt',
                data: trend.prompt,
                borderColor: '#9b59b6',
                backgroundColor: 'rgba(155,89,182,0.08)',
                fill: false,
                tension: 0.35,
                spanGaps: false,
                pointRadius: 3,
                pointHoverRadius: 5,
            },
            {
                label: 'Completion',
                data: trend.completion,
                borderColor: '#2ecc71',
                backgroundColor: 'rgba(46,204,113,0.08)',
                fill: false,
                tension: 0.35,
                spanGaps: false,
                pointRadius: 3,
                pointHoverRadius: 5,
            },
        ],
        {
            showLegend: true,
            yAxis: { ticks: { precision: 0 } },
            tooltipCallbacks: {
                label: ctx => {
                    if (ctx.parsed.y == null) return `${ctx.dataset.label}: Unmeasured`;
                    return `${ctx.dataset.label}: ${ctx.parsed.y.toLocaleString()}`;
                },
            },
        }
    );
    if (chartTokens) {
        chartTokens.data.labels = trend.labels;
        chartTokens.data.datasets[0].data = trend.prompt;
        chartTokens.data.datasets[1].data = trend.completion;
        chartTokens.update('none');
    } else {
        chartTokens = new Chart(ctx, config);
        // Show legend for this chart specifically
        chartTokens.options.plugins.legend.display = true;
    }
}

function renderLatencyChart(trend) {
    const ctx = document.getElementById('chartLatency');
    if (!ctx || !ensureChart()) return;
    const config = makeLineChartConfig(
        trend.labels,
        [{
            label: 'p95 (ms)',
            data: trend.values,
            borderColor: '#e74c3c',
            backgroundColor: 'rgba(231,76,60,0.08)',
            fill: true,
            tension: 0.35,
            spanGaps: false, // null values appear as gaps (no data days)
            pointRadius: 3,
            pointHoverRadius: 5,
        }],
        {
            yAxis: {
                ticks: {
                    callback: v => v == null ? '' : v.toLocaleString() + ' ms',
                },
            },
            tooltipCallbacks: {
                label: ctx => ctx.parsed.y == null
                    ? 'p95: No data'
                    : `p95: ${ctx.parsed.y.toLocaleString()} ms`,
            },
        }
    );
    if (chartLatency) {
        chartLatency.data.labels = trend.labels;
        chartLatency.data.datasets[0].data = trend.values;
        chartLatency.update('none');
    } else {
        chartLatency = new Chart(ctx, config);
    }
}

function renderSubjectBarChart(subjectUsage, metric) {
    const ctx = document.getElementById('chartSubjectBar');
    if (!ctx || !ensureChart()) return;

    // Include the flexible-subject bucket in the cross-subject comparison;
    // include all entries as this chart always shows all subjects
    const labels = subjectUsage.map(s => s.subjectCode || s.subjectName);
    const values = subjectUsage.map(s => {
        if (metric === 'measuredTotalTokens') {
            return s.tokenMeasurement.measuredTotalTokens;
        }
        return s[metric] ?? 0;
    });

    const colors = [
        '#3498db', '#2ecc71', '#9b59b6', '#f39c12', '#e74c3c',
        '#1abc9c', '#e67e22', '#2980b9', '#27ae60', '#8e44ad',
    ];

    if (chartSubjectBar) {
        chartSubjectBar.data.labels = labels;
        chartSubjectBar.data.datasets[0].data = values;
        chartSubjectBar.data.datasets[0].backgroundColor = colors.slice(0, labels.length);
        chartSubjectBar.update('none');
    } else {
        chartSubjectBar = new Chart(ctx, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: metric,
                    data: values,
                    backgroundColor: colors.slice(0, labels.length),
                    borderRadius: 4,
                    borderSkipped: false,
                }],
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    x: {
                        beginAtZero: true,
                        grid: { color: 'rgba(0,0,0,0.05)' },
                        ticks: { precision: 0 },
                    },
                    y: { grid: { display: false } },
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => {
                                const v = ctx.parsed.x;
                                return v == null ? 'N/A' : v.toLocaleString();
                            },
                        },
                    },
                },
            },
        });
    }
}

function coverageClass(coveragePercent, measuredCount) {
    if (measuredCount === 0) return 'rp-coverage--unknown';
    if (coveragePercent >= 90) return 'rp-coverage--full';
    if (coveragePercent >= 50) return 'rp-coverage--partial';
    return 'rp-coverage--none';
}

function coverageLabel(tm) {
    if (tm.eligibleMessageCount === 0) return '<span class="rp-coverage rp-coverage--full">100%</span>';
    if (tm.measuredMessageCount === 0) return '<span class="rp-coverage rp-coverage--unknown">Unmeasured</span>';
    const cls = coverageClass(tm.coveragePercent, tm.measuredMessageCount);
    return `<span class="rp-coverage ${cls}">${tm.coveragePercent.toFixed(1)}%</span>`;
}

function fmtNullableNum(v) {
    return v != null ? v.toLocaleString() : '<span style="color:var(--color-text-secondary)">—</span>';
}

function renderSubjectTable(subjectUsage) {
    const tbody = document.getElementById('subjectTableBody');
    if (!tbody) return;
    if (!subjectUsage || subjectUsage.length === 0) {
        tbody.innerHTML = '<tr class="rp-table-empty"><td colspan="8">No subject data.</td></tr>';
        return;
    }
    tbody.innerHTML = subjectUsage.map(s => {
        const tm = s.tokenMeasurement;
        return `<tr>
            <td>${escHtml(s.subjectCode ? `${s.subjectCode} — ${s.subjectName}` : s.subjectName)}</td>
            <td class="rp-td-num">${s.uniqueActiveUserCount.toLocaleString()}</td>
            <td class="rp-td-num">${s.activeSessionCount.toLocaleString()}</td>
            <td class="rp-td-num">${s.completedAssistantGenerationCount.toLocaleString()}</td>
            <td class="rp-td-num">${fmtNullableNum(tm.measuredPromptTokens)}</td>
            <td class="rp-td-num">${fmtNullableNum(tm.measuredCompletionTokens)}</td>
            <td class="rp-td-num">${fmtNullableNum(tm.measuredTotalTokens)}</td>
            <td class="rp-td-num">${coverageLabel(tm)}</td>
        </tr>`;
    }).join('');
}

function rateClass(rate) {
    if (rate >= 15) return 'rp-rate--high';
    if (rate >= 5) return 'rp-rate--medium';
    return 'rp-rate--low';
}

function renderHealthTable(subjects) {
    const tbody = document.getElementById('healthTableBody');
    if (!tbody) return;
    if (!subjects || subjects.length === 0) {
        tbody.innerHTML = '<tr class="rp-table-empty"><td colspan="5">No subjects need attention.</td></tr>';
        return;
    }
    tbody.innerHTML = subjects.map(s => `<tr>
        <td>${escHtml(s.subjectCode)} — ${escHtml(s.subjectName)}</td>
        <td class="rp-td-num">${s.completedAssistantGenerationCount.toLocaleString()}</td>
        <td class="rp-td-num"><span class="${rateClass(s.noContextRatePercent)}">${s.noContextRatePercent.toFixed(1)}%</span></td>
        <td class="rp-td-num"><span class="${rateClass(s.generationFailureRatePercent)}">${s.generationFailureRatePercent.toFixed(1)}%</span></td>
        <td class="rp-td-num">${fmtMs(s.p95TotalResponseTimeMs)}</td>
    </tr>`).join('');
}

function renderHotDocuments(docs) {
    const tbody = document.getElementById('hotDocsTableBody');
    if (!tbody) return;
    if (!docs || docs.length === 0) {
        tbody.innerHTML = '<tr class="rp-table-empty"><td colspan="3">No document citations recorded.</td></tr>';
        return;
    }
    tbody.innerHTML = docs.map(d => `<tr>
        <td>${escHtml(d.documentTitle)}</td>
        <td>${escHtml(d.subjectCode)}</td>
        <td class="rp-td-num"><strong>${d.citedAnswerCount}</strong></td>
    </tr>`).join('');
}

function renderIndexingDonut(status) {
    const ctx = document.getElementById('chartIndexingDonut');
    const labels = ['Indexed', 'Processing', 'Failed'];
    const values = [status.indexedDocumentCount, status.processingDocumentCount, status.failedDocumentCount];
    const colors = ['#2ecc71', '#3498db', '#e74c3c'];

    // Guard chart rendering; the custom legend below still renders without Chart.js.
    if (ctx && ensureChart()) {
        if (chartIndexingDonut) {
            chartIndexingDonut.data.datasets[0].data = values;
            chartIndexingDonut.update('none');
        } else {
            chartIndexingDonut = new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels,
                    datasets: [{
                        data: values,
                        backgroundColor: colors,
                        borderWidth: 0,
                        hoverOffset: 6,
                    }],
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: true,
                    cutout: '65%',
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                // Recompute total from the live dataset so percentages stay
                                // correct after update() replaces the data (do not close over
                                // a `total` captured at first construction).
                                label: ctx => {
                                    const v = ctx.parsed;
                                    const data = ctx.dataset?.data ?? [];
                                    const total = data.reduce((sum, n) => sum + (Number(n) || 0), 0);
                                    const pct = total > 0 ? ((v / total) * 100).toFixed(1) : '0.0';
                                    return `${ctx.label}: ${v} (${pct}%)`;
                                },
                            },
                        },
                    },
                },
            });
        }
    }

    // Update custom legend
    const legend = document.getElementById('rpDonutLegend');
    if (legend) {
        legend.innerHTML = labels.map((lbl, i) => `
            <div class="rp-donut-legend-item">
                <span class="rp-donut-legend-swatch" style="background:${colors[i]}"></span>
                <span>${lbl}</span>
                <span class="rp-donut-legend-count">${values[i]}</span>
            </div>
        `).join('');
    }
}

/** Render all chart and table components from normalized data. */
function renderAll(normalized, currentSubjectMetric) {
    renderKpis(normalized.kpis);
    renderTrendSubjectOptions(normalized.trendSubjectOptions, normalized.trendSubjectId);
    updateTrendSubtitle(normalized.trendSubjectId, normalized.trendSubjectOptions);
    renderGenerationsChart(normalized.generationsTrend);
    renderTokensChart(normalized.tokensTrend);
    renderLatencyChart(normalized.latencyTrend);
    renderSubjectBarChart(normalized.subjectUsage, currentSubjectMetric);
    renderSubjectTable(normalized.subjectUsage);
    renderHealthTable(normalized.subjectsNeedingAttention);
    renderHotDocuments(normalized.hotDocuments);
    renderIndexingDonut(normalized.indexingStatus);
}

// ── Controller ────────────────────────────────────────────────

$(function () {
    applyChartDefaults();

    let currentSubjectMetric = 'uniqueActiveUserCount';
    let currentNormalized = null;

    // Concurrency generation: guards against out-of-order AJAX responses when the
    // user changes range/role/trend faster than the network responds. Each load
    // establishes a new generation; a stale response is dropped before rendering.
    let _reportReqId = 0;

    function getParams() {
        return {
            range: parseInt($('#rangeSelect').val(), 10),
            role: parseInt($('#roleSelect').val(), 10),
            trendSubjectId: $('#trendSubjectSelect').val() || null,
        };
    }

    function showLoading() {
        $('#rpStatus').removeAttr('hidden');
        $('#rpStatusText').text('Loading…');
    }

    function hideLoading() {
        $('#rpStatus').attr('hidden', true);
    }

    function showError(msg) {
        hideLoading();
        // Prepend a dismissible error banner
        const err = $('<div class="rp-error"><i class="fas fa-exclamation-triangle"></i><span></span></div>');
        err.find('span').text(msg);
        $('.rp-page').prepend(err);
        setTimeout(() => err.fadeOut(() => err.remove()), 6000);
    }

    /** Full reload — fetches and renders everything. */
    function loadDashboard() {
        const p = getParams();
        const reqId = ++_reportReqId;
        showLoading();
        fetchDashboard(p.range, p.role, p.trendSubjectId)
            .done(dto => {
                if (reqId !== _reportReqId) return; // superseded by a newer load
                currentNormalized = normalizeDashboard(dto);
                renderAll(currentNormalized, currentSubjectMetric);
                hideLoading();
            })
            .fail((xhr, status, err) => {
                if (reqId !== _reportReqId) return; // stale failure; a newer load owns the UI
                showError(`Failed to load dashboard: ${xhr.responseJSON?.error ?? err}`);
            });
    }

    /**
     * Trend-only reload — when only the trend subject changes,
     * only the three daily charts need updating.
     * The subject chart/table always retains all subjects.
     */
    function loadTrendOnly() {
        const p = getParams();
        const reqId = ++_reportReqId;
        showLoading();
        fetchDashboard(p.range, p.role, p.trendSubjectId)
            .done(dto => {
                if (reqId !== _reportReqId) return; // superseded by a newer load
                const norm = normalizeDashboard(dto);
                // Update trend charts only
                renderGenerationsChart(norm.generationsTrend);
                renderTokensChart(norm.tokensTrend);
                renderLatencyChart(norm.latencyTrend);
                updateTrendSubtitle(norm.trendSubjectId, norm.trendSubjectOptions);
                // KPI and tables stay from full load
                hideLoading();
            })
            .fail((xhr, status, err) => {
                if (reqId !== _reportReqId) return; // stale failure; a newer load owns the UI
                showError(`Failed to update trends: ${xhr.responseJSON?.error ?? err}`);
            });
    }

    // Bind range and role controls — full reload
    $('#rangeSelect, #roleSelect').on('change', loadDashboard);

    // Bind trend-subject control — trend-only reload
    $('#trendSubjectSelect').on('change', loadTrendOnly);

    // Bind subject metric selector — update chart without fetch
    $('#subjectMetricSelect').on('change', function () {
        currentSubjectMetric = $(this).val();
        if (currentNormalized) {
            renderSubjectBarChart(currentNormalized.subjectUsage, currentSubjectMetric);
        }
    });

    // Initial load
    loadDashboard();
});
