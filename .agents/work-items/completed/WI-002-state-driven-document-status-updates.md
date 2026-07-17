# WI-002: State-Driven Document Status Updates

Last verified: 2026-07-13  
Status: Completed

## Goal

Route realtime indexing updates through document resource state, reject stale observations, and preserve the freshest status when HTTP loads and SignalR updates overlap.

## Implemented Outcome

- Replaced direct status-badge DOM editing with the normal client state architecture:
  - the SignalR adapter emits `document:status:update`;
  - `onDocumentStatusUpdate` normalizes and buffers the payload;
  - `mutateState_Document_Status` updates the matching resource;
  - `state:document:status` re-renders the affected badge through a pure template.
- Added `SubjectId` and `UpdatedAt` to `DocumentFileDto`.
- Normalized document IDs, subject IDs, status progress, and status timestamps before committing resources to client state.
- Stored `status`, `statusProgress`, and `statusUpdatedAt` on each document resource.
- Rejected invalid statuses, invalid timestamps, delayed stale events, and duplicate events before state mutation.
- Kept documents outside the resolved collection from becoming partial phantom rows.
- Added a generation-scoped status buffer for SignalR updates received while a document HTTP load is in flight.
- Added a three-way status merge before an HTTP result replaces state:
  - the status in the HTTP response;
  - the status already committed to current client state;
  - the newest SignalR update buffered during that HTTP generation.
- Used the greatest `statusUpdatedAt` as the winner. On equal timestamps, current state beats HTTP and buffered realtime data beats both so transient progress is preserved.
- Added silent document reconciliation when the status hub first connects or reconnects.
- Added cache-aware `load*({ useCache: true })` methods and explicit `reload*()` wrappers that bypass cache.
- Made every load invocation replace its concurrency token before any default-state or cache shortcut.
- Resolved no-selection defaults before consulting cache.
- Preserved complete document query metadata: subject, chapter, search, page size, and page index.
- Kept actual request failures visible in the console even when their generation is stale, while preventing stale requests from committing state or showing failure UI.
- Standardized domain document variables as `doc`, reserving `document` for the browser DOM global.

## Locked Invariants

- SignalR adapters translate hub traffic into semantic page events and do not own page DOM or page state.
- Realtime document status changes enter state through a privileged mutator.
- A stale or duplicate status snapshot must be rejected before mutation and rendering.
- HTTP and SignalR are peers for freshness; neither transport wins unconditionally.
- During a document load generation, realtime updates are written both to visible state when possible and to that generation's reconciliation buffer.
- The buffer is a temporary write-ahead overlay, not a second long-lived store.
- A document-load response may replace state only after merging HTTP, current-state, and buffered snapshots.
- Missing document IDs do not create partial resource objects.
- Every load call creates a new concurrency generation before returning a default or cached result.
- Default-state resolution precedes cache lookup.
- `load*()` may reuse matching committed state; `reload*()` explicitly bypasses cache.
- Empty collections still carry the contextual metadata needed to identify their query.
- Silent loads suppress loading/failure/success UI events but still commit state and render through state events.
- Reserve the JavaScript identifier `document` for the DOM global. Use `doc` for a domain document and `documents` for collections.

## Verification Recorded

The final manual report covered:

- valid numeric and string status names;
- invalid numeric and string status names;
- progress statuses with valid, zero, missing, and invalid progress;
- non-progress statuses receiving an irrelevant progress value;
- ISO timestamp strings and epoch-millisecond timestamps;
- invalid timestamp values;
- progress values `0`, fractional values, and `100`;
- a newer SignalR update followed by an older delayed update;
- full document-list re-render after a realtime update;
- unknown and non-visible document IDs;
- in-flight HTTP reconciliation when the target document was already in state;
- in-flight HTTP reconciliation when the target document was absent from the previous query;
- status-hub reconnection reconciliation;
- subject deselection races across parallel subject-detail, chapter, and document loads;
- matching status-badge and delete-control resource IDs after badge replacement.

All final reported WI-002 checks passed.

## Known Limitations

- JavaScript timestamps have millisecond precision. Two server updates inside the same millisecond can compare as equal.
- Intermediate chunking progress is transient and is not reconstructed from persistent storage after a missed message.
- Client status durability ends when a document falls out of the current query scope.
- A future multi-instance or concurrent-reindex design may require a server-issued monotonic `StatusRevision`.
- The current indexer intentionally persists phase boundaries but broadcasts intermediate chunk progress without a database save. Replacing every progress push with `SaveAndUpdate` would add writes without making progress durable.

## Implementation Anchors

- `0fa65f7d1c1c4f6841ad36e7a2ba04cafd74279f` — realtime document status reconciliation
- `PresentationLayer/DTOs/DocumentLibraryDtos.cs`
- `PresentationLayer/Pages/Documents/Library.cshtml.cs`
- `PresentationLayer/wwwroot/js/documents/document-library.js`
- `PresentationLayer/wwwroot/js/documents/document-library-templates.js`
- `PresentationLayer/wwwroot/js/documents/document-library-signalr.js`
- `BusinessLayer/Services/AI/Indexing/DocumentIndexer.cs`
- `PresentationLayer/Realtime/SignalRDocumentStatusRealtimeNotifier.cs`

## Resume Prompt

This work item is complete. For future resource-loading or realtime-ordering changes, read ADR 0003, ADR 0004, and ADR 0009 before modifying the load-generation, cache, or status-reconciliation flow.
