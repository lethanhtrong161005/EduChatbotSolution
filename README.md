# EduChatAI - Educational Chatbot Solution

EduChatAI is an enterprise-grade, **4-tier architecture** web application built with **ASP.NET Core MVC**. The project leverages **PostgreSQL** with the **pgvector** extension for semantic vector searches and integrates with the **OpenAI API** to provide an intelligent, context-aware educational chatbot experience.

---

## 🛠️ Tech Stack

* **Framework:** .NET 10.0 (ASP.NET Core MVC).
* **Database:** PostgreSQL 18 with `pgvector` (Vector Similarity Search)
* **ORM:** Entity Framework Core (EF Core) via Npgsql
* **Authentication:** Custom BCrypt + Cookie Authentication (no ASP.NET Identity)
* **AI Integration:** OpenAI API (Embeddings & Chat Completions)
* **Containerization:** Docker & Docker Compose (Custom Bridge Network)

---

## 📂 Project Architecture & Structure

The solution strictly adheres to the **Separation of Concerns (SoC)** principle using a **4-Tier Layered Architecture** with a dedicated **Domain** layer as the shared kernel:

```text
EduChatbotSolution/ (Solution Root)
│
├── 1. Domain/ (Class Library -> .dll | No dependencies)
│   ├── Common/                  # Shared generic types (e.g., PaginatedList<T>)
│   ├── Constants/               # Application-wide constants (role names, TempData keys, messages)
│   │   └── AppConstants.cs
│   ├── DTOs/                    # Data Transfer Objects shared across all layers
│   │   ├── LoginRequestDto.cs
│   │   └── RegisterRequestDto.cs
│   ├── Exceptions/              # Domain exception types used across all layers
│   │   ├── BadRequestException.cs
│   │   ├── EntityConstraintException.cs
│   │   ├── EntityNotFoundException.cs
│   │   └── UserClaimException.cs
│   └── Domain.csproj
│
├── 2. DataAccessLayer/ (Class Library -> .dll | Depends on: Domain)
│   ├── Data/                    # EduChatbotDbContext and EF Core configuration
│   ├── Entities/                # Database models mapping directly to DB tables
│   │   ├── ApplicationUser.cs, Role.cs, UserRole.cs
│   │   ├── Subject.cs, Chapter.cs, Document.cs, Chunk.cs
│   │   ├── Conversation.cs, Message.cs, Citation.cs
│   │   ├── SubscriptionPlan.cs, UserSubscription.cs, PaymentTransaction.cs
│   │   ├── TestQuestion.cs, Experiment.cs, TestResponse.cs
│   │   └── BaseEntity.cs
│   ├── Migrations/              # EF Core database migrations
│   ├── Repositories/            # Generic repository pattern for CRUD operations
│   │   └── GenericRepository.cs
│   ├── UnitOfWork/              # Unit of Work pattern coordinating repositories
│   │   ├── IUnitOfWork.cs
│   │   └── UnitOfWork.cs
│   └── DataAccessLayer.csproj
│
├── 3. BusinessLayer/ (Class Library -> .dll | Depends on: Domain, DataAccessLayer)
│   ├── Services/                # Core business logic, validation, and AI orchestrations
│   │   ├── Interfaces/          # Service contracts (e.g., IAuthService.cs)
│   │   └── Implementations/     # Service implementations (e.g., AuthService.cs)
│   └── BusinessLayer.csproj
│
├── 4. PresentationLayer/ (ASP.NET Core MVC Web App | Depends on: Domain, BusinessLayer)
│   ├── Controllers/             # Handles HTTP requests and delegates to BusinessLayer
│   │   ├── AccountController.cs
│   │   └── HomeController.cs
│   ├── Defaults/                # Default configuration values (e.g., AuthenticationDefaults.cs)
│   ├── Extensions/              # Application startup extensions (e.g., HostExtensions.cs)
│   ├── Middleware/              # Custom middleware (e.g., CustomExceptionMiddleware.cs)
│   ├── Models/                  # ViewModels for rendering UI Views (e.g., ErrorViewModel.cs)
│   ├── Options/                 # Configuration option classes (e.g., ErrorHandlingOptions.cs)
│   ├── Routing/                 # Custom routing (e.g., SlugifyParameterTransformer.cs)
│   ├── Views/                   # .cshtml files (Razor layouts and frontend components)
│   ├── wwwroot/                 # Static assets (CSS, JS, Images)
│   ├── Program.cs               # Application bootstrap and DI configuration
│   ├── appsettings.json         # App configurations (Connection Strings, API Keys)
│   └── PresentationLayer.csproj
│
├── docker-compose.yml           # Docker Compose orchestration
├── docker-compose.override.yml  # Environment-specific overrides
└── EduChatbotSolution.slnx      # Solution File
```

---

## 🔗 Dependency Graph

```text
┌──────────────────────────────────────────────────────────┐
│                    PresentationLayer                     │
│              (ASP.NET Core MVC Web App)                  │
└──────────┬──────────────────────────────┬────────────────┘
           │                              │
           ▼                              ▼
┌────────────────────┐         ┌──────────────────────────┐
│   BusinessLayer    │         │                          │
│   (Services/BLL)   │         │                          │
└────────┬───────────┘         │                          │
         │                     │        Domain            │
         ▼                     │   (Shared Kernel DLL)    │
┌────────────────────┐         │   DTOs, Exceptions,      │
│  DataAccessLayer   │────────▶│   Common, Constants      │
│  (Repositories/DAL)│         │                          │
└────────────────────┘         └──────────────────────────┘
```

> **Domain** is the innermost layer with **zero dependencies**. All other layers reference it for shared DTOs, exceptions, constants, and common types.