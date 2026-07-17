# WI-000: Document File Semantics Refactor

Last verified: 2026-07-11  
Status: Completed with review follow-ups

## Goal

Replace path-based temporary/durable ambiguity with explicit reception, validation, staging, buffering, durable strategy, and stream-reading boundaries.

## Implemented Outcome

- Added request-scoped reception and separate MIME/extension validation.
- Replaced temporary storage with opaque-locator staging operations.
- Added idempotent local file leases for non-seekable replay and path-only SDK calls.
- Replaced entity-aware file services with local and Supabase durable strategies.
- Renamed document locators, removed durable `FileName`, and persisted actual storage method.
- Added dedicated subject storage configuration with Supabase fallback.
- Made parsing and presentation reads stream-based.
- Added typed locator, read, deletion, validation, and reception results.
- Renamed initial status and durable directory semantics to `Received`.
- Added a Hangfire persistence adapter so typed persistence failure fails the job before indexing continuations.
- Regenerated the storage migration with data-preserving forward migration, timestamp trigger, Supabase backfill, and best-effort rollback reconstruction.

## Locked Invariants

- Validate before staging.
- `Unspecified` means not durably persisted; reads route to staging, durable mutations fail.
- Durable filenames are deterministic from document ID and canonical type extension.
- Database state changes only after physical durable storage succeeds.
- Staging cleanup happens after database success and is best effort.
- Supabase never buffers a whole document into a `byte[]`.
- Parsers do not dispose caller-owned streams.

## Verification Recorded

- Solution build succeeded with zero warnings.
- NUnit suite passed `53/53` after subsequent result/directory naming adjustments.
- EF reported no pending model changes when the migration was last verified.
- Forward and rollback SQL were generated and inspected.

## Review Follow-Ups

1. `Library.OnDeleteAsync` currently catches every exception and returns unauthorized. Restore targeted authentication/claim handling so cancellation and operational failures are not mislabeled.
2. `DocumentFileDirectory.Received` and `FileDirReceived` are current names, but `FileStorageOptions.FileDirectoryUploaded` remains. Rename the option and strategy references for semantic consistency if compatibility permits.

## Implementation Anchors

- `BusinessLayer/Services/Documents/File`
- `Domain/Contracts/IDocumentFileService.cs`
- `Domain/Contracts/IDurableStorageStrategy.cs`
- `DataAccessLayer/Migrations/20260710202753_AddDocumentStorageConfiguration.cs`
- `UnitTests/DocumentFileServiceTests.cs`

## Resume Prompt

This work item is complete. Do not rebuild it. If addressing a review follow-up, inspect current code and make that follow-up a separate focused task with its own verification.
