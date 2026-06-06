# EduChatAI - Educational Chatbot Solution

EduChatAI is an enterprise-grade, **4-tier architecture** web application built with **ASP.NET Core MVC**. It uses **PostgreSQL** with the **pgvector** extension for semantic vector search, **Ollama** for local embeddings & chat generation, **Redis** for caching, and **Hangfire** for background document-indexing jobs. The result is an intelligent, context-aware educational chatbot with a built-in document library, subscription plans, and online payments.

---

## 🛠️ Tech Stack

| Concern              | Technology |
|----------------------|------------|
| **Framework**        | .NET 10.0 (ASP.NET Core MVC) |
| **Database**         | PostgreSQL 18 + `pgvector` (1024-dim vector similarity search) |
| **ORM**              | Entity Framework Core via Npgsql (snake_case naming) |
| **Authentication**   | ASP.NET Core Identity + Cookie + Google OAuth, email confirmation required |
| **AI / Embeddings**  | Ollama (`bge-m3` embeddings, `qwen3` chat) via `OllamaSharp` |
| **Caching**          | Redis (StackExchange.Redis) + RedisInsight UI |
| **Background Jobs**  | Hangfire (PostgreSQL storage) — async document parse → chunk → embed pipeline |
| **Payments**         | ZaloPay (sandbox) |
| **Email**            | SMTP (Gmail) for verification codes & password resets |
| **Mapping**          | AutoMapper |
| **Testing**          | xUnit (`UnitTests` project) |
| **Containerization** | Docker & Docker Compose (custom bridge network) |

---

## 📂 Project Architecture & Structure

The solution follows **Separation of Concerns (SoC)** using a **4-Tier Layered Architecture** with a shared **Domain** kernel. Note that the projects' assembly/file names are short (`Domain`, `DataAccess`, `Business`, `Presentation`), while the folders keep the descriptive `*Layer` names.

```text
EduChatbotSolution/ (Solution Root)
│
├── Domain/ (Domain.csproj -> .dll | No dependencies — shared kernel)
│   ├── Common/                  # AppConstants, Enums, PaginatedList<T>
│   ├── Contracts/               # Service interfaces (IAuthService, IDocumentService, IEmbeddingService, IPaymentService, ...)
│   ├── DTOs/                    # Cross-layer data objects (ChunkDto, ParsedDocument, EmbeddingResult)
│   ├── Entities/                # EF Core entities (ApplicationUser, Subject, Chapter, Document, Chunk,
│   │                            #   ChatSession, ChatMessage, Citation, Plan, Subscription, Order, Payment, ...)
│   ├── Exceptions/              # Domain exceptions (BadRequest, EntityNotFound, EntityConstraint, UserClaim)
│   └── Utils/                   # DateTimeHelper, FileHelper
│
├── DataAccessLayer/ (DataAccess.csproj -> .dll | Depends on: Domain)
│   ├── Data/                    # EduChatbotDbContext + EF Core configuration & seeding
│   ├── Migrations/              # EF Core migrations (InitialCreate -> ...)
│   ├── Repositories/            # GenericRepository<T> (generic CRUD)
│   └── UnitOfWork/              # IUnitOfWork / UnitOfWork coordinating repositories
│
├── BusinessLayer/ (Business.csproj -> .dll | Depends on: Domain, DataAccess)
│   ├── Background/              # DocumentIndexer (Hangfire job orchestrating the indexing pipeline)
│   ├── Parsing/                 # SimpleParser (IDocumentParser)
│   ├── Chunking/                # FixedLengthChunker (IDocumentChunker, 1000 chars / 200 overlap)
│   ├── Embedding/               # OllamaEmbeddingService + OllamaOptions (IEmbeddingService)
│   ├── ExternalPayment/         # ZaloPayService + PaymentProviderOptions
│   ├── Services/                # Core business logic (Auth, Subject, Chapter, Document, Subscription,
│   │                            #   Order, Payment, Email, EmailVerification, UserManagement)
│   ├── Templates/               # HTML email templates (verification code, password reset, ...)
│   └── Utils/                   # SubscriptionHelper
│
├── PresentationLayer/ (Presentation.csproj -> ASP.NET Core MVC | Depends on: Domain, Business)
│   ├── Controllers/             # Account, Home, Admin, Documents, Subscriptions, Payment, Error
│   ├── Models/                  # ViewModels (Vm) for the Razor views
│   ├── Views/                   # .cshtml (Account, Admin, Documents, Home, Payment, Subscriptions, Shared)
│   ├── Mappings/                # AutoMapper profiles (Document, Payment, Subscription)
│   ├── Middleware/              # CustomExceptionMiddleware
│   ├── Filters/                 # HangfireAuthFilter (dashboard authorization)
│   ├── Extensions/              # HostExtensions (MigrateDb / SeedDb), ClaimExtensions
│   ├── Routing/                 # SlugifyParameterTransformer, KebabCaseQueryParameterRule
│   ├── Settings/                # AuthenticationSettings, ErrorHandlingDefaults
│   ├── Common/                  # NavBar helper
│   ├── wwwroot/                 # Static assets (Bootstrap, jQuery, Font Awesome, CSS, JS, images)
│   ├── Program.cs               # Bootstrap, DI, auth, Hangfire & HTTP pipeline
│   ├── Dockerfile               # Multi-stage build for the web image
│   └── appsettings.json         # Connection strings, API keys, provider config
│
├── UnitTests/ (UnitTests.csproj — xUnit | SubjectService, Subscription, UserManagement tests)
│
├── docker-compose.yml           # Orchestration: db, redis, redis-ui, web, ollama
├── docker-compose.override.yml  # Local dev overrides (ports, user secrets, https certs)
└── EduChatbotSolution.slnx      # Solution file
```

