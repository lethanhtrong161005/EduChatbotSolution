# EduChatAI Personal Agent Workspace

Last verified: 2026-07-13

This ignored directory holds private workflow guidance, architectural decisions, project orientation, and resumable handoffs for AI-assisted development.

Repository code, migrations, tests, and configuration remain authoritative.

## Start Here

- [Agent instructions](AGENTS.md)
- [Current project overview](context/project-overview.md)
- [Architecture decisions](adr/README.md)
- [Work-item process and index](work-items/README.md)
- [Three-day frozen contracts](contracts/three-day-delivery-contracts.md)
- [Teammate handoffs](handoffs/README.md)

## Assistant Types

The workspace is used by assistants with different capabilities.

### Repository Agents

Repository agents may have direct access to the local working tree and may be able to inspect files, edit code, and run verification commands.

They must inspect current code and `git status`, preserve unrelated edits, make only authorized changes, and report commands actually executed.

### Conversational Assistants

Conversational assistants may read project content through repository connectors, attached files, pasted code, or a mirrored copy of this workspace. They may not have access to uncommitted local changes, command execution, or direct file editing.

They must not claim to have inspected, modified, built, or tested anything outside their actual access.

When implementation is approved, they should provide complete ready-to-paste changes grouped by file, with replacement boundaries and verification instructions.

When `.agents` changes are appropriate but direct write access is unavailable, they should propose the changes first. After user approval, they should provide complete ready-to-paste contents for each file to add or replace.

## Collaboration Model

ChatGPT is the primary assistant for:

- discussion;
- architecture;
- requirements;
- tradeoffs;
- acceptance criteria;
- localized implementation;
- debugging;
- ready-to-paste code.

Codex or another repository agent is most useful for changes involving widespread repository modification, cross-layer fallout, migrations, many call sites, mechanical transformations, or repeated build/test/fix cycles.

A task affecting more than roughly five substantively changed files should prompt consideration of a Codex handoff, but file count is only a heuristic. The user decides whether implementation remains manual or is delegated.

Do not assume Codex or any other assistant has access to the conversation that produced a task. A handoff must be decision-complete.

## Status Vocabulary

- **Accepted ADR**: a deliberate constraint to preserve unless explicitly reconsidered.
- **Proposed ADR**: a preferred direction that still needs implementation-level agreement.
- **Active work item**: verified incomplete work with a resumable next step.
- **Completed work item**: verified implementation history, not a request to redo the work.

## Freshness Rule

Each document includes a last-verified date and implementation anchors.

Before acting on it:

1. inspect the relevant current repository code;
2. inspect migrations and tests where applicable;
3. reconcile differences in favor of the repository;
4. update the note only when a milestone, transfer, or durable decision warrants it.

The root repository `README.md` is the current public setup and architecture guide. These private notes add decision history, workflow constraints, and resumable context.

Active work-item notes do not need to reflect every intermediate commit. Code remains authoritative during ongoing implementation.

## When To Update This Workspace

Update these files for noticeable milestones:

- feature or work-item completion;
- accepted or superseded architectural decisions;
- major implementation changes that affect future work;
- important newly discovered hazards or invariants;
- resumable pauses;
- transfers between assistants, conversations, or tasks.

Context-transfer updates are an explicit exception to the milestone rule and may be created whenever the user needs them.

Avoid documentation churn for minor styling changes, small local fixes, or temporary implementation experiments.

After completing a feature, assistants should remind the user to consider updating the relevant work item, ADRs, project overview, and verification record.

## Content Boundaries

Do not place the following in `.agents`:

- credentials or secrets;
- connection strings;
- API keys or tokens;
- personal data;
- generated build output;
- large source-code copies;
- transient logs.

Use implementation anchors and concise summaries instead.
