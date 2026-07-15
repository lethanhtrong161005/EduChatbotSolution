# EduChatAI

EduChatAI is a Vietnamese educational knowledge-base and chatbot application. It combines subject-based document management, background document indexing, retrieval-augmented chat with citations, realtime updates, authentication, subscriptions, and payment workflows.

The application is under active development. Docker Compose is the primary setup path; running the ASP.NET Core project directly is supported for development and EF Core tooling.

## Technology

| Area | Current implementation |
| --- | --- |
| Web | .NET 10, ASP.NET Core Razor Pages, jQuery, Tailwind CSS 4 |
| Data | EF Core 10, PostgreSQL 18, Npgsql, pgvector |
| Authentication | ASP.NET Core Identity, cookie authentication, Google OAuth |
| Background work | Hangfire with PostgreSQL storage |
| Realtime | SignalR resource, document-status, chat, and comment hubs |
| AI providers | Ollama, OpenRouter, and Gemini through `Microsoft.Extensions.AI` |
| File storage | Local staging plus configurable local or Supabase durable storage |
| Cache | Redis and RedisInsight |
| Document parsing | PDF, DOCX, PPTX, and text stream-based parsers |
| Testing | NUnit, Moq, and coverlet |

## What Is Implemented

- Subjects, chapters, memberships, and role-sensitive document access.
- Multi-file document reception, validation, staging, durable persistence, and background indexing.
- PostgreSQL vector storage and retrieval-augmented chat with streamed answers and citations.
- Realtime resource notifications and document-processing status updates.
- ASP.NET Core Identity, email verification, account management, and Google sign-in.
- Subscription plans, purchases, user subscriptions, and ZaloPay integration scaffolding.
- Subject-specific AI and document-storage configuration with application defaults.

Some areas remain in progress, including the document upload progress modal, fully state-driven document status rendering, occurrence-level citation validation, and portions of external payment handling.

## Solution Structure

```text
EduChatAI.slnx
|- Domain/             Entities, contracts, constants, DTOs, and domain exceptions
|- DataAccessLayer/    EF Core contexts, migrations, repositories, and unit of work
|- BusinessLayer/      Document, AI, account, subscription, and payment services
|- PresentationLayer/ Razor Pages, SignalR, Hangfire adapters, and static assets
|- UnitTests/          NUnit unit and service tests
|- docker-compose.yml
`- PresentationLayer/Dockerfile
```

Project dependencies flow inward through the domain contracts:

```text
PresentationLayer -> BusinessLayer -> DataAccessLayer -> Domain
PresentationLayer ------------------------------------> Domain
```

## Docker Setup

### Prerequisites

- Docker Desktop or another Docker Engine with Docker Compose.
- Credentials for the configured AI providers. These are exchanged separately and are not stored in Git.
- An NVIDIA-compatible Docker runtime only when using the optional GPU configuration.

Node.js and the .NET SDK are not required on the host for the Docker workflow. The image builds Tailwind CSS in a dedicated Node stage and copies the generated stylesheet into the published .NET image.

### 1. Configure credentials

Create a local `.env` file from the provided template:

```powershell
Copy-Item .env.example .env
```

```bash
cp .env.example .env
```

Fill in the values supplied to you. `AI__OpenRouter__ApiKey` and `AI__Gemini__ApiKey` must currently be nonempty because their clients are registered during application startup. Configure the Supabase, Google, email, and ZaloPay values for the features you intend to exercise.

ASP.NET Core environment-variable nesting uses a double underscore, so `AI__Gemini__ApiKey` maps to `AI:Gemini:ApiKey`.

### 2. Start the stack

CPU-portable setup:

```bash
docker compose up --build
```

NVIDIA GPU acceleration for Ollama:

```bash
docker compose -f docker-compose.yml -f docker-compose.gpu.yml up --build
```

The base Compose file intentionally does not require a GPU.

### 3. Pull Ollama models

The Ollama volume starts without models. Pull the local models when you intend to select the Ollama provider:

```bash
docker exec educhatai_ollama ollama pull bge-m3
docker exec educhatai_ollama ollama pull qwen3
```

OpenRouter is currently the seeded global default. Ollama models are therefore not required merely to start the application, but requests configured to use Ollama require the corresponding model to be installed.

### 4. Open the services

| URL | Service |
| --- | --- |
| `http://localhost:8080` | EduChatAI web application |
| `http://localhost:8080/hangfire` | Hangfire dashboard; authorization still applies |
| `http://localhost:5540` | RedisInsight |
| `http://localhost:11434` | Ollama API |

PostgreSQL and Redis are exposed on their standard host ports, `5432` and `6379`, for development tools.

In the Compose `Development` environment, the web application waits for healthy PostgreSQL and Redis containers, applies pending EF Core migrations, and runs idempotent seed checks during startup.

### Useful commands

```bash
docker compose ps
docker compose logs -f web
docker compose down
docker compose down -v
```

`docker compose down -v` deletes the PostgreSQL and Ollama volumes and should only be used for a full local reset.

## Visual Studio

The Docker Compose project preserves the Visual Studio debugging workflow:

