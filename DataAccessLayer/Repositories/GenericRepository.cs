using Domain.Common;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DataAccess.Repositories;

public class GenericRepository<TEntity>(DbContext context) where TEntity : class
{
    internal readonly DbContext context = context;
    internal readonly DbSet<TEntity> dbSet = context.Set<TEntity>();

    public virtual async Task<IEnumerable<TEntity>> GetAsync(
        string[] includeProperties = null!,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        (int pageSize, int pageIndex) paginationSettings = default,
        bool noTracking = false,
        bool deferLoading = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = dbSet;

        if (includeProperties != null)
        {
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }
        }

        if (filter != null)
            query = query.Where(filter);

        if (orderBy != null)
            query = orderBy(query);

        var count = await query.CountAsync(cancellationToken);

        var pageSize = paginationSettings.pageSize;
        var pageIndex = paginationSettings.pageIndex;

        if (pageSize > 0 && pageIndex > 0)
        {
            var skip = (pageIndex - 1) * pageSize;
            if (skip >= count)
            {
                var maxPage = (int)Math.Ceiling(count / (double)pageSize);
                pageIndex = Math.Max(maxPage, 1);
                skip = (pageIndex - 1) * pageSize;
            }
            query = query.Skip(skip)
                         .Take(pageSize);
        }

        if (noTracking)
        {
            query = query.AsNoTracking();
        }

        var items = await query.ToListAsync(cancellationToken);

        return new PaginatedList<TEntity>(items, count, pageSize, pageIndex);
    }

    public virtual async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return await dbSet.FindAsync([id], cancellationToken);
    }

    public virtual TEntity Insert(TEntity entityToInsert)
    {
        dbSet.Attach(entityToInsert);
        return dbSet.Add(entityToInsert).Entity;
    }

    public virtual TEntity Update(TEntity entityToUpdate)
    {
        var entry = dbSet.Attach(entityToUpdate);
        entry.State = EntityState.Modified;
        return entry.Entity;
    }

    public virtual TEntity Delete(TEntity entityToDelete)
    {
        dbSet.Attach(entityToDelete);
        return dbSet.Remove(entityToDelete).Entity;
    }

    public virtual async Task<TEntity> InsertAsync(TEntity entityToInsert, CancellationToken cancellationToken = default)
    {
        dbSet.Attach(entityToInsert);
        return (await dbSet.AddAsync(entityToInsert, cancellationToken)).Entity;
    }

    public virtual async Task<TEntity> DeleteAsync(object id, CancellationToken cancellationToken = default)
    {
        var entityToDelete = await dbSet.FindAsync([id], cancellationToken)
             ?? throw new EntityNotFoundException(id);
        return Delete(entityToDelete);
    }
}
