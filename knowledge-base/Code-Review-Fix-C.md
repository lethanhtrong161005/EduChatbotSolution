# Code-Review Fix — Teammate C (Reports & Experiment Visualization)

**Author:** Teammate C's coding agent
**Date:** 2026-07-14
**Source review:** [`knowledge-base/Code-Review.md`](./Code-Review.md)
**Scope:** C-owned Reports / Experiment results / Experiment comparison pages, per
`.agents/handoffs/teammate-c-agent-en.md` → "Exact File Scope".

This document records the process used to resolve the review comments (B1–B6), the root cause
verified in code for each comment, and the exact change applied.

---

## Process followed

- **B1 — Re-read the feature.** Re-read the C handoff, WI-006, the frozen contracts (§2 Admin
  Report Dashboard, §3 Experiments), and every C-owned file (`Reports*`, `Results*`, `Compare*`,
  the three `admin-*.js`, the two `admin-*.css`, and the two test files).
- **B2 — Detailed plan.** Mapped each of the 10 review comments to the exact code location and a
  fix confined to C-owned files.
- **B3 — Checked plan vs review + feature.** Confirmed every fix matches the review intent and the
  frozen contract (no handler/route/DTO/enum renames; documented error codes respected). Confirmed
  exception types actually present in the repo: `EntityNotFoundException` (→404) and
  `EntityConstraintException` (→409) — there is no `EntityConflictException`.
- **B4 — Implemented** the changes below.
- **B5 — Tests.** Added 3 handler tests; full C suite passes (45/45).
- **B6 — This document.**

---

## Ownership note — CHUNG #1 was a user-authorized override

Comment **CHUNG #1** ("Admin layout missing the new pages") requires editing
`PresentationLayer/Pages/Admin/_AdminLayout.cshtml`, which is an **owner-only** file:

- Handoff "Prohibited Files": *do not modify `_AdminLayout.cshtml`.*
- WI-006 "File Collision Policy": *owner alone edits `_AdminLayout.cshtml`; owner adds navigation
  after both branches integrate.*
- Contract "Integration Sequence" step 6: owner alone edits `_AdminLayout.cshtml`.

Per the Authority-And-Deviations protocol the conflict was raised and **the user explicitly chose
to override** the boundary. The nav links were therefore added. **The owner must be told at
integration time**, because:

1. This edit will likely **merge-conflict** with the owner's own navigation/DI integration step.
2. The two new nav targets (`/Admin/Reports`, `/Admin/Experiments/Results`) depend on **owner-only
   DI registration** of `IAdminReportService` / `IExperimentService` in `Program.cs`. Until that is
   registered the links compile and route but will fail at runtime (500). That DI wiring remains
   outside C's scope.

The following agreed statements are now out of sync and should be reconciled by the owner:
WI-006 "File Collision Policy" and contract Integration-Sequence step 6. `.agents` was **not**
edited (no authorization to change agreed records).

---

## Fixes applied

### CHUNG #2 — Dark cards on a light page (unreadable)

**Root cause.** `colors.css` flips `--color-surface`, `--color-text-*`, `--color-border-*` to dark
values under `@media (prefers-color-scheme: dark)`. C's pages consume those tokens, but the admin
shell stays light — so on a dark-OS machine cards rendered dark while the page background stayed
light (dark text on dark card).

**Fix.** Pin the tokens to their light values on the page-scope selectors. A class selector
out-specifies `:root`, and custom properties inherit, so the whole page subtree stays light
regardless of OS theme — matching the other light-mode admin pages.

`admin-reports.css` (`.rp-page`) and `admin-experiment-results.css` (`.er-page, .ec-page`):

```css
.rp-page {                 /* .er-page, .ec-page in the experiment CSS */
    --color-surface: #ffffff;
    --color-surface-alt: #f9fafb;
    --color-surface-hover: #f3f4f6;
    --color-card-bg: #ffffff;
    --color-text-primary: #111827;
    --color-text-secondary: #6b7280;
    --color-border-light: #e5e7eb;
    --color-border-neutral: #d1d5db;
}
```

### CHUNG #3 — Missing AJAX concurrency guard

**Root cause.** `loadDashboard`/`loadTrendOnly` (reports) and `openDetail`/`loadSummaries`
(results) had no request-generation guard; a slow earlier response could overwrite a newer one.

