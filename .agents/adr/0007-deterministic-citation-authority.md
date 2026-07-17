# ADR 0007: Deterministic Citation Authority

Last verified: 2026-07-11  
Status: Proposed

## Context

The answer model emits chunk markers such as `{{1}}`. Current processing deterministically validates the marker range and groups citations by referenced chunk, but occurrence-level supporting quotes are not persisted. Citation-extraction scaffolding asks a model to return occurrence and chunk identifiers that are already encoded in the answer, creating an avoidable trust and reconciliation problem.

## Decision

- Parse marker occurrences from the raw answer deterministically and preserve their order.
- Derive the authoritative source chunk from the parsed marker and the exact retrieval list supplied to generation.
- Assign UI citation indices deterministically; do not trust model-returned indices or chunk mappings.
- Ask the model only whether a specific occurrence is supported and, if so, for the smallest supporting quote.
- Persist repeated marker occurrences separately as `CitationOccurrence` records, even when they share one `Citation`/chunk.
- Defer fuzzy quote grouping and deduplication until a concrete need is demonstrated.

## Consequences

- Malformed, missing, extra, or reordered model output cannot silently remap sources.
- The extraction contract can be smaller but requires deterministic correlation with parsed occurrences.
- Current extraction and persistence code must change before this ADR can become accepted.

## Implementation Anchors

- `BusinessLayer/Services/AI/Chat/ChatGenerationService.cs`
- `Domain/Entities/Citation.cs`
- `Domain/Entities/CitationOccurrence.cs`
- `PresentationLayer/wwwroot/js/chat/chat-templates.js`
