# WI-003: Citation Occurrence Validation

Last verified: 2026-07-11  
Status: Active

## Goal

Persist trustworthy occurrence-level citation support while keeping marker-to-source mapping deterministic and outside model control.

## Verified Current State

- Generation supplies numbered context chunks and streams a raw answer containing `{{n}}` markers.
- `ProcessAnswer` validates marker ranges, maps referenced retrieval chunks to UI citation indices, and rewrites markers as `[[n]]`.
- Repeated references to the same chunk currently share one `ChunkUsage`; occurrence records and quotes are not populated.
- `ExtractCitationsAsync` exists but is not called by the generation flow.
- Its current model DTO redundantly returns `OccurrenceId` and `ChunkIndex` alongside quote validity.
- `CitationOccurrence` exists with `CitationId`, `OccurrenceIndex`, and `SupportingQuote`.

Implementation anchors:

- `BusinessLayer/Services/AI/Chat/ChatGenerationService.cs`
- `Domain/Entities/Citation.cs`
- `Domain/Entities/CitationOccurrence.cs`
- chat persistence and DTO mapping services discovered from the generation coordinator

## Locked Decisions

- Parse every marker occurrence deterministically from the raw answer.
- Use the supplied retrieval list as the authoritative chunk mapping.
- Treat repeated occurrences separately, even if they reference one chunk/citation.
- Ask the model to validate/extract support, not to choose authoritative occurrence or chunk identities.
- Invalid or absent support must remain representable without fabricating a quote.
- Avoid fuzzy quote grouping until a concrete requirement exists.

## Remaining Work

1. Specify an internal parsed-occurrence model containing stable order, raw marker, retrieval index, chunk ID, and deterministic citation index.
2. Refactor answer processing to return both grouped citation usage and ordered occurrences.
3. Define a minimal extraction request/response correlated by deterministic array position or a server-issued opaque token.
4. Validate response count and shape; fail closed for missing, extra, reordered, or unsupported items.
5. Attach validated supporting quotes to occurrence results.
6. Extend generation result, persistence, query DTOs, and UI source details as required.
7. Add tests for repeated markers, invalid ranges, malformed model output, null support, and marker order.

## Hazards And Boundaries

- Do not let a model-returned chunk index override deterministic parsing.
- Preserve streaming raw-answer behavior and final `[[citationIndex]]` client markup.
- Decide nullability before changing the database: `SupportingQuote` is currently non-nullable, while unsupported extraction logically has no quote.
- Keep citation index stable per rendered source while occurrence index remains per marker occurrence.

## Acceptance Criteria

- Every valid marker occurrence has deterministic source and order independent of model output.
- Repeated markers create separate occurrence records.
- Unsupported or malformed extraction cannot fabricate or remap a source.
- Persisted citations and occurrences round-trip through chat DTOs without breaking inline citation rendering.
- Tests cover valid, repeated, invalid, missing, extra, and reordered extraction cases.

## Resume Prompt

Read ADR 0007 and inspect generation plus persistence paths. First draft the parsed-occurrence and extraction I/O shapes, including unsupported-quote nullability, and discuss them before modifying code or migrations.
