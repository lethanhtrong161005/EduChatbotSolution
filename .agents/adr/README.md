# Architecture Decision Records

Last verified: 2026-07-13

ADRs capture decisions and their consequences. Current code is authoritative; verify implementation anchors before relying on an ADR.

| ADR | Status | Decision |
|---|---|---|
| [0001](0001-layered-razor-jquery-architecture.md) | Accepted | Preserve layered .NET, Razor Pages, and jQuery architecture |
| [0002](0002-document-chapter-subject-integrity.md) | Accepted | Enforce same-subject document/chapter tags in the database |
| [0003](0003-client-state-and-event-architecture.md) | Accepted | Use privileged state mutators and event-driven rendering |
| [0004](0004-resource-centric-realtime-notifications.md) | Accepted | Broadcast resource changes rather than page-specific events |
| [0005](0005-document-file-lifecycle-and-storage.md) | Accepted | Separate reception, validation, staging, buffering, and durable storage |
| [0006](0006-subject-storage-configuration.md) | Accepted | Resolve durable policy per subject and persist actual strategy |
| [0007](0007-deterministic-citation-authority.md) | Proposed | Derive citation occurrence mapping deterministically outside the model |
| [0008](0008-docker-first-development-and-build.md) | Accepted | Use a portable Docker-first build while preserving Visual Studio-specific behavior |
| [0009](0009-client-resource-loading-and-realtime-freshness.md) | Accepted | Reconcile cached HTTP resources and realtime updates by generation and freshness |
| [0010](0010-chat-turns-and-assistant-variants.md) | Proposed | Group assistant variants by user reply and stable turn order |
| [0011](0011-three-day-experiments-and-measured-usage.md) | Proposed | Run serialized production-path experiments and report measured usage honestly |

New ADRs should include `Status`, `Context`, `Decision`, `Consequences`, and `Implementation Anchors`. Supersede old ADRs rather than silently rewriting their decisions.
