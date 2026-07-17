# ADR 0004: Resource-Centric Realtime Notifications

Last verified: 2026-07-13  
Status: Accepted

## Context

Subjects, chapters, memberships, documents, users, and chat sessions can appear on multiple pages. Page-specific SignalR groups and payloads would duplicate server logic and couple domain changes to individual screens. Document indexing also emits progress-oriented updates whose delivery and persistence semantics differ from ordinary CRUD notifications.

## Decision

- Publish generalized `ResourceUpdate` payloads describing resource type, action, identity, name, and relationship properties.
- Support subscriptions to resource types, individual resources, and related collections.
- Derive SignalR groups from resource semantics on the server.
- Pass the caller connection ID with mutating HTTP requests and exclude that caller from broadcasts; the caller updates from its local operation.
- Keep the document indexing status stream separate because it carries progress-oriented status data rather than ordinary resource CRUD changes.
- Client adapters translate hub messages and connection lifecycle into semantic page events; page code decides how state changes.
- SignalR callbacks do not directly edit page DOM.
- Realtime status payloads carry freshness metadata sufficient for the client to reject stale or duplicate observations.
- Status-hub availability after initial connection or reconnection triggers silent canonical HTTP reconciliation.
- Durable phase transitions may be persisted before notification, while intermediate progress may remain transient.
- Cross-transport load generations, buffering, and freshness ordering follow ADR 0009.

## Consequences

- New pages can reuse domain-level notifications without new server event types.
- Payload relationship properties must remain sufficient for group derivation.
- Reconnection logic must restore subscriptions and reconcile data that may have changed during a delivery gap.
- A semantic page event is not permission to bypass the page's state mutators.
- Realtime delivery is not a durable event log; canonical HTTP refresh remains necessary after connection gaps.
- Transient progress cannot be reconstructed from persistence unless a future design stores it explicitly.

## Implementation Anchors

- `Domain/Contracts/DTOs/ResourceUpdate.cs`
- `Domain/Contracts/DTOs/DocumentStatusUpdate.cs`
- `PresentationLayer/Realtime/SignalRResourceRealtimeNotifier.cs`
- `PresentationLayer/Realtime/ResourceHub.cs`
- `PresentationLayer/Realtime/SignalRDocumentStatusRealtimeNotifier.cs`
- `PresentationLayer/wwwroot/js/documents/document-library-signalr.js`
- [ADR 0009](0009-client-resource-loading-and-realtime-freshness.md)
