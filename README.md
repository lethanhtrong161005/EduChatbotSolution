# EduChatAI - Educational Chatbot Solution

EduChatAI is an enterprise-grade, 3-tier architecture web application built with **ASP.NET Core MVC**. The project leverages **PostgreSQL** with the **pgvector** extension for semantic vector searches and integrates with the **OpenAI API** to provide an intelligent, context-aware educational chatbot experience.

---

## 🛠️ Tech Stack

* **Framework:** .NET 8.0 / .NET 10.0 (ASP.NET Core MVC)
* **Database:** PostgreSQL 18 with `pgvector` (Vector Similarity Search)
* **ORM:** Entity Framework Core (EF Core) via Npgsql
* **AI Integration:** OpenAI API (Embeddings & Chat Completions)
* **Containerization:** Docker & Docker Compose (Custom Bridge Network)

---

## 📂 Project Architecture & Structure

The solution strictly adheres to the **Separation of Concerns (SoC)** principle using a classic 3-Tier Layered Architecture:

```text
EduChatbotSolution/ (Solution Root)
│
├── 1. DataAccessLayer/ (Class Library -> .dll)
│   ├── Data/                  # ApplicationDbContext and EF Core Migrations
│   ├── Entities/              # Database models mapping directly to DB tables (e.g., Document, Subject)
│   ├── Repositories/          # Low-level database queries (CRUD, LINQ expressions)
│   │   ├── Interfaces/        # e.g., IDocumentRepository.cs
│   │   └── Implementations/   # e.g., DocumentRepository.cs
│   └── DataAccessLayer.csproj
│
├── 2. BusinessLayer/ (Class Library -> .dll)
│   ├── DTOs/                  # Data Transfer Objects (e.g., DocumentDto, UploadRequestDto)
│   ├── Services/              # Core business logic, validation, and AI orchestrations
│   │   ├── Interfaces/        # e.g., IDocumentService.cs
│   │   └── Implementations/   # e.g., DocumentService.cs
│   └── BusinessLayer.csproj
│
├── 3. PresentationLayer/ (ASP.NET Core MVC Web App)
│   ├── Controllers/           # Handles incoming HTTP Requests and delegates to BusinessLayer
│   ├── Models/                # ViewModels specifically structured for rendering UI Views
│   ├── Views/                 # .cshtml files (HTML, Razor layouts, and frontend components)
│   ├── wwwroot/               # Static assets (CSS, JS, Images, and client-side uploads)
│   ├── Program.cs             # Application bootstrap and Dependency Injection (DI) configuration
│   ├── appsettings.json       # App configurations (Connection Strings, API Keys)
│   └── PresentationLayer.csproj
│
└── EduChatbotSolution.sln     # Visual Studio / VS Code Solution File