# EduChatAI Agent Instructions

Last verified: 2026-07-13

Repository code, migrations, tests, and configuration are the source of truth. Treat these notes as orientation, workflow guidance, and decision history. Verify any detail that may have changed before relying on it.

## Assistant Roles And Capabilities

Different assistants may have different access to the project. Determine the current assistant's actual capabilities before acting.

### Repository Agents

A repository agent has direct access to the current working tree and may be able to:

- inspect project files and `git status`;
- edit files;
- run builds, tests, migrations, Docker commands, and other verification;
- create diffs or commits when explicitly authorized.

Examples may include Codex or an IDE-integrated coding agent.

Repository agents must:

- inspect the current working tree before proposing or applying changes;
- preserve unrelated local edits;
- avoid reverting, overwriting, or reformatting files outside the requested scope;
- make focused changes only after implementation is authorized;
- run applicable verification and report exact results;
- distinguish commands actually run from commands merely recommended;
- never create commits, branches, migrations, dependencies, or broad refactors without explicit direction.

### Conversational Assistants

A conversational assistant may be able to read repository files through connectors, attachments, or pasted content, but does not necessarily have access to:

- the local working tree;
- uncommitted changes;
- local development configuration;
- command execution;
- direct file editing.

Conversational assistants must not imply that they inspected, modified, built, or tested anything they could not actually access.

When implementation is authorized, a conversational assistant should:

- provide complete, ready-to-paste changes grouped by file;
- prefer complete methods, classes, markup regions, or files over fragmented snippets;
- state exact replacement boundaries;
- include all dependent edits required for the proposed implementation;
- provide verification commands and manual checks;
- clearly identify anything that remains unverified;
- ask for current code when repository or attachment context may be stale.

If `.agents` files should be changed and the assistant lacks write access, it must first propose the intended documentation changes. After approval, it should provide complete ready-to-paste contents for every file to add or replace.

## Primary Collaboration Model

ChatGPT is the user's primary assistant for:

- requirements discussion;
- architecture and design;
- tradeoff analysis;
- acceptance criteria;
- localized implementation;
- code review;
- debugging;
- ready-to-paste code.

The user generally prefers to apply and adjust code manually rather than delegate every implementation to a repository agent.

Codex is primarily used for changes that benefit substantially from direct working-tree access, broad repository discovery, automated edits, or repeated verification.

Suggest a Codex handoff when a task involves one or more of:

- more than five substantively changed files;
- changes spanning several solution layers or subsystems;
- public contract or signature changes with many call sites;
- EF Core model changes, migrations, and associated service/presentation/test fallout;
- repository-wide renames or mechanical transformations;
- repeated build, test, and repair cycles;
- implementation scope that cannot be understood reliably from a small group of files.

File count is a heuristic, not a hard rule. A small migration may still suit Codex, while a larger set of trivial edits may still be practical as ready-to-paste changes.

The user decides whether to delegate. When suggesting Codex:

1. explain why repository-agent execution would help;
2. do not assume Codex has access to the current conversation;
3. prepare a decision-complete handoff if the user approves;
4. include requirements, accepted decisions, implementation anchors, hazards, exclusions, acceptance criteria, and verification steps.

## Working Mode

- Work discussion-first until the user explicitly authorizes implementation.
- Before proposing changes, inspect the relevant current code and applicable agent notes.
- Summarize the current design before treating older conversational context as authoritative.
- Surface inconsistencies, risks, and tradeoffs before implementation.
- Do not silently expand scope to fix adjacent issues.
- Point out adjacent bugs separately and let the user decide whether to include them.
- Keep accepted architectural decisions separate from temporary implementation notes.
- Do not assume another assistant has access to this conversation.

## Orientation Order

1. Read [README.md](README.md) and [context/project-overview.md](context/project-overview.md).
2. Read ADRs relevant to the requested subsystem.
3. Read the active work-item handoff when continuing existing work.
4. Inspect the current implementation.
5. If working-tree access exists, inspect `git status`.
6. Reconcile any disagreement in favor of current repository code and migrations.
7. For document-library work, inspect the Razor PageModel, markup, main JavaScript, templates, and SignalR adapter together.
8. For document-library loading or realtime ordering, read [ADR 0009](adr/0009-client-resource-loading-and-realtime-freshness.md).

## Project Conventions

