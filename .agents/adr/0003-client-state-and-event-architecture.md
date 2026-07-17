# ADR 0003: Client State And Event Architecture

Last verified: 2026-07-13  
Status: Accepted

## Context

The document library combines navigation, caching, uploads, pagination, cancellation, and realtime updates. Direct DOM mutation from callbacks made state hard to reason about, allowed cached resources and rendered UI to diverge, and made overlapping resource loads difficult to reconcile.

## Decision

- Separate view state, resource state, and private controller state.
- Only `mutateState_*` functions directly mutate application state.
- State mutators emit named events; one event may have multiple independent handlers.
- `updateState_*` functions resolve, derive, or acquire state and do not manipulate DOM.
- `updateUi_*` functions render or manipulate DOM and do not acquire resources.
- Initial UI action handlers may read the DOM, but must move values into state immediately.
- Template functions return escaped markup and avoid side effects.
- Use delegated jQuery handlers for dynamic markup.
- Cache complete contextual resolution metadata even on empty collections so every result remains attributable to its query.
- Cache identity must include every query input that can change the returned collection.
- Resource replacement that can overlap realtime updates must follow ADR 0009's generation and freshness rules.
- Reserve the JavaScript identifier `document` for the browser DOM global. Use `doc` for a domain document resource and `documents` for collections.

## Consequences

- UI output is deterministic from state and can be reconstructed after realtime events or full rerenders.
- Event graphs may branch and are not required to form a single linear pipeline.
- Mutator naming is intentionally broader than `set*`; queue operations and compound transitions remain valid mutators.
- Collection metadata is part of state correctness, not incidental decoration.
- Item-level state events may update a focused surface without forcing a full collection rerender.
- Ad hoc badge, class, or queue-row changes outside this flow are architectural violations unless explicitly temporary.
- Large page modules require disciplined internal regions for mutators, event handlers, UI renderers, normalization, and consistency helpers.

## Implementation Anchors

- `PresentationLayer/wwwroot/js/documents/document-library.js`
- `PresentationLayer/wwwroot/js/documents/document-library-templates.js`
- `PresentationLayer/wwwroot/js/documents/document-library-signalr.js`
- [ADR 0009](0009-client-resource-loading-and-realtime-freshness.md)
