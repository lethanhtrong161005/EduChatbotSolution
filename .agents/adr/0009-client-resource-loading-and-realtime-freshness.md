# ADR 0009: Client Resource Loading And Realtime Freshness

Last verified: 2026-07-13  
Status: Accepted

## Context

The document library can issue parallel HTTP requests while selections, searches, pagination, uploads, deletes, and SignalR updates change client state. A request may complete after its result is obsolete. A realtime status message may also arrive late, be duplicated, or overlap an HTTP response whose snapshot was produced earlier.

Neither HTTP nor SignalR is unconditionally fresher. The client needs one ordering model that prevents stale requests from redrawing the page and prevents either transport from regressing document status.

## Decision

### Load generations

- Every load invocation creates and stores a fresh concurrency token before any return path.
- The token is replaced even when the method resolves a no-selection default or returns cached state.
- Only the current generation may commit a response or emit user-facing failure state.
- Actual request failures are logged for diagnostics even when their generation is stale.

### Defaults and cache

- Resolve the canonical no-selection/default state before consulting cache.
- `load*()` methods use matching committed state by default.
- `reload*()` methods explicitly bypass cache and return the underlying promise.
- Cache identity includes every input that can affect the returned resource.
- Empty collections retain the same contextual metadata as non-empty results.
- Silent loads suppress lifecycle loading/success/failure UI but still commit state and render through state events.

### Realtime status freshness

- Document resources carry `status`, `statusProgress`, and `statusUpdatedAt`.
- Invalid statuses and timestamps are ignored.
- An incoming realtime snapshot whose timestamp is less than or equal to the resource's current timestamp is rejected before mutation.
- Missing document IDs do not create partial phantom resources.

### Generation-scoped reconciliation buffer

- While a document HTTP generation is active, valid SignalR status updates are also written to a buffer owned by that generation.
- The buffer preserves updates for:
  - documents already visible in current state;
  - documents expected in the incoming query but absent from the previous collection.
- The buffer is a temporary write-ahead overlay and is discarded when its generation completes or is superseded.

### Three-way merge

Before an HTTP document result replaces state, compare:

1. the status snapshot in the HTTP DTO;
2. the status snapshot already committed to the matching current-state document;
3. the newest SignalR snapshot buffered during the active HTTP generation.

The greatest `statusUpdatedAt` wins. Candidate order is intentional for equal timestamps: current state beats HTTP, and buffered realtime data beats both, preserving transient progress.

### Connection recovery

- Initial status-hub connection and later reconnection both trigger a silent authoritative document reload.
- Timestamp comparison prevents stale data from winning, while the reload repairs events missed during a delivery gap.

## Consequences

- Delayed HTTP responses cannot redraw state after selection or query changes.
- Delayed, duplicated, or out-of-order SignalR messages cannot regress document status.
- Visible status updates survive full list rerenders.
- Realtime updates for an incoming page survive the transition without creating phantom rows.
- Load methods have a consistent distinction between cached resolution and forced retrieval.
- The implementation carries additional metadata and a short-lived reconciliation buffer.
- JavaScript timestamp comparison has millisecond precision. A future multi-instance or concurrent-reindex design may require a server-issued monotonic `StatusRevision`.
- Intermediate indexing progress remains transient unless persistence is explicitly added in a future decision.

## Implementation Anchors

- `0fa65f7d1c1c4f6841ad36e7a2ba04cafd74279f`
- `PresentationLayer/DTOs/DocumentLibraryDtos.cs`
- `PresentationLayer/Pages/Documents/Library.cshtml.cs`
- `PresentationLayer/wwwroot/js/documents/document-library.js`
- `PresentationLayer/wwwroot/js/documents/document-library-signalr.js`
- `PresentationLayer/Realtime/SignalRDocumentStatusRealtimeNotifier.cs`
- `BusinessLayer/Services/AI/Indexing/DocumentIndexer.cs`
- [ADR 0003](0003-client-state-and-event-architecture.md)
- [ADR 0004](0004-resource-centric-realtime-notifications.md)
