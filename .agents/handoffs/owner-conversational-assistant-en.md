# Owner Conversational Assistant Handoff — Phase 2 To Backend Implementation

Verified: 2026-07-13

## Mission And Assistant Role

You are returning to EduChatAI after providing the original Phase 1 discovery and handoff. Continue supporting the owner through the remaining backend implementation for the three-day sprint. You can read the public repository and the `.agents` files that the owner manually supplies, but you may not have the owner's current uncommitted working tree.

Inspect current code before proposing changes. When you cannot edit or execute locally, provide complete ready-to-paste changes grouped by file, state exact replacement boundaries, and never claim that you built or tested work you could not run.

## Authority And Deviations

Repository code is authoritative for the current implementation state. `.agents` records the agreed plan, rationale, ownership boundaries, and contracts. The owner's confirmed direction is final for what the project should become.

If a request deviates from `.agents`, do not silently follow or reject it. Cite the relevant `.agents` file and heading, explain the conflict and likely consequences, and explicitly ask: “Do you intend to override this agreed plan?” After confirmation, follow the owner's direction and identify every affected code contract, handoff, ADR, or work-item statement that must be reconciled. Never create an undocumented local fork of a shared contract or ownership rule.

## Repository Synchronization Check

At handoff creation, the owner was on `dev` at `0fa65f7d1c1c4f6841ad36e7a2ba04cafd74279f`. All Phase 2 changes were still local and uncommitted. Before backend work, verify that the repository snapshot you can read contains at least:

- `DataAccessLayer/Migrations/20260713022424_AddPhase2ContractDataModel.cs`;
- `DataAccessLayer/Migrations/20260713023205_AddChatMessageVariants.cs`;
- `DataAccessLayer/Repositories/ChatTurnRepository.cs`;
- `Domain/Contracts/DTOs/TestDatasetDtos.cs`;
- `UnitTests/ContractBoundaryTests.cs`, `ContractFixtureSerializationTests.cs`, and `ChatAndExperimentDataModelTests.cs`.

If these anchors are absent, the public branch is behind the owner's working tree. Ask the owner to publish or provide the current branch before designing backend changes. Do not recreate Phase 2 from the older Phase 1 snapshot.

## What Changed During This Session

The owner and Codex refined the sprint plan before implementation:

- approved stable `MessageIndex` chat ordering and coordinator-history changes;
- changed the demo subject from `SE401` to seeded `DB201` (`Database Systems`);
- kept every experiment in history while allowing detailed comparison of exactly two compatible completed runs;
- changed reports to adoption-and-health metrics and removed daily document uploads;
- made timeframe selection mandatory (`7 days`, `30 days`, or `all time`) and made trends default to all subjects with optional one-subject filtering;
- kept cross-subject ranking and tables unfiltered by the trend subject;
- defined compatible-index reuse and incompatible-index reindex/progress/chat-lock behavior without promising a completion time;
- transferred the Vietnamese 50-question dataset and its validation test from C to B;
- simplified the Vietnamese human handoffs while keeping technical contracts in English agent documentation.

The owner then approved Phase 2 production implementation. It delivered:

- frozen AI configuration, report, experiment, evaluator, dataset, and chat-variant DTOs/interfaces without registering unfinished report or experiment services;
- global chunk defaults `1000`/`200`, nullable subject overrides, document successful-index configuration, subject index availability, nullable provider usage, and the strict experiment persistence model;
- two EF migrations with guarded chat backfill, composite chat relationships, filtered indexes, role checks, and a deferred PostgreSQL selection constraint;
- atomic chat exchange creation, same-row failed reset, regeneration, lookup, selection, sequential indices, selected-only session history, and row locking;
- `MessageIndex` coordinator history and atomic exchange use in the existing generate handler;
- measured retrieval, first-token, total-generation, and title timings, with provider token values persisted only when supplied and throughput calculated only for positive measured completion tokens.

Afterward, language-specific DTO naming was removed because the dataset already carries `Language`: `TestDatasetDto` and `TestDatasetQuestionDto` now live in `TestDatasetDtos.cs`. Generic `Phase2*` tests were renamed by purpose to `ContractBoundaryTests`, `ContractFixtureSerializationTests`, and `ChatAndExperimentDataModelTests`.

The current PostgreSQL timestamp function and trigger are lowercase `update_timestamp`. Older mixed-case names are historical and must not be copied.

## Verification Already Performed

- Full build: 0 warnings and 0 errors.
- Full Phase 2 suite against a disposable PostgreSQL database: 91 passed, 0 failed, 0 skipped.
- Post-rename suite without the disposable database variable: 86 passed, 0 failed, 5 expected PostgreSQL-gated skips.
- EF reported no pending model changes.
- Both migrations applied and rolled back on disposable PostgreSQL.
- Valid legacy chat backfill, malformed-history rejection, deferred selection enforcement, and repository concurrency were exercised.
- No shared database was updated; disposable databases were removed.

Re-run relevant checks against the repository state you actually receive. Treat this evidence as session history, not proof that a later public branch is unchanged.

## Reading Order

1. `.agents/AGENTS.md` for collaboration, architecture, and verification rules.
2. `.agents/context/project-overview.md` for current solution orientation.
3. `.agents/work-items/active/WI-006-three-day-four-flow-delivery.md` for ownership, sequencing, cut lines, and demo gates.
4. `.agents/adr/0010-chat-turns-and-assistant-variants.md` and `.agents/adr/0011-three-day-experiments-and-measured-usage.md` for durable decisions.
5. `.agents/contracts/three-day-delivery-contracts.md` and its fixtures for exact frozen interfaces.
6. `.agents/handoffs/teammate-*-agent-en.md` only when checking owner/teammate collision boundaries.

Code wins when an implementation fact differs from documentation. Ask the owner before changing an agreed behavior or ownership boundary.

## Remaining Owner Backend Sequence

### 1. Chunking, configuration, and subject reindex

Start here. Inspect `IDocumentChunker`, `FixedLengthChunker`, `DocumentIndexer`, `AiConfigurationResolver`, and the frozen AI configuration contract. Add the three approved strategies through a small strategy selector/factory, pass effective size and overlap into the selected chunker, persist successful indexing configuration only after embedding succeeds, and implement owner-controlled subject save/reindex behavior. Unknown legacy index configuration is incompatible and requires safe reindex.

### 2. Admin report service

Implement `IAdminReportService` with `Asia/Bangkok` daily grouping, zero-filled dates, range/role/trend-subject controls, honest token coverage, p95 response time, distinct answer/document citation ranking, health thresholds, and cross-subject sections that remain unfiltered by the optional trend subject.

### 3. Experiment orchestration and evaluator

Wire owner-controlled dataset import/seed, implement compatibility preflight, per-subject serialization, subject chat availability, production reindex reuse, production chat generation reuse, progress persistence, and the C# structured-output RAGAS-style evaluator. Compatible runs skip reindex. Incompatible runs reindex affected documents, then start questions automatically. Do not claim Python RAGAS execution.

### 4. Owner-only integration

Register completed services and jobs, add shared Admin navigation, connect B/C pages after their branches arrive, run the three-question and 50-question demonstrations, and complete final migration/browser/provider verification. Preserve A/B/C file ownership from WI-006.

## Immediate Resume Request

After confirming repository synchronization, help the owner begin the first backend batch: inspect the current chunking and indexing implementation, then produce a decision-complete, test-first change set for chunker selection and all three strategies. Do not redesign the frozen Phase 2 contracts or generate another migration unless current code proves a schema change is necessary.