- Preserve the four-project dependency direction: `Domain` <- `DataAccess` <- `Business` <- `Presentation`, with `Presentation` also consuming domain contracts.
- Keep Razor Pages plus jQuery. Do not propose a SPA framework as incidental cleanup.
- Prefer explicit service logic, exact-purpose DTOs, projected EF queries, and database constraints for cross-entity invariants.
- Domain contracts and other public-facing signatures use `cancellationToken`. Internal implementations conventionally shorten it to `cxlTkn`.
- All entities inherit `NaturalEntity` or `CategoryLikeEntity`. Their IDs are GUID and integer respectively.
- Entity timestamps are database-managed. `CreatedAt` uses `DEFAULT now()`; `UpdatedAt` uses the `update_timestamp()` trigger.
- Migrations require a useful `Down()` path. Any new timestamped table must install its update trigger.
- The current PostgreSQL timestamp function and trigger are both lowercase `update_timestamp`. Older migrations contain the historical names `Update_Timestamp_Function` and `UpdateTimestamp`; do not copy those names into new migrations.
- Use typed file-operation results. Cancellation throws; expected storage and not-found outcomes use result objects.
- Restrict direct `System.IO.File` access to local buffering, local staging, and local durable-storage implementations.
- Keep parsers stream-based and leave caller-owned streams open.
- Razor Page handlers that return JSON must defensively translate expected domain failures at the handler boundary:
  - Catch `BadRequestException` and return the endpoint's documented `400` JSON response;
  - Catch `EntityValidationException` and return the endpoint's documented `400` JSON response;
  - Catch `EntityNotFoundException` and return the endpoint's documented `404` JSON response;
  - Catch `EntityConflictException` and return the endpoint's documented `409` JSON response.
- Do not let expected request, validation, not-found, or conflict exceptions from JSON handlers fall through to the current HTML-oriented `CustomExceptionMiddleware`; AJAX/fetch callers must receive a parseable JSON error payload with the correct status code.
- Keep JSON-handler catches narrow. Do not catch `Exception`, do not convert unexpected failures into `401`/`403`, and do not suppress cancellation.
- Razor Page handlers that render HTML may rely on `CustomExceptionMiddleware` for expected domain and unexpected exceptions unless the page has a more specific user-facing recovery flow.
- Until WI-007 is completed, whether a handler is JSON-returning is determined by its contract and response behavior, not merely by the HTTP method or route.
- Treat Docker Compose as the primary setup. Keep the base Compose file CPU-portable and isolate GPU or Visual Studio behavior in explicit override files.
- Keep the ASP.NET `base` stage first in the Dockerfile; Visual Studio Fast mode targets it directly.
- Preserve the Tailwind build boundary: Docker generates CSS in the Node asset stage; host and Visual Studio builds run the project npm target unless `SkipTailwindBuild=true` is explicitly supplied by Docker.
- Use `ConnectionStrings:Database` across providers. Host defaults use `localhost`, Compose uses `db`, and User Secrets or hosting configuration supply overrides.
- Do not add empty optional environment variables to Compose: environment variables override mounted User Secrets. Use `.env` only for CLI credentials; the Visual Studio override must reset `env_file` and use mounted User Secrets.
- Frontend resource and view state may only be directly changed by `mutateState_*` methods.
- State mutators emit events; state resolution handlers derive or acquire state and do not manipulate DOM.
- UI handlers render or manipulate DOM and do not acquire resources.
- Use delegated jQuery handlers for dynamic markup.
- Escape user-provided and server-provided text in templates.
- Reserve the JavaScript identifier `document` for the browser DOM global. Use `doc` for a domain document resource and `documents` for collections.
- Every client load invocation must establish a new concurrency generation before returning a default or cached result.
- Resolve canonical no-selection defaults before consulting cache.
- `load*()` may use matching committed state; `reload*()` explicitly bypasses cache and returns the underlying promise.
- Cached collections retain complete contextual metadata, including empty results.
- Silent loads suppress lifecycle UI but still commit state and render through state events.
- Log actual request failures even when their generation is stale, but never let a stale request commit state or show failure UI.
- Realtime and HTTP freshness rules follow ADR 0009; neither transport wins unconditionally.

## Code Delivery Conventions

For localized ready-to-paste implementation:

- group changes by file;
- provide whole methods, classes, regions, or files;
- include required imports, registrations, markup, handlers, and templates;
- avoid unexplained placeholder code;
- preserve established names and architecture unless a change was explicitly accepted;
- separate required edits from optional improvements;
- state whether each block replaces existing code or is newly inserted;
- include expected behavior and verification steps.

