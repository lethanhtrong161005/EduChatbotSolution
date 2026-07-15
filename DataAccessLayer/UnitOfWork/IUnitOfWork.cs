using DataAccess.Repositories;
using Domain.Entities;

namespace DataAccess.UnitOfWork;

/// <summary>
/// Defines the Unit of Work contract, exposing lazy-loaded repositories
/// for all aggregate roots and coordinating database saves.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    // ── User ───────────────────────────────
    /// <summary>Gets the repository for <see cref="ApplicationUser"/> entities.</summary>
    GenericRepository<ApplicationUser> Users { get; }

    /// <summary>Gets the repository for <see cref="ApplicationRole"/> entities.</summary>
    GenericRepository<ApplicationRole> Roles { get; }

    /// <summary>Gets the repository for <see cref="ApplicationUserRole"/> entities.</summary>
    GenericRepository<ApplicationUserRole> UserRoles { get; }

    GenericRepository<UserImportBatch> UserImportBatches { get; }

    GenericRepository<UserImportRow> UserImportRows { get; }

    // ── Subscription & Payment ───────────────────────────────

    /// <summary>Gets the repository for <see cref="Plan"/> entities.</summary>
    GenericRepository<Plan> Plans { get; }

    /// <summary>Gets the repository for <see cref="PlanOption"/> entities.</summary>
    GenericRepository<PlanOption> PlanOptions { get; }

    /// <summary>Gets the repository for <see cref="Order"/> entities.</summary>
    GenericRepository<Order> Orders { get; }

    /// <summary>Gets the repository for <see cref="Subscription"/> entities.</summary>
    GenericRepository<Subscription> Subscriptions { get; }

    /// <summary>Gets the repository for <see cref="Payment"/> entities.</summary>
    GenericRepository<Payment> Payments { get; }

    // ── Subjects & Documents ─────────────────────────────────
    /// <summary>Gets the repository for <see cref="Subject"/> entities.</summary>
    GenericRepository<Subject> Subjects { get; }

    /// <summary>Gets the repository for <see cref="Membership"/> entities.</summary>
    GenericRepository<Membership> Memberships { get; }

    /// <summary>Gets the repository for <see cref="SubjectStorageConfiguration"/> entities.</summary>
    GenericRepository<SubjectStorageConfiguration> SubjectStorageConfigurations { get; }

    /// <summary>Gets the repository for <see cref="SubjectAiConfiguration"/> entities.</summary>
    GenericRepository<SubjectAiConfiguration> SubjectAiConfigurations { get; }

    /// <summary>Gets the repository for <see cref="GlobalAiConfiguration"/> entities.</summary>
    GenericRepository<GlobalAiConfiguration> GlobalAiConfigurations { get; }

    SubjectIndexRepository SubjectIndexes { get; }

    /// <summary>Gets the repository for <see cref="Chapter"/> entities.</summary>
    GenericRepository<Chapter> Chapters { get; }

    /// <summary>Gets the repository for <see cref="Document"/> entities.</summary>
    GenericRepository<Document> Documents { get; }

    /// <summary>Gets the repository for <see cref="DocumentChapter"/> entities.</summary>
    GenericRepository<DocumentChapter> DocumentChapters { get; }

    /// <summary>Gets the repository for <see cref="DocumentComment"/> entities.</summary>
    GenericRepository<DocumentComment> DocumentComments { get; }

    /// <summary>Gets the repository for <see cref="ParsedSection"/> entities.</summary>
    GenericRepository<ParsedSection> ParsedSections { get; }

    /// <summary>Gets the repository for <see cref="Chunk"/> entities.</summary>
    GenericRepository<Chunk> Chunks { get; }

    // ── Conversations ────────────────────────────────────────
    /// <summary>Gets the repository for <see cref="ChatSession"/> entities.</summary>
    GenericRepository<ChatSession> ChatSessions { get; }

    /// <summary>Gets the repository for <see cref="Domain.Entities.ChatSessionTitleGenerationSettings"/> entities.</summary>
    GenericRepository<ChatSessionTitleGenerationSettings> ChatSessionTitleGenerationSettings { get; }

    /// <summary>Gets the repository for <see cref="Domain.Entities.ChatSessionTitleGenerationMetrics"/> entities.</summary>
    GenericRepository<ChatSessionTitleGenerationMetrics> ChatSessionTitleGenerationMetrics { get; }

    /// <summary>Gets the repository for <see cref="ChatMessage"/> entities.</summary>
    GenericRepository<ChatMessage> ChatMessages { get; }

    ChatTurnRepository ChatTurns { get; }

    GenericRepository<ChatMessageGenerationSettings> ChatMessageGenerationSettings { get; }

    GenericRepository<ChatMessageGenerationMetrics> ChatMessageGenerationMetrics { get; }

    /// <summary>Gets the repository for <see cref="Citation"/> entities.</summary>
    GenericRepository<Citation> Citations { get; }

    GenericRepository<CitationOccurrence> CitationOccurrences { get; }

    AdminReportRepository AdminReports { get; }

    // ── Research & Evaluation ────────────────────────────────
    /// <summary>Gets the repository for <see cref="TestQuestion"/> entities.</summary>
    GenericRepository<TestQuestion> TestQuestions { get; }

    /// <summary>Gets the repository for <see cref="Experiment"/> entities.</summary>
    GenericRepository<Experiment> Experiments { get; }

    GenericRepository<ExperimentConfigurationSnapshot> ExperimentConfigurationSnapshots { get; }

    ExperimentRunRepository ExperimentRuns { get; }

    /// <summary>Gets the repository for <see cref="TestResponse"/> entities.</summary>
    GenericRepository<TestResponse> TestResponses { get; }

    GenericRepository<TestResponseContext> TestResponseContexts { get; }

    /// <summary>Persists all pending changes to the database.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
