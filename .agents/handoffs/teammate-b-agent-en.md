# Agent Handoff — Teammate B AI Administration And Vietnamese Dataset

Verified: 2026-07-13

## Architecture And Anchors

EduChatAI uses .NET 10 Razor Pages, jQuery, Tailwind/admin CSS, numeric enum JSON, and `[Authorize(Roles = "Admin")]` for current admin pages. Existing admin conventions are anchored in `SubjectManage.cshtml(.cs)`, `_AdminLayout.cshtml`, and `admin-subject-manage.js`. Subject/global AI entities and `AiConfigurationResolver` exist, but there is no admin AI page or experiment UI.

## Authority And Deviations

Repository code is authoritative for the current implementation state. `.agents` records the agreed plan, rationale, ownership boundaries, and contracts. The user's confirmed direction is final for what the project should become.

If a user request deviates from `.agents`, cite the relevant `.agents` file and heading, explain the conflict and likely consequences, and explicitly ask whether the user intends to override the agreed plan before acting. Do not silently fork a contract or cross an ownership boundary. After confirmation, follow the user's direction and identify every affected contract, handoff, ADR, or work-item statement that must be reconciled.

## Assigned Task

Create the Vietnamese 50-question `DB201` dataset, subject AI configuration page, and experiment creation page. Develop the UI fixture-first against owner-provided frozen DTO/interfaces. Render global default, stored override, and effective value separately. Implement reindex, index-compatibility preflight, confirmation, and experiment-create interaction states without implementing AI core behavior.

## Frozen Contracts

Read “Subject AI Configuration,” experiment creation, and “Vietnamese dataset contract” in [three-day-delivery-contracts.md](../contracts/three-day-delivery-contracts.md). Use exact PageModel names, handlers, routes, request/response types, numeric enums, validations, status codes, and dataset metadata. Every save property is present; null removes an override.

Treat these contracts as the agreed baseline. If the owner foundation does not compile against them, or if a user asks to change them, follow the Authority And Deviations protocol. Do not rename or locally fork a contract without confirmed direction.

Before experiment submission, POST the four indexing inputs to `Preflight`. If `BlockingReason` is non-null, show it and do not submit. If compatible, state that the run can start immediately. If reindex is required, show `AffectedDocumentCount` and explain that DB201 chat is temporarily unavailable and the selected configuration remains active afterward. Do not estimate duration. After a `202`, navigate immediately to `/admin/experiments/results?id={ExperimentId}` whether or not reindex is required.

## Exact File Scope

Create only:

- `BusinessLayer/Services/AI/Experiments/Data/db201-vi-50.json`
- `PresentationLayer/Pages/Admin/AiConfiguration.cshtml`
- `PresentationLayer/Pages/Admin/AiConfiguration.cshtml.cs`
- `PresentationLayer/Pages/Admin/Experiments/Create.cshtml`
- `PresentationLayer/Pages/Admin/Experiments/Create.cshtml.cs`
- `PresentationLayer/wwwroot/js/admin/admin-ai-configuration.js`
- `PresentationLayer/wwwroot/js/admin/admin-experiment-create.js`
- `PresentationLayer/wwwroot/css/admin-ai-configuration.css`
- `PresentationLayer/wwwroot/css/admin-experiment-create.css`
- `UnitTests/AdminAiConfigurationPageTests.cs`
- `UnitTests/AdminExperimentCreatePageTests.cs`
- `UnitTests/VietnameseExperimentDatasetTests.cs`

## Prohibited Files

Do not modify entities, Domain contracts, resolver, chunkers, indexer, model/provider factories, experiment runner/evaluator, DbContext, migrations, `Business.csproj`, import/seed wiring, `Program.cs`, `_AdminLayout.cshtml`, chat, reports, experiment results/comparison, or C's pages.

## Fixture-First Work

Use these exact fixtures as mock handler results:

- [subject-ai-configuration.json](../contracts/fixtures/subject-ai-configuration.json)
- [ai-configuration-options.json](../contracts/fixtures/ai-configuration-options.json)
- [subject-reindex-response.json](../contracts/fixtures/subject-reindex-response.json)
- [experiment-create-options.json](../contracts/fixtures/experiment-create-options.json)
- [experiment-index-preflight-request.json](../contracts/fixtures/experiment-index-preflight-request.json)
- [experiment-index-preflight.json](../contracts/fixtures/experiment-index-preflight.json)
- [experiment-create-request.json](../contracts/fixtures/experiment-create-request.json)
- [experiment-create-response.json](../contracts/fixtures/experiment-create-response.json)

Keep data acquisition separate from normalization/rendering so mocked results and live AJAX responses use the same functions. Do not leave production handlers returning fixture data once owner services are available.

## Dataset Requirements

Create JSON matching `TestDatasetDto`. Metadata is exactly `db201-vi-50-v1`, `vi`, and `DB201`. IDs are exactly `DB201-VI-001` through `DB201-VI-050`. Every question and ground truth is non-empty Vietnamese and grounded in the final DB201 demo documents. Own the focused validation tests, but do not edit project embedding, import, seed, or resource wiring.

## Expected Tests

Test Admin authorization metadata, handler-to-service argument mapping, response status mapping, full save serialization including nulls, validation boundaries, inherited/effective rendering data, duplicate reindex prevention, compatible/required/blocked preflight states, affected-document confirmation, distinct question selection, immediate create-response navigation, and dataset schema/count/IDs/language/non-empty fields.

## Verification Commands

```powershell
dotnet test UnitTests/UnitTests.csproj --no-restore --filter "FullyQualifiedName~AdminAiConfigurationPageTests|FullyQualifiedName~AdminExperimentCreatePageTests|FullyQualifiedName~VietnameseExperimentDatasetTests"
dotnet build EduChatAI.slnx --no-restore
git diff --check
```

Manually check desktop/mobile layout, all loading/empty/failure/success states, console, and network requests.

## Expected Final Report

Return one reviewable branch or squashed commit. Report files, exact commands/results, manual checks, dataset grounding method, warnings, and residual risks. Preserve unrelated changes. Do not silently modify frozen contracts; handle any mismatch through the Authority And Deviations protocol.
