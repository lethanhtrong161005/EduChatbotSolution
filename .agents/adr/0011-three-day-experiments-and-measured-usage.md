# ADR 0011: Three-Day Experiments And Measured Usage

Last verified: 2026-07-13  
Status: Accepted; contracts, persistence foundation, and usage instrumentation implemented

## Context

The repository has subject-over-global AI configuration and one production indexing/chat path. It has incomplete `Experiment`, `TestQuestion`, and `TestResponse` entities but no runner, evaluator, report service, or experiment UI. The current chat service persists zero token and timing values, while the installed AI abstractions expose nullable usage. There is only one production chunk set per document, so comparing chunking configurations cannot run concurrently without a much larger experiment-index design.

The sprint needs a real ASP.NET Core demonstration within three days, one subject, a Vietnamese 50-question dataset, question-level persistence, honest RAGAS-style scores, and visual comparison.

## Decision

### Configuration and indexing

- Use seeded subject `DB201` as the fixed demo subject.
- Extend global, subject, and effective AI configuration minimally with `ChunkSize` and `ChunkOverlap`.
- Add fixed-length, recursive-separator, and sentence/paragraph-packing strategies. Do not add semantic chunking.
- Persist `IndexedChunkingStrategy`, `IndexedChunkSize`, `IndexedChunkOverlap`, and `IndexedEmbeddingModel` on each document only after indexing succeeds. These values describe the live index, while subject/global values describe the desired configuration.
- Leave the new document index fields null for legacy rows because size and overlap cannot be reconstructed safely; the first compatibility check reindexes them.
- An experiment request contains a complete indexing, retrieval, generation-model, and judge-model configuration.
- Before submission, a preflight compares the requested indexing values with every subject document and returns compatibility plus affected-document count. Retrieval, generation, prompt, and judge changes do not require reindexing.
- A compatible experiment reuses the live index. An incompatible experiment acquires a per-subject exclusive lock, marks the subject index unavailable to chat, applies requested values as subject overrides, resolves and persists the immutable effective snapshot, and reindexes affected documents through the production path.
- Persist subject index availability as `Ready`, `Reindexing`, or `Failed`. Production chat rejects retrieval while availability is not `Ready`; successful manual reindex recovers a failed subject.
- The last experiment configuration remains the subject's active production configuration. The UI must state this before submission.
- Runs for one subject are serialized. This avoids introducing a second chunk store during the sprint.
- The results page opens immediately after creation and shows document/question progress through queued, indexing, running, evaluating, completed, and failed states. It does not promise a fixed duration.

This deliberately favors a real, short implementation over concurrent isolated experiment indexes. A future ADR may add experiment-scoped chunks.

### Production pipeline reuse

The experiment runner builds the same `ChatGenerationRequest` and invokes the same `IChatGenerationService` used by production chat. It must not duplicate embedding, vector search, context assembly, prompt construction, citation processing, or provider factories.

Each question persists its generated answer, retrieved context text, generation settings snapshot, nullable provider token counts, measured timings, status, and evaluation result before the next question begins.

### Evaluator

- Implement one C# structured-output evaluator using `Microsoft.Extensions.AI.IChatClient`.
- Use configured Gemini `gemini-3.5-flash` as the fixed judge for comparable sprint runs.
- The response contains `Faithfulness`, `AnswerRelevancy`, `ContextPrecision`, `ContextRecall`, and optional concise `Explanation`.
- Reject non-finite values and values outside `[0, 1]`.
- Product copy must say “RAGAS-style faithfulness, answer relevancy, context precision, and context recall.”
- Do not claim or imply that the Python RAGAS package ran.

### Usage and timing

