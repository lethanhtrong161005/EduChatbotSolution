# WI-006: Three-Day Four-Flow Delivery

Last verified: 2026-07-13  
Status: Active  
Hotspot: Current  
Implementation state: Phase 2 contract/data/chat foundation complete; owner services and teammate UI flows remain pending

## Goal

Deliver four real happy-path flows in a three-day, four-person sprint:

1. document upload, three configurable chunking strategies, reindex, and indexed-content use;
2. bilingual RAG chat with session deletion, failed retry, completed regeneration, navigation, and selection;
3. admin adoption-and-health reports with range/role controls, all-subject or one-subject trends, subject comparison, source use, failures, latency, indexing status, and token coverage;
4. a Vietnamese production-path experiment with question-level RAGAS-style evaluation and visual run comparison.

Phase 1 froze the private handoffs and contracts. Phase 2 was explicitly approved and implemented the shared contracts, data model, migrations, chat persistence foundation, stable history, and truthful generation metrics.

## Baseline

- Branch: `dev`
- HEAD: `0fa65f7d1c1c4f6841ad36e7a2ba04cafd74279f`
- Baseline worktree: clean; branch was three commits ahead of `origin/dev`
- `.agents` is ignored by `/.agents/` in `.gitignore`; ordinary Git status and diff do not show this private workspace.

Verified implementation anchors:

- nullable subject overrides, global defaults, and resolver precedence exist;
- only `FixedLengthChunker` exists and is injected as one singleton; `DocumentIndexer` does not select a strategy;
- global/subject/effective configuration now includes chunk size and overlap; only `FixedLengthChunker` is implemented so far;
- upload/indexing and document status flow exist; reindex does not;
- chat history is ordered by stable `MessageIndex`; provider usage is nullable and response timings are measured;
- embedding usage is nullable in `EmbedResult` but indexing does not persist it;
- chat deletion exists only in the persistence service; no owned HTTP handler or UI exists;
- chat JavaScript has local-only variant and regeneration scaffolding with no persistence;
- report and experiment application services/pages do not exist;
- experiment entities now implement the frozen snapshot, progress, per-question result, and retrieved-context foundation; the runner/evaluator services remain pending;
- installed packages are `Microsoft.Extensions.AI` 10.7.0, `Microsoft.Extensions.AI.OpenAI` 10.7.0, OpenAI 2.11.0, and OllamaSharp 5.4.25.

## Frozen Decisions

- Shared contracts: [three-day-delivery-contracts.md](../../contracts/three-day-delivery-contracts.md)
- Chat turn/variant design: [ADR 0010](../../adr/0010-chat-turns-and-assistant-variants.md)
- Experiment and measured-usage design: [ADR 0011](../../adr/0011-three-day-experiments-and-measured-usage.md)
- Demo subject: seeded `DB201` (`Database Systems`).
- Chunkers: `FixedLength`, `RecursiveSeparator`, `SentenceParagraph`; no semantic strategy.
- Chat variants remain separate assistant `ChatMessage` rows and reuse an explicit stable logical `MessageIndex`.
- Token absence is null/unmeasured, never fabricated zero.
- Experiments use a C# evaluator and describe metrics as RAGAS-style.
- Report trends default to all subjects, may filter one subject, and never filter the cross-subject chart/table.
- Reports use 7-day, 30-day, and all-time ranges, role filtering, local `Asia/Bangkok` days, and distinct answer/document citation counts.
- Per-subject experiment execution is serialized, reuses a compatible live index, and locks DB201 chat while incompatible indexing settings are applied.
- Every experiment remains in history; detailed comparison selects exactly two compatible completed runs.

## Ownership

### Owner — AI core, data, and integration

- entity/configuration/resolver changes;
- chunker factory, three strategies, index selection, subject reindex;
- nullable provider usage and timing instrumentation;
- chat persistence/entity/EF foundation and migrations after approval;
- report projections, range/role/trend-subject filters, and service;
- document indexed-configuration fields, subject index availability, experiment preflight/orchestration, production-path generation, evaluator, persistence;
- contract DTOs/interfaces, DI, seed import, admin navigation, and final integration.

### A — chat reliability and variants

- owned session deletion handler/UI;
- failed retry;
- completed regeneration;
- preview navigation and explicit selection;
- PageModel variant handlers, SignalR behavior, and chat JavaScript/templates;
- focused chat tests.

Handoffs: [human](../../handoffs/teammate-a-human-vi.md), [agent](../../handoffs/teammate-a-agent-en.md).

### B — AI administration presentation

- subject AI configuration Razor/UI;
- inherited, override, and effective-value presentation;
- reindex action presentation;
- experiment creation/configuration Razor/UI;
- 50-question Vietnamese `DB201` dataset with Vietnamese ground truth and focused validation tests;
- fixture-first PageModel/client tests.

Handoffs: [human](../../handoffs/teammate-b-human-vi.md), [agent](../../handoffs/teammate-b-agent-en.md).

### C — reports and experiment visualization