Do not present pseudocode as completed implementation unless it is explicitly labeled as pseudocode.

## High-Risk Areas

- Document/chapter subject keys: changing `SubjectId` can invalidate composite relationship keys.
- Document storage: preserve staging/durable boundaries, deterministic retry names, and actual storage-method persistence.
- Chat citations: never trust a model to authoritatively map citation markers to chunks.
- Realtime updates: preserve resource-centric subscriptions, caller exclusion, state-driven rendering, generation-safe loading, and canonical recovery after connection gaps.
- Subscription dates, overlaps, upgrades, downgrades, and renewals are business rules; discuss changes first.
- Authentication and authorization failures must not be conflated with general exceptions.
- Exception presentation currently differs by endpoint type. JSON-returning handlers must map expected domain exceptions themselves until [WI-007](work-items/active/WI-007-request-aware-exception-middleware.md) makes `CustomExceptionMiddleware` request-aware.
- Configuration work must not expose credentials in tracked files, generated handoffs, logs, or agent notes.
- Docker and Visual Studio use intentionally different credential sources; do not accidentally reintroduce `.env` precedence into Visual Studio debugging.

## Verification

Repository agents should run applicable verification directly. Conversational assistants should provide the same commands as instructions and report them as unexecuted unless actual execution access exists.

### General

- Build: `dotnet build EduChatAI.slnx --no-restore`
- Tests: `dotnet test EduChatAI.slnx --no-build --no-restore`

### Docker And Compose

- Validate `docker-compose.yml`.
- Validate its GPU and Visual Studio override combinations with `docker compose ... config --quiet`.
- Confirm the normalized Visual Studio merge:
  - targets `base`;
  - has no inherited `env_file`;
  - enables GPU;
  - enables HTTPS on port `8081`;
  - mounts User Secrets.
- Do not enable dependency-aware Visual Studio startup. It tears down the Compose containers when debugging stops, contrary to the preferred persistent-infrastructure workflow.
- Build the web image with `docker compose build web`.
- Verify `/app/wwwroot/css/styles.css` is nonempty.
- Confirm PostgreSQL and Redis health.
- Confirm migration and startup logs.
- Confirm HTTP `200` from the web application.
- Confirm HTTP `200` from Ollama when the Ollama service is part of the tested setup.
- In a sandboxed Codex task, Docker CLI parsing may work while Docker daemon access requires command elevation. Use the host Docker daemon; do not configure a second Docker installation.

### EF Core

- Generate and inspect forward migration SQL.
- Generate and inspect rollback SQL.
- Check for pending model changes.
- Verify timestamp defaults and triggers for timestamped entities.
- Verify data-preservation behavior for nontrivial migrations.

### Frontend

- Exercise affected Razor Pages at desktop and mobile widths.
- Inspect the browser console.
- Inspect failed or unexpected network requests.
- Verify loading, empty, failure, success, cancellation, and realtime states relevant to the change.

Always report:

- commands actually run;
- commands not run;
- failures or warnings;
- residual risks;
- manual checks still required.

## Maintaining These Notes

Update `.agents` for noticeable milestones, including:

- completion of a feature or substantial work item;
- acceptance, rejection, or supersession of an architectural decision;
- a significant implementation change that affects future reasoning;
- discovery of an important invariant, hazard, or workflow constraint;
- pausing work at a point that must be resumable;
- transferring context to another assistant or task.

Context-transfer updates are allowed even when no implementation milestone has occurred. Assistants do not need to track or predict when such transfers are required; the user may request them explicitly.

Do not update `.agents` for every small UI adjustment, naming cleanup, or intermediate fix. Active work-item notes do not need to mirror every commit while implementation is ongoing. Repository code remains authoritative.

After feature completion, remind the user to consider:

- updating or completing the relevant work item;
- adding or updating an ADR if a durable decision changed;
- refreshing `context/project-overview.md` if project orientation changed;
- updating implementation anchors and verification records.

Record durable architectural decisions as ADRs. Do not turn temporary implementation notes into accepted decisions.

Move work items to `completed/` only after implementation and verification.

Do not store:

- secrets;
- connection strings;
- tokens;
- personal data;
- generated build output;
- large source-code excerpts;
- transient debugging logs.
