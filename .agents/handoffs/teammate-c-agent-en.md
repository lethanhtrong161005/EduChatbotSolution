# Agent Handoff — Teammate C Reports And Experiment Visualization

Verified: 2026-07-13

## Architecture And Current Anchors

EduChatAI is a .NET 10 Razor Pages/jQuery application. Admin pages use `[Authorize(Roles = "Admin")]` and `_AdminLayout.cshtml`. No report service/page or experiment result page exists. The Phase 2 experiment persistence model is implemented and owner-controlled; C must consume frozen DTOs only.

## Authority And Deviations

Repository code is authoritative for the current implementation state. `.agents` records the agreed plan, rationale, ownership boundaries, and contracts. The user's confirmed direction is final for what the project should become.

If a user request deviates from `.agents`, cite the relevant `.agents` file and heading, explain the conflict and likely consequences, and explicitly ask whether the user intends to override the agreed plan before acting. Do not silently fork a contract or cross an ownership boundary. After confirmation, follow the user's direction and identify every affected contract, handoff, ADR, or work-item statement that must be reconciled.

## Assigned Task

Create the Admin adoption-and-health report page, experiment results/history page, and pairwise comparison page. Develop against exact fixtures, then connect to owner service interfaces without changing normalization/rendering.

## Frozen Contracts

Read “Admin Report Dashboard” and experiment results/comparison in [three-day-delivery-contracts.md](../contracts/three-day-delivery-contracts.md). Use exact types, routes, handlers, numeric enums, nullability, and errors as the agreed baseline. Handle any code or user-request conflict through the Authority And Deviations protocol.

Missing token usage is null, not zero. Partial measurement is the known sum plus explicit coverage. Product copy says RAGAS-style and never claims Python RAGAS execution.

Report controls are range, role, and optional trend subject. Trends default to all subjects. `TrendSubjectId` filters only the three daily series; the horizontal subject chart and subject table always retain all subjects. Render prompt/completion token series separately, use null gaps for missing latency, and use the indexing-status donut rather than subject pies. The horizontal chart switches among active users, active sessions, completed generations, and measured tokens.

The experiment results page lists every summary row and polls the selected run through queued, preparing-index, running, evaluating, completed, and failed states. Show indexed/affected documents and completed/total questions. Only completed runs are selectable; enable Compare only for exactly two runs with the same subject and question set. Do not limit stored or displayed history to two runs.

## Exact File Scope

Create only:

- `PresentationLayer/Pages/Admin/Reports.cshtml`
- `PresentationLayer/Pages/Admin/Reports.cshtml.cs`
- `PresentationLayer/Pages/Admin/Experiments/Results.cshtml`
- `PresentationLayer/Pages/Admin/Experiments/Results.cshtml.cs`
- `PresentationLayer/Pages/Admin/Experiments/Compare.cshtml`
- `PresentationLayer/Pages/Admin/Experiments/Compare.cshtml.cs`
- `PresentationLayer/wwwroot/js/admin/admin-reports.js`
- `PresentationLayer/wwwroot/js/admin/admin-experiment-results.js`
- `PresentationLayer/wwwroot/js/admin/admin-experiment-compare.js`
- `PresentationLayer/wwwroot/css/admin-reports.css`
- `PresentationLayer/wwwroot/css/admin-experiment-results.css`
- `UnitTests/AdminReportsPageTests.cs`
- `UnitTests/AdminExperimentResultsPageTests.cs`

## Prohibited Files

Do not modify entities, Domain contracts, DbContext, migrations, `Business.csproj`, import/seed wiring, AI/retrieval/metrics/evaluator/runner services, `Program.cs`, `_AdminLayout.cshtml`, B's pages, B's dataset or dataset tests, or chat files.

## Fixtures

- [admin-report-dashboard.json](../contracts/fixtures/admin-report-dashboard.json)
- [experiment-summaries.json](../contracts/fixtures/experiment-summaries.json)
- [experiment-result.json](../contracts/fixtures/experiment-result.json)
- [experiment-result-preparing-index.json](../contracts/fixtures/experiment-result-preparing-index.json)
- [experiment-comparison.json](../contracts/fixtures/experiment-comparison.json)

Keep AJAX/resource acquisition separate from normalization and rendering. The same render functions must accept fixtures and live DTOs.

## Expected Tests

Cover Admin authorization and range/role/trend-subject service mapping; all report ranges; all-subject trend default; one-subject trend filtering without changing the subject table; zero-filled days; missing/partial/full token coverage; null subject bucket; distinct answer/document citation counts; chart/table normalization; every experiment progress state; exactly-two comparison selection; and comparison validation errors.

## Verification Commands

```powershell
dotnet test UnitTests/UnitTests.csproj --no-restore --filter "FullyQualifiedName~AdminReportsPageTests|FullyQualifiedName~AdminExperimentResultsPageTests"
dotnet build EduChatAI.slnx --no-restore
git diff --check
```

Manually verify desktop/mobile pages, all three ranges, all-subject and DB201 trends, role filtering, null coverage, experiment polling, two-run comparison, browser console, and network failures.

## Expected Final Report

Return one reviewable branch or squashed commit. Report files, exact commands/results, manual checks, warnings, and residual risks. Preserve unrelated changes. Do not silently alter frozen contracts; handle any mismatch through the Authority And Deviations protocol.
