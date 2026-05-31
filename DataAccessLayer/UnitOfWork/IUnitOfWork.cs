using DataAccess.Repositories;
using Domain.Entities;

namespace DataAccess.UnitOfWork;

/// <summary>
/// Defines the Unit of Work contract, exposing lazy-loaded repositories
/// for all aggregate roots and coordinating database saves.
/// </summary>
public interface IUnitOfWork
{
    // ── Subscription & Payment ───────────────────────────────
    /// <summary>Gets the repository for <see cref="SubscriptionPlan"/> entities.</summary>
    GenericRepository<SubscriptionPlan> SubscriptionPlans { get; }

    /// <summary>Gets the repository for <see cref="UserSubscription"/> entities.</summary>
    GenericRepository<UserSubscription> UserSubscriptions { get; }

    /// <summary>Gets the repository for <see cref="PaymentTransaction"/> entities.</summary>
    GenericRepository<PaymentTransaction> PaymentTransactions { get; }

    // ── Subjects & Documents ─────────────────────────────────
    /// <summary>Gets the repository for <see cref="Subject"/> entities.</summary>
    GenericRepository<Subject> Subjects { get; }

    /// <summary>Gets the repository for <see cref="Chapter"/> entities.</summary>
    GenericRepository<Chapter> Chapters { get; }

    /// <summary>Gets the repository for <see cref="Document"/> entities.</summary>
    GenericRepository<Document> Documents { get; }

    /// <summary>Gets the repository for <see cref="Chunk"/> entities.</summary>
    GenericRepository<Chunk> Chunks { get; }

    // ── Conversations ────────────────────────────────────────
    /// <summary>Gets the repository for <see cref="Conversation"/> entities.</summary>
    GenericRepository<Conversation> Conversations { get; }

    /// <summary>Gets the repository for <see cref="Message"/> entities.</summary>
    GenericRepository<Message> Messages { get; }

    /// <summary>Gets the repository for <see cref="Citation"/> entities.</summary>
    GenericRepository<Citation> Citations { get; }

    // ── Research & Evaluation ────────────────────────────────
    /// <summary>Gets the repository for <see cref="TestQuestion"/> entities.</summary>
    GenericRepository<TestQuestion> TestQuestions { get; }

    /// <summary>Gets the repository for <see cref="Experiment"/> entities.</summary>
    GenericRepository<Experiment> Experiments { get; }

    /// <summary>Gets the repository for <see cref="TestResponse"/> entities.</summary>
    GenericRepository<TestResponse> TestResponses { get; }

    /// <summary>Persists all pending changes to the database.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
