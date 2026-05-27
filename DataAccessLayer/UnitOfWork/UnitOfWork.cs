namespace DataAccessLayer.UnitOfWork;

public class UnitOfWork(NewsManagementDbContext context) : IUnitOfWork, IAsyncDisposable
{
    private GenericRepository<SystemAccount>? _accountRepository;
    private GenericRepository<NewsArticle>? _newsArticleRepository;
    private GenericRepository<Category>? _categoryRepository;
    private GenericRepository<Tag>? _tagRepository;

    public GenericRepository<SystemAccount> AccountRepository => _accountRepository ??= new GenericRepository<SystemAccount>(context);
    public GenericRepository<NewsArticle> NewsArticleRepository => _newsArticleRepository ??= new GenericRepository<NewsArticle>(context);
    public GenericRepository<Category> CategoryRepository => _categoryRepository ??= new GenericRepository<Category>(context);
    public GenericRepository<Tag> TagRepository => _tagRepository ??= new GenericRepository<Tag>(context);

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
