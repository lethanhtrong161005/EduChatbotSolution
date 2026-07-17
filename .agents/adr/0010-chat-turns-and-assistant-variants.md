# ADR 0010: Stable Chat Turns And Assistant Variants

Last verified: 2026-07-13  
Status: Accepted; shared persistence and history foundation implemented

## Context

`ChatMessage` currently stores each user or assistant message as an independent row. `ChatPersistenceService` loads rows by `SentAt`, and `ChatGenerationCoordinator` builds history from completed rows whose `SentAt` precedes the target assistant row. A regenerated assistant response created after later turns would therefore be placed after those turns and could build history from the wrong user message.

Completed responses must retain independent content, raw content, citations, settings, metrics, status, and errors. Failed retries must reuse the failed row. The design must be small enough for the three-day sprint and must support deterministic migration of valid legacy conversations.

## Options Considered

### A. Assistant self-reference plus stable message index

Each assistant row references the user row it answers. Every user row has a stable, 1-based `MessageIndex`; its assistant slot is the next index, and every variant for that response reuses the assistant slot. Assistant rows also have a 1-based `VariantIndex` and a selection flag.

This preserves the existing `ChatMessage` ownership of generation artifacts and adds no new table. It does require explicit relationship constraints and turn-aware queries.

### B. Explicit `ChatTurn` entity

A turn row would own one user message, many assistant variants, a selected assistant ID, and turn order. This is structurally clean and makes exactly-one selection easier to express, but it adds a table, two message relationships, more migration work, and broader persistence changes than the deadline allows.

### C. Variant group plus message order

A standalone variant group and order field would still need a stable relationship to the user message. It duplicates the purpose of a reply relationship and makes safe legacy backfill less obvious.

## Decision

Adopt Option A when Phase 2 is explicitly approved.

Add these members to `Domain.Entities.ChatMessage`:

```csharp
public int? MessageIndex { get; set; }
public Guid? InReplyToMessageId { get; set; }
public int? VariantIndex { get; set; }
public bool IsSelectedVariant { get; set; }
public virtual ChatMessage? InReplyToMessage { get; set; }
public virtual ICollection<ChatMessage> AssistantVariants { get; } = [];
```

Rules:

- `MessageIndex` is 1-based and required for user and assistant rows; it is null for system rows.
- For turn `N`, the user has `MessageIndex = 2N - 1` and every assistant variant has `MessageIndex = 2N`.
- A user row has no reply target or variant index and is never selected.
- An assistant row references exactly one user row in the same session and turn.
- `VariantIndex` is 1-based and unique within the replied-to user turn.
- exactly one assistant variant is selected whenever a user turn has assistant variants;
- the first assistant response is variant 1 and selected;
- retry keeps the same message ID, message index, variant index, and selection;
- regeneration creates the next variant index and selects it in the same transaction;
- switching selection never invokes generation.

## EF And PostgreSQL Requirements

Configure:

- an alternate key on `(Id, ChatSessionId)`;
- a self-reference from `(InReplyToMessageId, ChatSessionId)` to that alternate key with `DeleteBehavior.NoAction`;
- a unique filtered index on `(ChatSessionId, MessageIndex)` for `ChatRole.User`;
- a unique filtered index on `(InReplyToMessageId, VariantIndex)` for assistant rows;
- a unique filtered index on `InReplyToMessageId` where `IsSelectedVariant = true`;
- an index on `(ChatSessionId, MessageIndex, VariantIndex)` for ordered session and history queries;
- a check constraint encoding the role-specific nullability and positive-index rules;
- a deferred PostgreSQL constraint trigger that verifies every assistant reply target is a user row in the same session, its `MessageIndex` equals the target user's index plus one, and every user turn with assistant rows has exactly one selected variant at transaction commit.

Session deletion continues to cascade from `ChatSession` to all messages. The self-reference is `NoAction` so it does not create a second cascade path. Direct deletion of a user message is rejected until its assistant variants are removed; the supported product operation deletes the whole owned session.