---

## 🔗 Dependency Graph

```text
┌──────────────────────────────────────────────────────────┐
│                    PresentationLayer                       │
│              (ASP.NET Core MVC Web App)                    │
└──────────┬───────────────────────────────┬────────────────┘
           │                               │
           ▼                               ▼
┌────────────────────┐         ┌──────────────────────────┐
│   BusinessLayer    │         │                          │
│   (Services/BLL)   │         │                          │
└────────┬───────────┘         │                          │
         │                     │        Domain            │
         ▼                     │   (Shared Kernel DLL)    │
┌────────────────────┐         │  Entities, Contracts,    │
│  DataAccessLayer   │────────▶│  DTOs, Exceptions,       │
│  (Repositories/DAL)│         │  Common, Utils           │
└────────────────────┘         └──────────────────────────┘
```

> **Domain** is the innermost layer with **zero dependencies**. All other layers reference it for shared entities, service contracts, DTOs, exceptions, and helpers.

---

## 🐳 Running with Docker (recommended)

The entire stack runs through Docker Compose. The `web` service is built from [PresentationLayer/Dockerfile](PresentationLayer/Dockerfile); the database image already bundles `pgvector`.

### Services & ports

| Service     | Container             | Image                                   | Host port(s)        | Purpose |
|-------------|-----------------------|-----------------------------------------|---------------------|---------|
| `web`       | `educhatbot_mvc`      | built from `PresentationLayer/Dockerfile` | `8080`, `8081`      | ASP.NET Core MVC app |
| `db`        | `postgres`            | `pgvector/pgvector:0.8.2-pg18-trixie`   | `65432` → 5432      | PostgreSQL + pgvector |
| `redis`     | `educhatbot_redis`    | `redis:7-alpine`                        | `6379`              | Cache |
| `redis-ui`  | `educhatbot_redis_ui` | `redis/redisinsight:latest`             | `5540`              | Redis GUI |
| `ollama`    | `educhatbot_ollama`   | `ollama/ollama`                         | `11434`             | Local embeddings & chat (uses GPU: `gpus: all`) |

### 1. Configure secrets

