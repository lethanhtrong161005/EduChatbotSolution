# ADR 0002: Document And Chapter Subject Integrity

Last verified: 2026-07-11  
Status: Accepted

## Context

Every document belongs to one subject. Chapter tags are optional, but any tagged chapter must belong to the same subject as the document. Service-only validation would allow races, imports, or future code paths to create invalid cross-subject associations.

## Decision

- Model `Document` and `Chapter` as many-to-many through explicit `DocumentChapter`.
- Include `SubjectId` on the join.
- Use composite foreign keys against principal/alternate keys `(Document.Id, Document.SubjectId)` and `(Chapter.Id, Chapter.SubjectId)`.
- Treat an untagged document as subject-wide, not invalid.
- Validate requested chapter IDs in services for useful errors while retaining database enforcement.

## Consequences

- Cross-subject chapter tagging is rejected regardless of application entry point.
- Changing `SubjectId` on an existing document or chapter affects relationship keys and requires deliberate handling.
- EF mappings and migrations are more complex than a conventional two-column join table.

## Implementation Anchors

- `Domain/Entities/DocumentChapter.cs`
- `Domain/Entities/Document.cs`
- `Domain/Entities/Chapter.cs`
- `DataAccessLayer/Data/EduChatAIDbContext.cs`