- Change prompt and completion token fields in chat, title, experiment, and reporting contracts to nullable numeric types.
- Persist provider counts when supplied; persist null when not supplied. Never fabricate zero.
- Compute tokens per second only when completion tokens are measured and positive; otherwise persist null.
- Measure retrieval time, time to first text token, and total response time with `Stopwatch`; time to first token is nullable if no text token arrives.
- `Microsoft.Extensions.AI` 10.7.0 exposes `ChatResponse.Usage`, streamed `UsageContent`, and nullable `UsageDetails.InputTokenCount` / `OutputTokenCount`.
- OllamaSharp 5.4.25 maps prompt and response evaluation counts into `UsageDetails` and exposes embedding prompt counts.
- The OpenAI adapter is capable of translating OpenAI-compatible usage into the abstraction. OpenRouter and Gemini runtime responses still require one credentialed smoke test each; unsupported or omitted usage remains null.
- `EmbeddingService` already returns nullable generated-embedding usage. Indexing must aggregate it instead of discarding it.

### Reporting semantics

- Usage reports count every persisted assistant generation variant because every variant consumes resources; selection does not erase cost.
- Subjectless sessions appear in an explicit “All subjects” bucket with `SubjectId = null` rather than being attributed to a real subject.
- Reports always offer 7-day, 30-day, and all-time ranges plus a role filter. Trend charts default to all subjects and may filter one subject without changing the all-subject comparison section.
- Daily buckets use the organization timezone `SE Asia Standard Time` (`Asia/Bangkok`) and include zero-activity dates.
- Token totals separate measured prompt, completion, and total tokens and always include measured-message count, eligible-message count, and coverage percent.
- If eligible count is zero, measured token values are zero. If eligible count is positive and no message is measured, measured token values are null. Partial coverage reports known sums together with coverage.
- Adoption metrics are unique active users, active sessions, and completed assistant generations. Health metrics are generation success, citation coverage, p95 response time, no-context/failure subject rows, and current indexing status.
- Most-cited documents count distinct completed answer/document pairs, not raw chunks or citation occurrences.
- Experiment history lists every run, while detailed comparison accepts exactly two compatible completed runs.

## Consequences

- Experiments are real and comparable but an incompatible run temporarily changes and locks the live `DB201` subject configuration and index.
- Compatibility preflight, a durable subject availability state, a per-subject lock, progress UI, and clear warning are mandatory.
- Previous runs remain comparable because results and retrieved contexts are persisted before later reindexing.
- Compatible experiments avoid unnecessary reindex delay; incompatible duration remains data/provider dependent and is never shown as a promise.
- Experiment entities, immutable configuration snapshots, progress fields, retrieved-context rows, document index metadata, subject availability, nullable usage, and frozen service contracts are implemented.
- Chunker selection, subject reindex orchestration, report queries, dataset import/seed wiring, experiment execution, and the evaluator remain owner work.
- Provider capability is verified from installed packages, but live OpenRouter and Gemini usage must remain an explicit integration check.

## Implementation And Remaining Anchors

- `Domain/Entities/SubjectAiConfiguration.cs`
- `Domain/Entities/GlobalAiConfiguration.cs`
- `Domain/Entities/Subject.cs`
- `Domain/Entities/Document.cs`
- `Domain/Entities/ChatMessageGenerationMetrics.cs`
- `Domain/Entities/Experiment.cs`
- `Domain/Entities/TestQuestion.cs`
- `Domain/Entities/TestResponse.cs`
- `Domain/Contracts/DTOs/EffectiveAiConfiguration.cs`
- `BusinessLayer/Services/AI/AiConfigurationResolver.cs`
- `BusinessLayer/Services/AI/Indexing/`
- `BusinessLayer/Services/AI/Chat/ChatGenerationService.cs`
- `BusinessLayer/Services/AI/Experiments/`
- `BusinessLayer/Services/Reports/`
- `PresentationLayer/Pages/Admin/`
- `DataAccessLayer/Data/EduChatAIDbContext.cs`
- `DataAccessLayer/Migrations/`
- [Three-day shared contracts](../contracts/three-day-delivery-contracts.md)
