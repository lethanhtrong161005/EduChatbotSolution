# WI-001: Upload Queue And Progress Modal

Last verified: 2026-07-13  
Status: Completed

## Goal

Make multi-file upload reliable and expose a non-blocking bottom-corner progress panel with per-file status, progress, cancellation, retry, and removal. Uploads continue while the user navigates the document library.

## Implemented Outcome

- Added an explicit upload-item state machine using `Pending`, `Starting`, `Active`, `Succeeded`, `Failed`, and `Aborted`.
- Limited upload execution to three simultaneous transfers.
- Captured subject and chapter destinations when each item is queued so later navigation cannot redirect an upload.
- Added a non-blocking, collapsible bottom-right queue panel with per-file progress and an aggregate settled/total progress ring.
- Added result-sensitive header states:
  - active work uses the normal upload treatment;
  - all succeeded uses success styling;
  - mixed terminal results use warning styling;
  - zero successful results use failure styling.
- Added legal row actions:
  - `Pending`, `Starting`, and `Active`: Cancel;
  - `Failed` and `Aborted`: Retry and Remove;
  - `Succeeded`: Remove.
- Kept a fixed two-slot action column so filenames, percentages, and progress bars do not shift when row actions change.
- Closing an outstanding queue cancels its cancellable items. Removing the final settled row closes the panel.
- Added retryable subject, chapter, detail, and document loading surfaces.
- Added loading, failure, retry, and successful-empty states for upload chapter tags.
- Added same-subject chapter validation before staging an uploaded file.
- Added query-aware immediate insertion of successful upload DTOs on matching page-one document queries.
- Added a final silent authoritative document reload after the queue settles.
- Added operation-specific upload and delete feedback instead of generic background-load success messages.

## Locked Invariants

- Queue and transfer callbacks change state only through `mutateState_Upload_*` methods.
- Upload-row and header markup is derived from state through templates and state events.
- A queued item retains its original `File`, subject ID, and immutable chapter-ID collection across retry.
- No more than three items may be `Starting` or `Active` at once.
- Any asynchronous preparation inserted between dequeue and send must recheck terminal state immediately before binding the transfer and calling `send`.
- `Pending`, `Starting`, and `Active` are cancellable; settled states are not.
- Retry resets the same failed or aborted queue item to `Pending`; it does not create a duplicate item.
- Remove is a client-side deletion of settled queue history, not an abort transition.
- Aggregate progress is settled items over total items. Outcome styling communicates whether terminal results were all successful, mixed, or all unsuccessful.
- Active cancellation is best-effort at the HTTP transport boundary. An `Aborted` client row does not guarantee that server-side persistence or scheduled jobs were compensated.

## Verification Recorded

Manual verification was performed under these constraints:

- failures were emulated by temporarily returning `BadRequest()` from Razor Page handlers;
- retries were verified by removing the forced failure and applying Visual Studio Hot Reload;
- latency and starting-state races were emulated with server breakpoints or a temporary pre-send delay;
- no dedicated network-throttling tools were available.

The reported checks covered:

- one and many-file queues;
- the three-transfer concurrency ceiling;
- pending, starting, and active cancellation;
- Cancel All behavior;
- failed and aborted retry;
- settled-row removal and final-row panel closure;
- immutable subject/chapter destinations across navigation and retry;
- minimized-panel behavior;
- mixed terminal outcomes and header styling;
- retryable load failures;
- no-chapter subject rendering;
- query-aware immediate upload insertion;
- final silent canonical reconciliation;
- escaped filenames and stable action layout.

All final reported WI-001 manual checks passed.

## Known Limitations And Deferred Work

- A late client-side abort may still leave a server-created document. The final silent reload deliberately discovers such state.
- The final canonical reload can shift page 2+ because a newly inserted document changes pagination boundaries.
- Plain-text files are still advertised by the client but may be rejected by signature/MIME validation. This defect is tracked separately in [WI-005](../active/WI-005-plain-text-document-validation.md).

## Implementation Anchors

- `33befed7528cfd9041def6a3c1635930378a0bd5` — concurrent queue and progress panel
- `880633cf392eb6f5544f40d1025361eef9b6ec39` — upload recovery and retryable resource loading
- `PresentationLayer/wwwroot/js/documents/document-library.js`
- `PresentationLayer/wwwroot/js/documents/document-library-templates.js`
- `PresentationLayer/Pages/Documents/Library.cshtml`
- `PresentationLayer/Pages/Documents/Library.cshtml.cs`
- `BusinessLayer/Services/AI/Indexing/DocumentIndexer.cs`

## Resume Prompt

This work item is complete. Do not rebuild the upload queue from the obsolete active-state assumptions. Treat TXT validation and true server-side cancellation compensation as separate work.
