# Agent Handoff — Teammate A Chat Reliability And Variants

Verified: 2026-07-13

## Architecture

EduChatAI is .NET 10 with layered Domain/DataAccess/Business/Presentation projects, Razor Pages, jQuery, Hangfire, SignalR, EF Core, and NUnit. Preserve dependency direction and the current event/state client conventions. Repository code is authoritative.

The owner foundation from [ADR 0010](../adr/0010-chat-turns-and-assistant-variants.md) is implemented. Confirm it builds before beginning; do not replace its entity, persistence, migration, atomic exchange, or `MessageIndex` history behavior.

## Authority And Deviations

Repository code is authoritative for the current implementation state. `.agents` records the agreed plan, rationale, ownership boundaries, and contracts. The user's confirmed direction is final for what the project should become.

If a user request deviates from `.agents`, cite the relevant `.agents` file and heading, explain the conflict and likely consequences, and explicitly ask whether the user intends to override the agreed plan before acting. Do not silently fork a contract or cross an ownership boundary. After confirmation, follow the user's direction and identify every affected contract, handoff, ADR, or work-item statement that must be reconciled.

## Current Anchors

- `ChatPersistenceService.GetMessagesBySessionAsync` returns selected variants in `MessageIndex` order.
- `ChatGenerationCoordinator` already filters and orders history by stable `MessageIndex`.
- `IndexModel.OnPostGenerateAsync` already creates the user/assistant exchange atomically.
- `chat.js` already contains local `_variants`, `_activeVariant`, `switchVariant`, and a regeneration stub.
- `chat-templates.js` already renders previous/next and regenerate controls.
- `chat-signalr.js` translates hub callbacks into semantic jQuery events.

## Assigned Task

Implement owned session deletion, same-row failed retry, completed-response regeneration, compact variant preview, explicit variant selection, required SignalR behavior, and focused tests. Use the existing coordinator for retry/regeneration. Do not change its history, retrieval, metrics, or prompt construction.

## Frozen Contracts

Read and follow the complete “Chat Reliability And Variants” section in [three-day-delivery-contracts.md](../contracts/three-day-delivery-contracts.md). Exact handlers:

- `IndexModel.OnDeleteSessionAsync(Guid id, CancellationToken cxlTkn)`
- `IndexModel.OnPostRetryAsync(Guid sessionId, RetryAssistantMessageRequest request, CancellationToken cxlTkn)`
- `IndexModel.OnPostRegenerateAsync(Guid sessionId, RegenerateAssistantMessageRequest request, CancellationToken cxlTkn)`
- `IndexModel.OnGetVariantAsync(Guid sessionId, Guid messageId, CancellationToken cxlTkn)`
- `IndexModel.OnPutSelectedVariantAsync(Guid sessionId, SelectAssistantVariantRequest request, CancellationToken cxlTkn)`

Use the exact routes, response types, numeric enums, status codes, and SignalR method names in that document. Treat them as the agreed baseline. If current code or a user request conflicts with them, follow the Authority And Deviations protocol before changing anything.

## Exact File Scope

Modify only:

- `PresentationLayer/Pages/Chat/Index.cshtml`
- `PresentationLayer/Pages/Chat/Index.cshtml.cs`
- `PresentationLayer/Realtime/AiChatHub.cs`
- `PresentationLayer/wwwroot/js/chat/chat.js`
- `PresentationLayer/wwwroot/js/chat/chat-signalr.js`
- `PresentationLayer/wwwroot/js/chat/chat-templates.js`

Create only:

- `UnitTests/ChatPageModelTests.cs`

## Prohibited Files

Do not modify Domain entities/contracts, `ChatPersistenceService`, `ChatGenerationCoordinator`, DbContext, migrations, model snapshot, `Program.cs`, AI configuration, retrieval, model factories, prompt assembly, indexing, experiments, reports, or token instrumentation.

## Required Behavior

- Ownership is checked before every handler mutation/read.
- Retry accepts only a failed assistant and uses `ResetFailedAssistantMessageAsync`; it keeps identity and enqueues the existing coordinator.
- Regeneration accepts only a completed assistant and uses `CreateAssistantVariantAsync`; the new variant is pending and selected before enqueue.
- Preview fetches one full variant and does not change selection.
- Selection is atomic through the persistence service and emits selection notification without generation.
- Treat the existing coordinator history behavior as a completed foundation dependency: it is ordered by `MessageIndex`, contains selected completed prior assistant slots plus user rows, and never uses regenerated `SentAt` as order.
- Client state stores the current full DTO and compact navigation identities. Server IDs are authoritative.
- Resource update deletion and chat events reconcile across tabs through state, not direct SignalR DOM edits.

## Fixtures

- [chat-session-variants.json](../contracts/fixtures/chat-session-variants.json)
- [chat-variant-response.json](../contracts/fixtures/chat-variant-response.json)
- [chat-generation-start.json](../contracts/fixtures/chat-generation-start.json)

## Expected Tests

Cover owned/not-owned deletion; invalid retry/regenerate statuses; same-ID retry; next-index regeneration; preview without selection; selection without generation; regeneration of an early turn after later turns; duplicate mutation conflict; and SignalR event payload mapping.

## Verification Commands

```powershell
dotnet test UnitTests/UnitTests.csproj --no-restore --filter "FullyQualifiedName~ChatPageModelTests"
dotnet build EduChatAI.slnx --no-restore
git diff --check
```

Manually test two browser tabs, desktop/mobile layouts, browser console, and network failures.

## Expected Final Report

Report modified files, behavior completed, exact commands and results, manual scenarios, warnings, and residual risks. Preserve unrelated changes. Return one reviewable branch or squashed commit. Do not silently create or modify contracts; handle any conflict through the Authority And Deviations protocol.
