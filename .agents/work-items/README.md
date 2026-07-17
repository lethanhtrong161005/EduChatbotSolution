# Work-Item Handoffs

Last verified: 2026-07-14

Work-item files make a task resumable without replaying an entire conversation. Verify their implementation anchors before acting.

## Active

- **Current hotspot:** [WI-006: Three-day four-flow delivery](active/WI-006-three-day-four-flow-delivery.md)
- [WI-003: Citation occurrence validation](active/WI-003-citation-occurrence-validation.md)
- [WI-005: Plain-text document validation](active/WI-005-plain-text-document-validation.md)
- [WI-007: Request-aware exception middleware](active/WI-007-request-aware-exception-middleware.md)

## Completed

- [WI-000: Document file semantics refactor](completed/WI-000-document-file-semantics-refactor.md)
- [WI-001: Upload queue and progress modal](completed/WI-001-upload-queue-and-progress-modal.md)
- [WI-002: State-driven document status updates](completed/WI-002-state-driven-document-status-updates.md)
- [WI-004: Docker-first setup and README refresh](completed/WI-004-docker-first-setup-and-readme-refresh.md)

## Handoff Format

Every handoff should contain:

1. goal and user-visible outcome;
2. verified current state with implementation anchors;
3. decisions already locked by discussion or ADR;
4. remaining work in dependency order;
5. hazards and explicit out-of-scope boundaries;
6. acceptance criteria and verification commands;
7. a short resume prompt for the next agent.

Move a file to `completed/` only after implementation and verification. If a decision changes, update or supersede its ADR as well as the handoff.