**Fix.** Monotonic generation counters; a response older than the current generation is dropped
before it renders (matches AGENTS.md "new concurrency generation before returning" and the
`_currentRequestId` pattern from the review).

```js
// admin-reports.js
let _reportReqId = 0;
function loadDashboard() {
    const reqId = ++_reportReqId;
    fetchDashboard(...).done(dto => {
        if (reqId !== _reportReqId) return; // superseded by a newer load
        ...
    });
}
```

`admin-experiment-results.js` adds `_detailReqId` (guards `openDetail`) and `_summariesReqId`
(guards `loadSummaries`). Polling already guarded via `_pollingId`.

### REPORTS #1 — `Chart` undefined → JavaScript crash

**Root cause.** `Reports.cshtml` loaded Chart.js from a CDN with a **malformed Subresource
Integrity hash** (`sha256-…5pY=`, 43 chars; a valid SHA-256 SRI is 44). The browser blocked the
script, `Chart` was undefined, and `new Chart(...)` threw — taking down the whole dashboard. Render
functions did not guard `typeof Chart`.

**Fix (two layers):**

1. `Reports.cshtml` — removed the invalid `integrity`/`crossorigin` so the CDN script executes:

```html
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.3/dist/chart.umd.min.js"></script>
```

2. `admin-reports.js` — a guard so a missing Chart degrades gracefully (KPIs/tables still render):

```js
let _chartMissingWarned = false;
function ensureChart() {
    if (typeof Chart !== 'undefined') return true;
    if (!_chartMissingWarned) { console.warn('[admin-reports] Chart.js is not loaded; charts will be skipped.'); _chartMissingWarned = true; }
    return false;
}
// each render fn: if (!ctx || !ensureChart()) return;
```

> **Residual risk / SRI trade-off:** removing the bogus hash drops Subresource Integrity for this
> CDN script (a *wrong* hash guaranteed the script never loaded, which is strictly worse). The
> owner may later vendor Chart.js locally under `wwwroot/lib` or restore a *verified* SRI hash —
> both are outside C's declared file scope.

### RESULTS #2 — Detail progress lost while polling

**Root cause.** `openDetail()` manually flattened `completedQ/totalQ/indexedDocs/affectedDocs` onto
the summary, but `startPolling()` re-normalizes via `normalizeResult` → `normalizeSummary`, which
did **not** set `completedQ/totalQ`. So the progress stepper showed `undefined/undefined` during
polling.

**Fix.** Moved the flattening into `normalizeSummary()` (single source of truth) and removed the
redundant block from `openDetail()`:

```js
function normalizeSummary(s) {
    return {
        ...,
        progress: `${s.completedQuestionCount}/${s.totalQuestionCount}`,
        completedQ: s.completedQuestionCount,   // survives polling
        totalQ: s.totalQuestionCount,
        indexedDocs: s.indexedDocumentCount,
        affectedDocs: s.affectedDocumentCount,
    };
}
```

### RESULTS #3 — Score row stayed visible for non-completed runs

**Root cause.** `renderExperimentDetail` removed `hidden` from `#erScoreRow` for Completed runs
(via `renderAggregateScores`) but never restored it. Viewing a Running/Failed run after a Completed
one left stale scores visible.

**Fix.** Re-hide the row when the run is not Completed:

```js
if (s.status === ExperimentStatus.Completed) {
    renderAggregateScores(s.scores);
} else {
    const scoreRow = document.getElementById('erScoreRow');
    if (scoreRow) scoreRow.setAttribute('hidden', true);
}
```

### RESULTS #4 — `OnGetDashboardAsync` must return 404 for an unknown subject

**Root cause.** The handler did not catch `EntityNotFoundException`; an unknown `trendSubjectId`
fell through to the HTML error middleware instead of a JSON 404 (AJAX callers need parseable JSON).

**Fix.** `Reports.cshtml.cs` (contract documents 400/404 for this handler — no 409 added):

```csharp
try
{
    var dashboard = await _reportService.GetDashboardAsync(range, role, trendSubjectId, cxlTkn);
    return new JsonResult(dashboard);
}
catch (EntityNotFoundException ex)
{
    return NotFound(new { error = ex.Message });
}
```