## Legacy Backfill Policy

The current write path creates one user row followed by one assistant row, and current seed data uses increasing timestamps. The migration must nevertheless run a preflight before assigning relationships.

For each session, order legacy rows by `(SentAt, CreatedAt, Id)`. Empty sessions are valid. A non-empty legacy session is safe only when:

- it contains user and assistant roles only;
- the first row is a user;
- roles alternate user, assistant;
- every user has exactly one following assistant before the next user;
- no assistant is orphaned.

For safe sessions, assign `MessageIndex` with `row_number()` over the full alternating message sequence starting at 1, set each assistant's `InReplyToMessageId` to the preceding user ID, and set the assistant to `VariantIndex = 1` and selected.

If any session fails preflight, the migration must raise an exception and make no schema state final. The owner must inspect and explicitly map or remove the anomalous rows before rerunning. Timestamp order alone is not a reliable semantic backfill for non-alternating data, so the migration must not guess.

## History And Read Behavior

History for a target assistant is ordered by ascending `MessageIndex` and contains:

1. completed rows whose `MessageIndex` is less than the target assistant slot;
2. only the selected completed assistant row for each earlier assistant slot;
3. therefore the target's user row immediately before the target slot;
4. never an unselected variant and never a later logical message.

`MaxHistoryMessages` is applied to that flattened ordered sequence while preserving the target user row. `SentAt` remains display and reporting metadata but is not conversation order. A regenerated assistant keeps its original `MessageIndex` even though it receives a new `SentAt`.

The normal session DTO contains every user row and only the selected assistant row for each turn. A compact variant navigation object lists variant identities, indices, statuses, and selection. A separate handler fetches one variant's full content and citations.

## Mutation Behavior

Failed retry is valid only for an owned assistant row in `Failed` status. It clears `Content`, `RawContent`, `GenerationErrors`, citations and occurrences, generation settings, and generation metrics; sets the same row to `Pending`; and invokes the existing coordinator.

Completed regeneration is valid only for an owned completed assistant row. It creates a new pending assistant row for the same reply target and turn, assigns `max(VariantIndex) + 1`, deselects the old variant, selects the new variant, and invokes the existing coordinator with current resolved subject configuration.

Selection is valid only for an assistant variant in the owned session. It changes selection atomically and does not alter generation artifacts.

## Consequences

- Regeneration after later messages cannot corrupt turn order.
- Existing settings, metrics, citations, content, status, and errors remain naturally variant-scoped.
- Entity relationships, atomic persistence, selected session projections, mappings, coordinator history, and focused repository tests are implemented and turn-aware.
- The database uses one raw-SQL deferred constraint trigger in addition to EF indexes and checks.
- A still owns the PageModel handlers, SignalR behavior, frontend state, and user-visible retry/regeneration/selection controls.

## Implementation Anchors

- `Domain/Entities/ChatMessage.cs`
- `Domain/Contracts/DTOs/ResolvedChatMessage.cs`
- `Domain/Contracts/DTOs/ResolvedChatVariantNavigation.cs`
- `Domain/Contracts/DTOs/ChatExchangeResult.cs`
- `Domain/Contracts/IChatPersistenceService.cs`
- `BusinessLayer/Services/AI/Chat/ChatPersistenceService.cs`
- `DataAccessLayer/Repositories/ChatTurnRepository.cs`
- `DataAccessLayer/Data/EduChatAIDbContext.cs`
- `DataAccessLayer/Migrations/`
- `PresentationLayer/DTOs/ChatSessionDto.cs`
- `PresentationLayer/DTOs/ChatVariantDtos.cs`
- `PresentationLayer/Mappings/ChatMappingProfile.cs`
- `UnitTests/ChatTurnRepositoryIntegrationTests.cs`
- `UnitTests/ChatGenerationHistoryTests.cs`
- [Three-day shared contracts](../contracts/three-day-delivery-contracts.md)