- report Razor/UI, three timelines, indexing-status donut, subject chart/table, health table, and hot-document table;
- experiment history/progress, results, and pairwise comparison Razor/UI;
- fixture-first PageModel/client tests.

Handoffs: [human](../../handoffs/teammate-c-human-vi.md), [agent](../../handoffs/teammate-c-agent-en.md).

## File Collision Policy

- Owner alone edits `PresentationLayer/Program.cs`, `PresentationLayer/Pages/Admin/_AdminLayout.cshtml`, `BusinessLayer/Business.csproj`, EF migrations/snapshot, entities, and owner services.
- Owner completes the chat foundation before A branches. A does not edit `ChatMessage`, `IChatPersistenceService`, `ChatPersistenceService`, DbContext, migrations, or seed integration.
- The owner completed `ChatGenerationCoordinator` history ordering and the current generate-handler bridge. A must not change coordinator history, retrieval, metrics, or prompt construction.

## Phase 2 Foundation Completion

Completed on 2026-07-13:

- published the frozen AI configuration, report, experiment, evaluator, dataset, and chat contracts;
- added chunk size/overlap, index availability, successful document-index configuration, nullable usage, and the strict experiment persistence model;
- added `AddPhase2ContractDataModel` and `AddChatMessageVariants` with deterministic legacy chat backfill and deferred variant validation;
- implemented atomic chat exchanges, failed retry reset, regeneration, lookup, selection, compact navigation, and selected-variant session reads;
- changed coordinator history from `SentAt` to `MessageIndex` and changed the existing generate handler to atomic exchange creation;
- measured retrieval, first-token, total-generation, and title timings and persisted provider token usage only when supplied;
- verified focused tests, frozen JSON fixtures, forward/rollback SQL, valid backfill, rejected invalid selection, and both migration directions on disposable PostgreSQL.

The PostgreSQL timestamp function and trigger are lowercase `update_timestamp`; older migration names are historical and must not be copied.
- B and C use separate pages, scripts, and styles. Neither edits `_AdminLayout.cshtml`; owner adds navigation after both branches integrate.
- B owns only `BusinessLayer/Services/AI/Experiments/Data/db201-vi-50.json` and `UnitTests/VietnameseExperimentDatasetTests.cs`; owner owns DTO, resource/import, seed, and project wiring.

## Critical Path

```text
contract-only code + approved chat foundation
  -> A chat implementation

configuration entity/resolver + chunker factory + persisted indexed values
  -> B configuration UI connection
  -> subject reindex
  -> production chat remains healthy

usage/timing persistence
  -> report projections
  -> C dashboard connection

subject index preflight/availability + shared reindex/generation + dataset
  -> experiment runner/evaluator
  -> B creation connection
  -> C result/comparison connection
```

## Schedule And Gates

### Hour 0–4

- Record baseline and preserve local work.
- Implement frozen DTO/interface contract-only foundation.
- Obtain explicit migration approval and implement chat variant foundation.
- Issue A/B/C branches from the same foundation.
- Fixtures deserialize against the contract types.

### Hour 4–8

- Owner implements chunker factory and all three strategies with focused tests.
- Owner connects chunk selection to effective configuration.
- B and C render fixture-backed pages.
- A loads server-backed turn/variant state.

Gate at hour 8: three chunkers are designed, tested, and contractually connected; configuration UI contracts are usable.

### Hour 8–14

- Owner implements subject configuration save and reindex reset/queue flow.
- Owner instruments retrieval, first token, total time, and provider usage.
- A implements deletion, retry reset, and regeneration creation.
- B completes configuration and experiment-create interaction states.

Gate at hour 14: nullable token/timing design is persisted, at least one configured provider has an observed non-null usage result, and Vietnamese/English prompt behavior is manually checked.

### Hour 14–24

- Owner connects reindex end to end, records successful size/overlap/model/strategy values, and verifies all three strategies reach `Indexed`.
- Owner implements report projections, range/role/trend-subject filtering, latency, citation coverage, and token coverage semantics.
- A completes navigation, selection, SignalR, and tests.
- C completes report and experiment visualization against fixtures.

Gate at hour 24: subject save/reindex is live for all three strategies, indexed values are persisted, and production chat is blocked only during a subject-wide reindex.

### Hour 24–30

- Owner implements compatibility preflight, durable subject index availability, experiment persistence, serialized orchestration, production generation reuse, and C# evaluator.
- B completes the first validated 50-question dataset revision and checks it against the final DB201 demo documents.
- Integrate B create and C result/comparison pages.

Gate at hour 30: a compatible three-question run skips reindex; an incompatible run shows progress, reaches `Indexed`, then completes retrieval, generation, evaluation, persistence, and results display.

### Hour 30–34

- Connect dashboard to real report service.
- Verify timeframe, role, optional trend-subject, measured-token coverage, and null behavior.
- Resolve owner-only navigation/DI integration.

Gate at hour 34: no required report chart or table uses fixture data.

### Hour 34–40

