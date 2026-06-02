using DataAccess.Data;
using DataAccess.Repositories;
using Domain.Entities;

namespace DataAccess.UnitOfWork;

/// <summary>
/// Implements the Unit of Work pattern, providing lazy-loaded repositories
/// for all aggregate roots and coordinating saves via a shared DbContext.
/// </summary>
public class UnitOfWork(EduChatbotDbContext context) : IUnitOfWork
{
    GenericRepository<Plan>? _plans;
    GenericRepository<PlanOption>? _planOptions;
    GenericRepository<Order>? _orders;
    GenericRepository<Subscription>? _subscriptions;
    GenericRepository<Payment>? _payments;
    GenericRepository<Subject>? _subjects;
    GenericRepository<Chapter>? _chapters;
    GenericRepository<Document>? _documents;
    GenericRepository<Chunk>? _chunks;
    GenericRepository<Conversation>? _conversations;
    GenericRepository<Message>? _messages;
    GenericRepository<Citation>? _citations;
    GenericRepository<TestQuestion>? _testQuestions;
    GenericRepository<Experiment>? _experiments;
    GenericRepository<TestResponse>? _testResponses;

    /// <inheritdoc/>
    public GenericRepository<Plan> Plans => _plans ??= new GenericRepository<Plan>(context);
    /// <inheritdoc/>
    public GenericRepository<PlanOption> PlanOptions => _planOptions ??= new GenericRepository<PlanOption>(context);
    /// <inheritdoc/>
    public GenericRepository<Order> Orders => _orders ??= new GenericRepository<Order>(context);
    /// <inheritdoc/>
    public GenericRepository<Subscription> Subscriptions => _subscriptions ??= new GenericRepository<Subscription>(context);
    /// <inheritdoc/>
    public GenericRepository<Payment> Payments => _payments ??= new GenericRepository<Payment>(context);
    /// <inheritdoc/>
    public GenericRepository<Subject> Subjects => _subjects ??= new GenericRepository<Subject>(context);
    /// <inheritdoc/>
    public GenericRepository<Chapter> Chapters => _chapters ??= new GenericRepository<Chapter>(context);
    /// <inheritdoc/>
    public GenericRepository<Document> Documents => _documents ??= new GenericRepository<Document>(context);
    /// <inheritdoc/>
    public GenericRepository<Chunk> Chunks => _chunks ??= new GenericRepository<Chunk>(context);
    /// <inheritdoc/>
    public GenericRepository<Conversation> Conversations => _conversations ??= new GenericRepository<Conversation>(context);
    /// <inheritdoc/>
    public GenericRepository<Message> Messages => _messages ??= new GenericRepository<Message>(context);
    /// <inheritdoc/>
    public GenericRepository<Citation> Citations => _citations ??= new GenericRepository<Citation>(context);
    /// <inheritdoc/>
    public GenericRepository<TestQuestion> TestQuestions => _testQuestions ??= new GenericRepository<TestQuestion>(context);
    /// <inheritdoc/>
    public GenericRepository<Experiment> Experiments => _experiments ??= new GenericRepository<Experiment>(context);
    /// <inheritdoc/>
    public GenericRepository<TestResponse> TestResponses => _testResponses ??= new GenericRepository<TestResponse>(context);

    /// <inheritdoc/>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            context.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the DbContext resources asynchronously.</summary>
    /// <param name="disposing">Indicates whether managed resources should be released.</param>
    protected virtual async Task DisposeAsync(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            await context.DisposeAsync();
        }
        _disposed = true;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }
}
