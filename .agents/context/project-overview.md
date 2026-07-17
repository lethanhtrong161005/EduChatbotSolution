# EduChatAI Project Overview

Last verified: 2026-07-13

Repository code is the source of truth. This overview is a navigation aid, not a replacement for inspection.

## Product And Stack

EduChatAI is a Vietnamese educational knowledge-base and chat application. It combines course document management, RAG chat with citations, role-aware subject access, realtime updates, background indexing, authentication, subscriptions, and payments.

The current stack is .NET 10, ASP.NET Core Razor Pages, EF Core with PostgreSQL/Npgsql and pgvector, ASP.NET Core Identity, Hangfire, SignalR, Redis, AutoMapper, jQuery, TailwindCSS, and Font Awesome. Tests use NUnit, not xUnit.

## Solution Shape

- `Domain`: entities, contracts, DTOs, exceptions, constants, and dependency-free helpers.
- `DataAccessLayer`: `EduChatAiDbContext`, migrations, repositories, and unit of work.
- `BusinessLayer`: document, AI, account, subscription, payment, parsing, chunking, and embedding services.
- `PresentationLayer`: Razor Pages, DI/bootstrap, SignalR hubs/notifiers, background-job adapters, mappings, and static UI assets.
- `UnitTests`: focused NUnit service, storage, parser, and orchestration tests.

The root README is the current public setup and architecture guide. Repository code and configuration remain authoritative when details drift.

## Docker And Configuration

Docker Compose is the primary setup path. The Dockerfile keeps the ASP.NET `base` stage first because Visual Studio Fast mode targets it directly. Regular image builds also compile Tailwind in a `node:24-alpine` asset stage, copy the generated `styles.css` into the .NET build, and keep Node out of the runtime image. Ordinary host and Visual Studio builds still invoke the project Tailwind target and therefore require npm.

`docker-compose.yml` is the portable CPU-default stack and serves the app over HTTP on port `8080`. `docker-compose.gpu.yml` adds NVIDIA access for Ollama. Visual Studio includes that GPU override through the Compose project and applies `docker-compose.vs.debug.yml` for User Secrets, development HTTPS mounts, HTTPS redirection, and fixed port `8081`. The VS override resets the CLI `.env` file so User Secrets remain authoritative. Visual Studio dependency-aware startup is intentionally not enabled because it tears down the Compose containers when debugging stops; the normal workflow keeps infrastructure running between sessions.

The application uses one logical `ConnectionStrings:Database` key. Tracked app settings use `localhost` for host execution and EF tooling; Compose overrides the host with the service name `db`; User Secrets or a future hosting environment may replace the same key. CLI integration credentials come from an ignored `.env` shaped from `.env.example`; never place actual values in tracked files or these notes.

PostgreSQL and Redis have Compose health checks, and the web service waits for them before applying migrations and seed checks. Ollama model downloads remain a manual first-run operation.

## Domain Highlights

- `Subject` and `Chapter` use integer category-like IDs; most transactional entities use GUID natural IDs.
- Every `Document` belongs to one subject and may be tagged with zero or more chapters.
- `DocumentChapter` includes `DocumentId`, `ChapterId`, and `SubjectId`; composite foreign keys prevent cross-subject tags.
- `SubjectAiConfiguration` and `SubjectStorageConfiguration` are shared-key one-to-one dependants of `Subject`.
- Document storage records an opaque staging locator, an opaque durable locator, and the actual durable storage method.

## Document Flow

```text
HTTP file -> reception validation -> staging -> Document(Received, Unspecified)
-> Hangfire persistence job -> configured durable strategy
-> parse stream -> chunk -> embed -> indexed
```

Reception validates before staging. Non-seekable input is replayed through a local file lease. Staging does not expose local-path semantics to consumers. Durable strategies are entity-free and operate on streams, opaque locators, deterministic names, and durable directories. Supabase path-only SDK calls use local leases rather than whole-file byte arrays.

The owning subject resolves durable policy through `SubjectStorageConfiguration`; absent or unusable policy falls back to Supabase. The document persists the strategy actually used. Reads route to staging while storage is `Unspecified`, otherwise to the durable strategy.

## Document Library Client

The document library is a Razor Page with separate main, template, and SignalR JavaScript modules. It uses three categories of state:

- view state for selections, navigation, search, pagination, and panel visibility;
- resource state for subjects, chapters, documents, and upload items;
- private controller state for concurrency tokens, connection IDs, rollback values, initialization, and generation-scoped reconciliation buffers.

Only `mutateState_*` methods directly change state. State events may branch to independent resolution and rendering handlers. Templates are pure, and realtime events enter through state rather than directly manipulating DOM.

The upload system captures immutable subject/chapter destinations, limits execution to three simultaneous transfers, and exposes a non-blocking queue with cancellation, retry, removal, per-row progress, and aggregate outcome state. Successful uploads may be inserted immediately when they match the current page-one query, followed by a silent canonical reload.

