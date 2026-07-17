# ADR 0008: Docker-First Development And Build

Last verified: 2026-07-11  
Status: Accepted

## Context

The web project requires a generated Tailwind `styles.css`, but its Dockerfile previously ran the project build in a .NET SDK image without Node.js. A clean image build therefore failed when the `Presentation.csproj` Tailwind target invoked `npm`. The base Compose file also mixed portable services with Windows-specific Visual Studio mounts, required an NVIDIA GPU, duplicated local and shared database credentials, and exposed tracked integration secrets.

## Decision

- Treat Docker Compose as the primary setup path.
- Build Tailwind in a dedicated `node:24-alpine` stage using `npm ci`; copy only the generated stylesheet into the .NET build stage. Keep Node out of the final runtime image.
- Keep the ASP.NET `base` stage first in the Dockerfile. Visual Studio Fast mode targets that stage directly and does not tolerate moving an unrelated stage ahead of it.
- Keep host and Visual Studio project builds running Tailwind through `Presentation.csproj`. Docker passes `SkipTailwindBuild=true` only after the asset stage has produced the stylesheet.
- Install the minimal GSSAPI runtime dependency required by the PostgreSQL client in the final ASP.NET image.
- Keep `docker-compose.yml` portable, CPU-compatible, and HTTP-only on port `8080`.
- Put NVIDIA acceleration in `docker-compose.gpu.yml`; include it in Visual Studio through `AdditionalComposeFilePaths` while CLI users opt in explicitly.
- Put Windows User Secrets and development HTTPS certificate mounts in `docker-compose.vs.debug.yml`, not the automatically loaded general override.
- Reset the base CLI `env_file` in the Visual Studio override so environment variables from `.env` cannot override mounted User Secrets or be materialized into generated Visual Studio Compose caches.
- Enable HTTPS redirection explicitly for Visual Studio and publish its HTTPS endpoint on fixed port `8081`.
- Do not enable Visual Studio `DependencyAwareStart`. It tears down the Compose containers when debugging stops, while the preferred normal debugging workflow keeps infrastructure running between sessions. Compose health conditions remain part of the CLI stack, but Visual Studio dependency-aware launch behavior is not guaranteed.
- Use health checks for PostgreSQL and Redis and make the web service wait for healthy dependencies.
- Use one logical `ConnectionStrings:Database` key. Tracked app settings point host tools to `localhost`; Compose overrides the same key with the `db` service name; User Secrets or a hosting environment may replace it.
- Keep CLI integration credentials in an ignored `.env` file shaped from `.env.example`. Keep host and Visual Studio credentials in .NET User Secrets. Future hosting supplies the same hierarchical keys through its secret system.
- Do not track shared database credentials, API secrets, payment signing keys, personal sender addresses, or machine-specific callback URLs.
- Make HTTPS redirection configurable and disable it only in the portable HTTP Compose service.

## Consequences

- Clean Docker builds no longer depend on host Node.js or a pre-generated stylesheet.
- Visual Studio Fast mode still requires Node.js/npm because it builds the project on the host.
- Visual Studio credentials come from mounted User Secrets, while CLI Compose credentials come from `.env`.
- CPU-only machines can start the base stack; GPU users and Visual Studio apply the explicit GPU override.
- CLI Compose no longer depends on Windows `%APPDATA%` mounts or development certificates.
- Missing OpenRouter or Gemini keys still prevent application startup because those clients are registered eagerly; Docker users must populate `.env` before starting the web service.
- Ollama starts without models. `bge-m3` and `qwen3` remain explicit first-run pulls when Ollama-backed configurations are used.

## Implementation Anchors

- `PresentationLayer/Dockerfile`
- `PresentationLayer/Presentation.csproj`
- `PresentationLayer/Program.cs`
- `docker-compose.yml`
- `docker-compose.gpu.yml`
- `docker-compose.vs.debug.yml`
- `docker-compose.dcproj`
- `.env.example`
