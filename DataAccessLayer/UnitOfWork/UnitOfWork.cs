using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;

namespace DataAccessLayer.UnitOfWork;

/// <summary>
/// Implements the Unit of Work pattern, providing lazy-loaded repositories
/// for all aggregate roots and coordinating saves via a shared DbContext.
/// </summary>
public class UnitOfWork(EduChatbotDbContext context) : IUnitOfWork, IAsyncDisposable
{
    GenericRepository<ApplicationUser>? _users;
    GenericRepository<Role>? _roles;
    GenericRepository<SubscriptionPlan>? _subscriptionPlans;
    GenericRepository<UserSubscription>? _userSubscriptions;
    GenericRepository<PaymentTransaction>? _paymentTransactions;
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
    public GenericRepository<ApplicationUser> Users => _users ??= new GenericRepository<ApplicationUser>(context);
    /// <inheritdoc/>
    public GenericRepository<Role> Roles => _roles ??= new GenericRepository<Role>(context);
    /// <inheritdoc/>
    public GenericRepository<SubscriptionPlan> SubscriptionPlans => _subscriptionPlans ??= new GenericRepository<SubscriptionPlan>(context);
    /// <inheritdoc/>
    public GenericRepository<UserSubscription> UserSubscriptions => _userSubscriptions ??= new GenericRepository<UserSubscription>(context);
    /// <inheritdoc/>
    public GenericRepository<PaymentTransaction> PaymentTransactions => _paymentTransactions ??= new GenericRepository<PaymentTransaction>(context);
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