Resource loaders create a new concurrency generation before any default or cache return. `load*()` may reuse matching committed state; `reload*()` bypasses cache. Document collection metadata includes every query input.

Document status updates carry timestamps through resource state. Before an HTTP result replaces documents, the client reconciles the HTTP snapshot, current resource state, and realtime updates buffered during that load generation. Status-hub connection recovery triggers a silent canonical reload.

## Chat And Citations

Chat generation retrieves allowed chunks, streams a raw answer, rewrites valid `{{n}}` chunk markers into UI citation markers, and persists generated content and citations. Citation extraction scaffolding and `CitationOccurrence` entities exist, but occurrence-level quote validation is incomplete. Marker-to-chunk mapping must remain deterministic outside the model.

Chat history now uses stable `MessageIndex` ordering, selected completed prior variants, and atomic user/assistant exchange creation. Provider usage and first-token timing remain null when unavailable instead of being stored as fabricated zero. The server persistence and coordinator foundation are implemented; A still owns the retry, regeneration, selection, SignalR, and client UI flows.

## Three-Day Delivery

[WI-006](../work-items/active/WI-006-three-day-four-flow-delivery.md) freezes a four-person sprint for four flows: configurable document indexing/reindex, reliable RAG chat variants, Admin reporting, and a Vietnamese experiment/benchmark.

Planned ownership is horizontal:

- owner: AI configuration, chunking/indexing, chat persistence foundation, real usage/timing, report backend, experiment runner/evaluator/persistence, migrations, and integration;
- A: chat deletion, retry, regeneration, variant navigation/selection, coordinator/PageModel/SignalR/client, and focused tests;
- B: subject AI configuration, experiment-create presentation, and the Vietnamese 50-question dataset plus its focused validation tests;
- C: reports presentation and experiment results/comparison presentation.

The implemented chat foundation keeps each assistant variant as a `ChatMessage`, links it to the user with `InReplyToMessageId`, and gives logical user/assistant slots stable 1-based `MessageIndex` values. Regenerated variants reuse the assistant slot; history orders by `MessageIndex`, not regenerated `SentAt`. See accepted [ADR 0010](../adr/0010-chat-turns-and-assistant-variants.md).

The benchmark foundation now includes indexed-configuration fields, subject availability, strict experiment persistence, immutable configuration snapshots, progress, question results, retrieved contexts, and frozen service contracts. The pending owner service will use seeded subject `DB201`, preflight requested indexing settings, reuse a compatible live index, or serialize an incompatible reindex while chat is locked. It will then reuse production generation and a C# structured-output judge. Every run remains listed; detailed comparison selects exactly two compatible completed runs. Product copy calls the metrics RAGAS-style and never claims Python RAGAS execution. See accepted [ADR 0011](../adr/0011-three-day-experiments-and-measured-usage.md).

The frozen Admin report contract describes an adoption-and-health dashboard with 7-day, 30-day, and all-time ranges plus role filtering. Its three trends default to all subjects and may filter one subject without changing the cross-subject comparison. It separates prompt/completion token usage, shows p95 latency and citation/generation health, ranks documents by distinct citing answers, and uses a donut only for current indexing status. The owner report service and C's pages remain pending.

Phase 2 implemented the shared contracts, data model, two migrations, atomic chat persistence/history foundation, and truthful generation metrics. The three chunking strategies, subject reindex service, report queries, experiment execution/evaluation, teammate UI, dataset import/seed wiring, and final integration remain pending.

## Realtime And Background Work

The generalized resource hub publishes resource changes to type, instance, and related-collection groups. Request headers carry the caller connection ID so the originating client can update locally while other clients receive the broadcast. A separate document-status channel reports parsing, chunking, embedding, and indexing progress.

Hangfire runs durable persistence followed by parse, chunk, and embed continuations. The persistence adapter converts typed persistence failure into a failed job so indexing does not start prematurely. Indexing phase boundaries are persisted before notification; intermediate chunking progress is transient.

## Authentication, Authorization, And Payments

ASP.NET Core Identity provides cookie authentication, roles, email verification, and Google OAuth support. Subject membership and chief status govern document-management actions. Never translate unexpected service/database errors into authorization responses.

Subscriptions implement overlap-sensitive activation, upgrade, downgrade, renewal, expiration, and payment behavior. Treat these as explicit business rules. External payment integrations are incomplete in places; inspect the current services before planning changes.

## Current Hotspots

- **Current hotspot:** [WI-006 — Three-day four-flow delivery](../work-items/active/WI-006-three-day-four-flow-delivery.md)
- Active stretch item, not the hotspot: [WI-003 — Citation occurrence validation](../work-items/active/WI-003-citation-occurrence-validation.md)
- Active separate defect, not the hotspot: [WI-005 — Plain-text document validation](../work-items/active/WI-005-plain-text-document-validation.md)
