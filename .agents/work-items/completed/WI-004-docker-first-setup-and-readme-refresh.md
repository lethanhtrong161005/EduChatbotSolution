# WI-004: Docker-First Setup And README Refresh

Last verified: 2026-07-11  
Status: Completed

## Goal

Make Docker Compose the reliable primary setup, preserve the existing Visual Studio workflow, remove tracked development secrets, and replace the obsolete public README with current project guidance.

## Implemented Outcome

- Added a Node 24 Docker asset stage that installs locked npm dependencies and compiles Tailwind before the .NET build.
- Added an explicit Docker-only Tailwind skip while retaining mandatory Tailwind generation for ordinary host builds.
- Added the native GSSAPI runtime library needed to avoid PostgreSQL client startup warnings.
- Split Compose into a portable CPU base, an opt-in GPU override, and a Visual Studio debug override.
- Added PostgreSQL and Redis health checks and readiness-aware service dependencies.
- Disabled HTTPS redirection only for the HTTP-only CLI container.
- Replaced environment-name database selection with the single `ConnectionStrings:Database` key.
- Preserved the former shared connection string and existing development integration values in User Secrets before removing their tracked copies. No values are recorded here.
- Added an ignored `.env` workflow and a safe `.env.example` containing hierarchical ASP.NET configuration keys.
- Rewrote the root README for developers and evaluators using the current Razor Pages, Docker, AI-provider, document-processing, realtime, payment, and test architecture.

## Locked Decisions

- Docker Compose is the primary setup path.
- The Docker runtime image does not contain Node.js.
- CLI Compose defaults to CPU and HTTP port `8080`.
- Visual Studio debugging continues to use GPU Ollama, HTTPS certificates, and mounted User Secrets.
- Host EF tooling reaches the container database through `localhost:5432`; containers use the Compose service name `db`.
- Shared developer databases are selected by overriding `ConnectionStrings:Database`, not by switching ASP.NET environments.
- Credentials are exchanged separately and stored in `.env`, User Secrets, or the future hosting environment.

## Verification Recorded

- Reproduced the original clean-image failure: `npm: not found` from the project Tailwind target.
- Clean Docker image build completed after adding the asset stage.
- The final image and live CSS endpoint contained a nonempty `styles.css` of 86,709 bytes.
- Base, GPU, and Visual Studio Compose combinations parsed successfully.
- Normalized configuration confirmed CPU base, GPU override, Visual Studio GPU, HTTPS, and User Secrets mounts.
- PostgreSQL and Redis reported healthy; the web app, CSS endpoint, and Ollama API returned HTTP 200.
- Startup migrations and idempotent seed checks completed; the prior GSSAPI and HTTPS-redirection warnings were absent.
- Host solution build completed with zero warnings and errors and visibly ran Tailwind 4.3.1.
- NUnit passed 53 of 53 tests.

## Post-Completion Visual Studio Compatibility Adjustment

The initial Dockerfile placed the Node asset stage before the ASP.NET `base` stage. Visual Studio Fast mode generates a Compose build targeting `base` and failed with that ordering. The working follow-up keeps `base` first while leaving the regular multi-stage image build unchanged.

The Visual Studio override now enables HTTPS redirection on fixed port `8081` and resets the inherited CLI `.env` file. Mounted User Secrets are therefore authoritative during Visual Studio debugging and their overlapping keys are not emitted as environment variables in generated Compose caches. `DependencyAwareStart` remains omitted because it tears down the Compose containers when debugging stops; the preferred normal debugging workflow leaves infrastructure running between sessions.

## Operational Notes

- CLI users must create `.env` from `.env.example` and supply the separately exchanged credentials.
- Visual Studio users configure credentials through User Secrets; `.env` is reserved for CLI Compose.
- OpenRouter and Gemini API keys are currently startup requirements because both clients are constructed eagerly.
- Pull `bge-m3` and `qwen3` manually before selecting Ollama-backed embedding or chat configurations.
- `docker compose down -v` removes PostgreSQL and Ollama volumes and is a destructive local reset.
- Codex may require elevated command permission to access the host Docker daemon; this is sandbox access, not a second Docker installation.

## Implementation Anchors

- `PresentationLayer/Dockerfile`
- `PresentationLayer/Presentation.csproj`
- `PresentationLayer/Program.cs`
- `PresentationLayer/appsettings.json`
- `docker-compose.yml`
- `docker-compose.gpu.yml`
- `docker-compose.vs.debug.yml`
- `README.md`

## Resume Prompt

This work item is complete. Do not rebuild the Docker setup from older README or Compose assumptions. For changes to container behavior, first verify ADR 0008 against the current Dockerfile, Compose files, project build target, and configuration providers.
