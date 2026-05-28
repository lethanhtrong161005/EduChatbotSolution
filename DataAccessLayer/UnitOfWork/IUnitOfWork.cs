using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.UnitOfWork;

public interface IUnitOfWork
{
    GenericRepository<ApplicationUser> Users { get; }
    GenericRepository<SubscriptionPlan> SubscriptionPlans { get; }
    GenericRepository<UserSubscription> UserSubscriptions { get; }
    GenericRepository<PaymentTransaction> PaymentTransactions { get; }
    GenericRepository<Subject> Subjects { get; }
    GenericRepository<Chapter> Chapters { get; }
    GenericRepository<Document> Documents { get; }
    GenericRepository<Chunk> Chunks { get; }
    GenericRepository<Conversation> Conversations { get; }
    GenericRepository<Message> Messages { get; }
    GenericRepository<Citation> Citations { get; }
    GenericRepository<TestQuestion> TestQuestions { get; }
    GenericRepository<Experiment> Experiments { get; }
    GenericRepository<TestResponse> TestResponses { get; }

    Task SaveAsync(CancellationToken cancellationToken = default);
}