- `docker-compose.vs.debug.yml` mounts Windows User Secrets and development HTTPS certificates.
- The Visual Studio override resets the CLI `.env` file, so mounted User Secrets remain authoritative for overlapping configuration keys.
- `docker-compose.gpu.yml` is included through `AdditionalComposeFilePaths`, so Visual Studio debugging continues to request GPU-backed Ollama.
- The ASP.NET `base` stage must remain the first Dockerfile stage because Visual Studio Fast mode targets it directly.
- Visual Studio explicitly enables HTTPS redirection and publishes `https://localhost:8081`.
- `DependencyAwareStart` is intentionally omitted because it tears down the Compose containers when debugging stops; the normal workflow keeps infrastructure running between debug sessions.
- Fast-mode host builds still run `npm run tailwind:build` through `Presentation.csproj`.

Visual Studio development therefore requires Node.js/npm in addition to Docker and the .NET 10 SDK. The dedicated Docker asset stage is used by regular image builds; it does not remove the host npm requirement from Visual Studio Fast mode.

Use the Docker Compose project as the startup project and configure application credentials through the Presentation project’s .NET User Secrets. The root `.env` file is reserved for CLI Compose and does not supply Visual Studio debug credentials.

## Running the Web Project on the Host

Host execution uses `ConnectionStrings:Database` from `PresentationLayer/appsettings.json`, which points to PostgreSQL on `localhost:5432`. Start the infrastructure containers first:

```bash
docker compose up -d db redis ollama
```

Install frontend packages and build the application:

```bash
npm ci --prefix PresentationLayer
dotnet restore EduChatAI.slnx
dotnet run --project PresentationLayer/Presentation.csproj
```

The default launch URLs are:

- `http://localhost:5158`
- `https://localhost:7265`

Set local credentials with User Secrets rather than editing `appsettings.json`:

```bash
dotnet user-secrets set --project PresentationLayer/Presentation.csproj "AI:OpenRouter:ApiKey" "<value>"
dotnet user-secrets set --project PresentationLayer/Presentation.csproj "AI:Gemini:ApiKey" "<value>"
dotnet user-secrets set --project PresentationLayer/Presentation.csproj "BlobStorage:Supabase:ApiSecretKey" "<value>"
```

To use a shared development database temporarily, override the same logical connection key:

```bash
dotnet user-secrets set --project PresentationLayer/Presentation.csproj "ConnectionStrings:Database" "<shared connection string>"
```

Remove that override to return to the tracked localhost database:

```bash
dotnet user-secrets remove --project PresentationLayer/Presentation.csproj "ConnectionStrings:Database"
```

Future deployments should inject the same configuration keys through the hosting environment or its secret manager.

## Optional Admin Dashboard Demo History

The historical admin-report dataset is disabled by default, runs only in Development, and refreshes only visibly prefixed demo rows on each enabled startup. Do not enable it against a shared database.

Run the app directly from PowerShell with the demo history enabled:

```powershell
$env:DemoData__AdminReports__Enabled = "true"
dotnet run --project PresentationLayer/Presentation.csproj
```

Or enable it for Docker Compose:

```powershell
$env:EDUCHATAI_DEMO_ADMIN_REPORTS_ENABLED = "true"
docker compose up --build
```

Unset the environment variable or set it to `false` to return to normal startup. Refreshing the dataset preserves non-demo users, documents, chats, experiments, and report activity.

## EF Core Migrations

With the database container running, create or apply migrations from the host:

```bash
dotnet ef migrations add <MigrationName> --project DataAccessLayer --startup-project PresentationLayer
dotnet ef database update --project DataAccessLayer --startup-project PresentationLayer
```

Migrations live in `DataAccessLayer/Migrations`. Review both `Up()` and `Down()` behavior, including PostgreSQL trigger SQL, before applying schema changes.

## Document and Chat Flow

```text
HTTP upload
  -> validate content
  -> stage through an opaque locator
  -> create Document (Received)
  -> Hangfire durable-persistence job
  -> parse readable stream
  -> chunk and embed
  -> PostgreSQL/pgvector index
  -> retrieval-augmented chat and citations
```

Documents may be subject-wide or tagged with chapters. Database constraints prevent a document from being tagged with a chapter belonging to another subject. Durable storage is resolved from the owning subject’s storage configuration, with Supabase used as the policy fallback; the actual storage method is persisted on each document.

## Realtime Endpoints

The application exposes these SignalR hubs:

| Path | Purpose |
| --- | --- |
| `/resource` | Resource-centric change notifications |
| `/documents/status` | Document indexing progress |
| `/chat/answer` | Streaming chat generation |
| `/documents/comments` | Document comment updates |

The resource notification model supports resource types, individual resources, and collections related to a principal resource. It is not page-specific.

## Tests

Run the complete NUnit suite:

```bash
dotnet test EduChatAI.slnx
```

For a no-restore verification after a successful build:

```bash
dotnet build EduChatAI.slnx --no-restore
dotnet test EduChatAI.slnx --no-build --no-restore
```

## Security Notes

- Never commit `.env`, User Secrets, API keys, database credentials, or payment signing keys.
- The database password in Compose is a disposable local-development credential, not a deployment secret.
- Replace `AppBaseUrl` and payment callback/redirect URLs with externally reachable HTTPS URLs when testing provider callbacks.
- Production hosting must supply connection strings and integration credentials through its environment or secret manager.