### COMPARE #1 — `OnGetComparisonAsync` did not handle 409

**Root cause.** No try/catch; `EntityNotFoundException`/`EntityConstraintException` fell through to
HTML middleware. The contract documents both 404 and 409 for this handler.

**Fix.** `Compare.cshtml.cs` (note: `PageModel` has no `Conflict()` helper, so
`ConflictObjectResult` is used directly):

```csharp
try
{
    var comparison = await _experimentService.CompareAsync(leftId, rightId, cxlTkn);
    if (comparison is null)
        return NotFound(new { error = "One or both experiments were not found." });
    return new JsonResult(comparison);
}
catch (EntityNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
catch (EntityConstraintException ex)    { return new ConflictObjectResult(new { error = ex.Message }); }
```

### COMPARE #2 — Non-functional "Select All" checkbox

**Root cause.** `Results.cshtml` rendered `#erSelectAll`, but no JS bound it, and comparison is
strictly pairwise (exactly two runs) so a select-all has no valid use.

**Fix.** Removed the checkbox; kept an empty header cell for column alignment (colspan stays 14):

```html
<th class="er-th-check" aria-label="Select"></th>
```

### COMPARE #3 — Donut tooltip used a stale `total`

**Root cause.** `renderIndexingDonut` closed over `total` computed at first `new Chart()`. On
subsequent `update()` the data changed but the captured `total` did not, so tooltip percentages
became wrong.

**Fix.** Recompute `total` inside the tooltip callback from the live dataset:

```js
label: ctx => {
    const v = ctx.parsed;
    const data = ctx.dataset?.data ?? [];
    const total = data.reduce((sum, n) => sum + (Number(n) || 0), 0);
    const pct = total > 0 ? ((v / total) * 100).toFixed(1) : '0.0';
    return `${ctx.label}: ${v} (${pct}%)`;
}
```

---

## Tests added (B5)

| Test | File | Asserts |
|---|---|---|
| `OnGetDashboardAsync_SubjectNotFound_Returns404` | `UnitTests/AdminReportsPageTests.cs` | service throws `EntityNotFoundException` → `NotFoundObjectResult` |
| `OnGetComparisonAsync_ServiceThrowsNotFound_Returns404` | `UnitTests/AdminExperimentResultsPageTests.cs` | `EntityNotFoundException` → `NotFoundObjectResult` |
| `OnGetComparisonAsync_ServiceThrowsConstraint_Returns409` | `UnitTests/AdminExperimentResultsPageTests.cs` | `EntityConstraintException` → `ConflictObjectResult` |

JS changes have no unit harness in this repo → covered by the manual browser checks below.

---

## Verification results

```
dotnet build EduChatAI.slnx --no-restore           → 0 Errors (pre-existing warnings only, not in C files)
dotnet test … AdminReportsPageTests|AdminExperimentResultsPageTests
                                                    → Passed! Failed: 0, Passed: 45, Total: 45
git diff --check                                    → clean (LF/CRLF informational only)
```

Files changed (all C-owned except the authorized `_AdminLayout.cshtml`):
`Reports.cshtml`, `Reports.cshtml.cs`, `Experiments/Results.cshtml`, `Experiments/Compare.cshtml.cs`,
`_AdminLayout.cshtml`, `admin-reports.js`, `admin-experiment-results.js`, `admin-reports.css`,
`admin-experiment-results.css`, `AdminReportsPageTests.cs`, `AdminExperimentResultsPageTests.cs`.

### Manual checks still recommended (browser, desktop + mobile)

- Reports page renders with **no console crash** in both OS light and dark mode; cards are light and
  readable.
- Rapid range/role/trend switches never show stale data; donut tooltip percentages correct after a
  reload.
- Experiment detail progress counts persist while polling; opening a Running run after a Completed
  run hides the score row; the "Select All" header cell is gone.
- Compare surfaces friendly 404 / 409 messages.

---

## Residual risks

1. **Chart.js SRI removed** — see REPORTS #1 note; owner may vendor locally or add a verified hash.
2. **Owner-only DI** for `IAdminReportService` / `IExperimentService` must be registered in
   `Program.cs` or the new nav links 500 at runtime.
3. **`_AdminLayout.cshtml` merge conflict** likely with the owner's integration step (CHUNG #1
   override).
