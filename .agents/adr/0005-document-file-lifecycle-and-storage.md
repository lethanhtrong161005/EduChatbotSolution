# ADR 0005: Document File Lifecycle And Storage Boundaries

Last verified: 2026-07-11  
Status: Accepted

## Context

The former temporary-storage service combined MIME validation, local buffering, and filesystem storage. `Document.FilePath` ambiguously represented temporary and durable locations, strategies depended on entities, Supabase buffered whole files, and parsers required local paths.

## Decision

- Separate request reception, content validation, staging storage, local SDK buffering, and durable storage.
- Validate before staging. Seekable inputs are reset; non-seekable inputs are replayed once through an `ILocalFileLease`.
- Treat staging and durable locators as opaque outside their implementations.
- Use `ILocalFileLease : IDisposable, IAsyncDisposable`; disposal is idempotent and a transferred read stream owns final cleanup.
- Durable strategies are entity-free and accept streams, storage names, locators, and directories.
- Use typed locator, read, deletion, validation, and reception results. Cancellation continues to throw.
- Generate deterministic durable names from document ID and canonical validated extension in `DocumentFileService`.
- Make durable stores overwrite-safe so a failed database update can retry the same physical name.
- Parsers consume caller-owned readable, seekable streams positioned at zero and leave them open.
- Restrict direct filesystem APIs to local buffer, local staging, and local durable implementations.
- `Received = 0` represents successful ingestion. `Unspecified` storage means the file has not yet reached durable storage.

## Consequences

- Staging can move off local disk without changing consumers.
- Supabase path-only SDK operations use disk leases instead of whole-file byte arrays.
- `OpenReadAsync` routes `Unspecified` documents to staging and durable documents by recorded strategy.
- Move and delete fail for unspecified or unknown durable methods.
- After durable state is committed, staging deletion is best effort and must not reverse success.

## Implementation Anchors

- `BusinessLayer/Services/Documents/File/DocumentFileReceptionService.cs`
- `BusinessLayer/Services/Documents/File/DocumentFileService.cs`
- `Domain/Contracts/IDurableStorageStrategy.cs`
- `BusinessLayer/Services/AI/Indexing/Parsing/LocationAnnotatedParser.cs`
