# ADR 0006: Subject-Scoped Durable Storage Configuration

Last verified: 2026-07-11  
Status: Accepted

## Context

Storage policy does not belong in AI-named configuration entities. A global storage entity would add complexity before a global override is required, while storing policy only on each document would duplicate subject-wide intent and confuse desired policy with actual storage.

## Decision

- Persist subject policy in dedicated `SubjectStorageConfiguration`, a shared-key one-to-one dependant of `Subject`.
- Do not add global storage configuration yet.
- Resolve policy during durable persistence. An explicit supported subject method wins; missing, null, unspecified, or unsupported policy falls back to Supabase.
- Persist the actual strategy used on `Document.StorageMethod` only after durable storage succeeds.
- Keep `Document.StorageMethod.Unspecified = 0`, `LocalHardDrive = 1`, and `Supabase = 2`.
- New entities default to `Unspecified`; reads may use staging while unspecified, but durable move/delete do not guess.

## Consequences

- Storage policy is separated from AI concerns and can evolve independently.
- Per-document exceptions or global precedence require future decisions rather than implicit behavior.
- Existing durable documents are backfilled as Supabase by the introducing migration; future document rows default to unspecified.
- The configuration table requires `CreatedAt DEFAULT now()` and the `update_timestamp()` trigger.

## Implementation Anchors

- `Domain/Entities/SubjectStorageConfiguration.cs`
- `BusinessLayer/Services/Documents/File/DocumentStorageMethodResolver.cs`
- `Domain/Entities/Document.cs`
- `DataAccessLayer/Migrations/20260710202753_AddDocumentStorageConfiguration.cs`
