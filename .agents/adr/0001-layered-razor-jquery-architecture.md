# ADR 0001: Layered Razor Pages And jQuery Architecture

Last verified: 2026-07-11  
Status: Accepted

## Context

EduChatAI evolved from MVC toward Razor Pages while retaining a four-project layered solution. Current interactive pages use server-rendered markup with jQuery modules and TailwindCSS. Replacing this with an SPA would change routing, state ownership, authorization boundaries, build tooling, and the user’s preferred development workflow.

## Decision

- Preserve `Domain`, `DataAccessLayer`, `BusinessLayer`, and `PresentationLayer` dependency direction.
- Build current product pages with Razor Pages, jQuery, TailwindCSS, and existing realtime modules.
- Use DTOs and service contracts across layers; do not let presentation code bypass business/data boundaries for convenience.
- Do not introduce React, Vue, or another SPA framework without a separate architectural decision.
- Do not mix Bootstrap layout conventions into current Tailwind document-library work, even though legacy assets remain.

## Consequences

- Rich client behavior is organized into page-specific state, templates, network handlers, and realtime adapters rather than components from an SPA framework.
- Large JavaScript modules require disciplined internal boundaries and pure render helpers.
- Legacy README and MVC terminology may be stale and must not override current project structure.

## Implementation Anchors

- `EduChatAI.slnx`
- `PresentationLayer/Pages`
- `PresentationLayer/wwwroot/js/documents`
- `PresentationLayer/Program.cs`