Set the values described in **[Configuration & Environment Variables](#-configuration--environment-variables)** before the first run. At minimum you should provide the **Google OAuth** credentials and the **Gmail App Password**; the `OpenAI:ApiKey` and ZaloPay sandbox keys can stay as-is for local testing.

### 2. Build & start the stack

```bash
docker compose up -d --build
```

### 3. Pull the Ollama models (first run only)

The `ollama` container starts empty, so pull the two models the app expects:

```bash
docker exec -it educhatbot_ollama ollama pull bge-m3   # embeddings (1024-dim)
docker exec -it educhatbot_ollama ollama pull qwen3     # chat completion
```

> No NVIDIA GPU? Remove the `gpus: all` line from the `ollama` service in [docker-compose.yml](docker-compose.yml) to run on CPU.

### 4. Database migrations

In the **Development** environment the app **auto-applies EF Core migrations and seeds data on startup** (see `MigrateDb` / `SeedDbAsync` in [PresentationLayer/Program.cs](PresentationLayer/Program.cs)). No manual migration step is needed.

### 5. Open the app

| URL                              | Description |
|----------------------------------|-------------|
| `http://localhost:8080`          | App (HTTP)  |
| `https://localhost:8081`         | App (HTTPS) |
| `http://localhost:8080/hangfire` | Hangfire dashboard (indexing jobs) |
| `http://localhost:5540`          | RedisInsight UI |

### Useful commands

```bash
docker compose logs -f web      # tail the web app logs
docker compose ps               # service status
docker compose down             # stop & remove containers (keeps volumes)
docker compose down -v          # also remove db / ollama volumes (full reset)
```

---

## ⚙️ Configuration & Environment Variables

All settings live in [PresentationLayer/appsettings.json](PresentationLayer/appsettings.json). Several values are intentionally left **blank** and must be filled in before those features work. You can either edit the JSON file directly, or override any key via an environment variable using the `__` (double underscore) separator — the `docker-compose.yml` `web` service already does this for the connection string, OpenAI key, and Redis.

### 🔑 Values you must replace

#### 1. Google OAuth — `Authentication:Google`

```json
"Authentication": {
  "Google": {
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

These power **"Sign in with Google"**. The app **throws on startup if they are empty** (see [Program.cs](PresentationLayer/Program.cs#L92-L95)).

**How to obtain them:**
1. Go to the [Google Cloud Console](https://console.cloud.google.com/) → **APIs & Services → Credentials**.
2. **Create Credentials → OAuth client ID → Web application**.
3. Add an **Authorized redirect URI**: `https://localhost:8081/signin-google` (and any other base URL you deploy to + `/signin-google`).
4. Copy the generated **Client ID** into `ClientId` and **Client secret** into `ClientSecret`.

```json
"ClientId":     "1234567890-abcdefg.apps.googleusercontent.com",
"ClientSecret": "GOCSPX-xxxxxxxxxxxxxxxxxxxx"
```

#### 2. Gmail App Password — `Email:AppPassword`

```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SenderEmail": "your-account@gmail.com",
  "SenderName": "EduChatAI",
  "AppPassword": ""
}
```

Used to send **email-verification codes** and **password-reset codes**. This is **not** your normal Gmail password — it is a 16-character **App Password**.

**How to obtain it:**
1. Enable **2-Step Verification** on the Google account in `SenderEmail`.
2. Go to [Google Account → Security → App passwords](https://myaccount.google.com/apppasswords).
3. Generate a new app password (e.g. app name "EduChatAI").
4. Paste the 16-character value (spaces optional) into `AppPassword`, and make sure `SenderEmail` matches that account.

```json
"AppPassword": "abcd efgh ijkl mnop"
```

#### 3. OpenAI API Key — `OpenAI:ApiKey` *(optional for local dev)*

```json
"OpenAI": { "ApiKey": "sk-proj-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" }
```

Replace the placeholder with a real key from the [OpenAI dashboard](https://platform.openai.com/api-keys) if/when you use OpenAI. For everyday local development the embeddings & chat run on **Ollama**, so this can remain a placeholder.

### Other settings (sensible defaults provided)

| Section                         | Key                              | Default / Notes |
|---------------------------------|----------------------------------|-----------------|
| `ConnectionStrings`             | `DefaultConnection`              | Points at the `db` service (`Host=db`). Overridden in compose. |
| `Redis`                         | `ConnectionString`              | `localhost:6379` locally; `redis:6379` inside compose. |
| `Ollama`                        | `Endpoint` / `EmbeddingModel` / `ChatModel` | `http://ollama:11434`, `bge-m3`, `qwen3`. |
| `PaymentProviders:ZaloPay`      | `AppId` / `Key1` / `Key2` / `*Url` | ZaloPay **sandbox** keys. Update `CallbackUrl` / `RedirectUrlBase` to your public (e.g. ngrok) URL when testing real callbacks. |
| `AppBaseUrl`                    | —                                | Base URL used to build links in emails. |

> ⚠️ **Security:** `appsettings.json` currently contains sample/sandbox secrets. For anything beyond local development, move secrets to **environment variables**, **.NET User Secrets**, or a secrets manager — and never commit real production credentials.

#### Overriding via environment variables

Any key maps to an env var by replacing the `:` hierarchy with `__`. For example, to set the Google and email secrets for the `web` container, add to its `environment:` block in [docker-compose.yml](docker-compose.yml):

```yaml
environment:
  - Authentication__Google__ClientId=your-client-id
  - Authentication__Google__ClientSecret=your-client-secret
  - Email__AppPassword=your-app-password
```

---

## 🧪 Running tests

```bash
dotnet test UnitTests/UnitTests.csproj
```

---

## 💻 Running locally without Docker (optional)

You still need PostgreSQL (with `pgvector`), Redis, and Ollama reachable. Adjust `ConnectionStrings:DefaultConnection` (`Host=localhost`, `Port=65432` if using the compose DB), `Redis:ConnectionString`, and `Ollama:Endpoint` accordingly, then:

```bash
dotnet restore
dotnet run --project PresentationLayer/Presentation.csproj
```
