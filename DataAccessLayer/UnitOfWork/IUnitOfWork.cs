namespace DataAccessLayer.UnitOfWork;

public interface IUnitOfWork
{
    GenericRepository<SystemAccount> AccountRepository { get; }
    GenericRepository<NewsArticle> NewsArticleRepository { get; }
    GenericRepository<Category> CategoryRepository { get; }
    GenericRepository<Tag> TagRepository { get; }

    Task SaveAsync(CancellationToken cancellationToken = default);
}
