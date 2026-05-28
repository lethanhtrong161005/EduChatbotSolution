using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;

namespace DataAccessLayer.UnitOfWork;

public class UnitOfWork(EduChatbotDbContext context) : IUnitOfWork, IAsyncDisposable
{
    GenericRepository<ApplicationUser>? _users;
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

    public GenericRepository<ApplicationUser> Users => _users ??= new GenericRepository<ApplicationUser>(context);
    public GenericRepository<SubscriptionPlan> SubscriptionPlans => _subscriptionPlans ??= new GenericRepository<SubscriptionPlan>(context);
    public GenericRepository<UserSubscription> UserSubscriptions => _userSubscriptions ??= new GenericRepository<UserSubscription>(context);
    public GenericRepository<PaymentTransaction> PaymentTransactions => _paymentTransactions ??= new GenericRepository<PaymentTransaction>(context);
    public GenericRepository<Subject> Subjects => _subjects ??= new GenericRepository<Subject>(context);
    public GenericRepository<Chapter> Chapters => _chapters ??= new GenericRepository<Chapter>(context);
    public GenericRepository<Document> Documents => _documents ??= new GenericRepository<Document>(context);
    public GenericRepository<Chunk> Chunks => _chunks ??= new GenericRepository<Chunk>(context);
    public GenericRepository<Conversation> Conversations => _conversations ??= new GenericRepository<Conversation>(context);
    public GenericRepository<Message> Messages => _messages ??= new GenericRepository<Message>(context);
    public GenericRepository<Citation> Citations => _citations ??= new GenericRepository<Citation>(context);
    public GenericRepository<TestQuestion> TestQuestions => _testQuestions ??= new GenericRepository<TestQuestion>(context);
    public GenericRepository<Experiment> Experiments => _experiments ??= new GenericRepository<Experiment>(context);
    public GenericRepository<TestResponse> TestResponses => _testResponses ??= new GenericRepository<TestResponse>(context);

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    private bool _disposed = false;

    protected virtual async Task DisposeAsync(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            await context.DisposeAsync();
        }

        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }
}