- Run all 50 Vietnamese questions.
- Complete a second comparable run.
- Rehearse all four demos.
- Run build, tests, migration forward/rollback inspection, and browser checks.
- Freeze feature work and repair only demo-blocking defects.

Gate at hour 40: two completed runs compare visually, all four flows pass the rehearsal, and verification evidence is recorded.

## Cut Lines

Cut first:

- WI-003 citation occurrence work;
- usage support for every provider after one real provider works;
- prompt-editor polish;
- reindex cancellation;
- experiment cancellation or resume;
- attachments, personal file library, quiz generation, subscriptions, payment completion, context editing, and hidden reasoning.

Cut second:

- per-question evaluator explanation;
- context precision only if schedule recovery requires a three-metric display and copy is updated honestly;
- OpenRouter in the live demo;
- comparison of more than two runs at once; retain complete run history;
- date ranges beyond 30 days and all time after retaining the required 7-day option.

Never cut:

- three chunking strategies;
- one subject AI configuration dashboard;
- Vietnamese and English chat;
- session deletion, failed retry, completed regeneration, and variants after foundation starts;
- real metric persistence and coverage;
- all-subject and one-subject trends plus cross-subject usage comparison;
- 50 Vietnamese questions with Vietnamese ground truth;
- one real experiment run and a visual comparison using two completed runs.

## Demo Criteria

### Flow 1

1. Upload a document for `DB201`.
2. Show successful indexing.
3. Save and display each of the three strategies with size/overlap.
4. Reindex and reach `Indexed`.
5. Ask chat a question whose answer cites the indexed material.

### Flow 2

1. Ask one Vietnamese and one English question.
2. Delete an owned session.
3. Retry a deliberately failed assistant row and keep the same variant identity.
4. Regenerate a completed response and retain both variants.
5. Preview both variants, select one, send a follow-up, and show selected-history behavior.

### Flow 3

1. Switch among 7 days, 30 days, and all time.
2. Show all-subject trends, select `DB201`, and show only the timelines change.
3. Change the role filter and show adoption-and-health KPIs plus three timelines.
4. Show the all-subject horizontal chart/table, subject health, indexing-status donut, and hot documents.
5. Explain prompt/completion token coverage without treating unavailable usage as zero.

### Flow 4

1. Show the 50-question Vietnamese dataset.
2. Preflight a `DB201` experiment and show whether the index is compatible and how many documents are affected.
3. Create the run, open progress immediately, and run questions only after any required reindex completes.
4. Run the real production retrieval/generation path and C# evaluator.
5. Open question-level results and aggregate RAGAS-style scores.
6. List all runs, select exactly two compatible completed runs, and compare them visually.

## Risks And Recovery

| Risk | Early signal | Recovery |
|---|---|---|
| Legacy chat data fails safe backfill | preflight detects non-alternating roles | stop migration, export offending IDs, explicitly map or remove anomalies, rerun; never infer from late regeneration timestamps |
| Provider omits usage | usage remains null in smoke test | demo the provider that returns usage; keep null and coverage for others |
| Reindex leaves stale or ambiguous chunks | indexed values are null/mismatched or duplicate chunks remain | treat unknown values as incompatible, reset transactionally, persist successful strategy/size/overlap/model, and verify before unlock |
| Experiment reindex conflicts with chat | subject configuration changes during a run | persist `Reindexing`, reject DB201 chat retrieval, serialize per subject, and restore `Ready` only after every affected document succeeds |
| Experiment reindex fails | subject availability becomes `Failed` | show failure, keep chat unavailable, and recover through successful manual Admin reindex |
| Evaluator emits invalid scores | JSON or bounds validation fails | persist question failure, retry once through job policy, show failure honestly |
| Fixture/backend mismatch | UI normalization or deserialization fails | contract fixture tests block integration; contract changes require coordinated stop |
| Owner integration overloaded | hour-24 gate slips | apply cut-first list immediately and keep teammates on frozen files |
| Dataset lacks grounding | answers cannot be tied to demo documents | B reviews against the final `DB201` documents before the 50-question run |

## Verification

During implementation:

- focused NUnit tests per owner;
- PostgreSQL repository tests use `EDUCHATAI_PHASE2_TEST_DATABASE` and must point only to a disposable database migrated to the latest model; the fixture seeds its own anonymous user;
- `dotnet build EduChatAI.slnx --no-restore`;
- `dotnet test EduChatAI.slnx --no-build --no-restore`;
- migration forward and rollback SQL inspection;
- browser checks at desktop and mobile widths;
- network and console inspection;
- real provider usage smoke test;
- three-question and 50-question experiment runs;
- four-flow rehearsal.

## Resume Prompt

Read the [owner conversational assistant handoff](../../handoffs/owner-conversational-assistant-en.md), then this work item, both ADRs, and the shared contracts. Verify that the repository snapshot contains the complete Phase 2 contract/data/chat foundation. Begin owner backend work with the chunker selector and three strategies, connect effective size/overlap to indexing, then implement subject configuration save and reindex. Preserve A/B/C ownership and use the documented deviation protocol before changing an agreed contract or boundary.
