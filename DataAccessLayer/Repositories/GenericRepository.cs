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
        bool asNoTracking = false,
        bool asSplitQuery = false,
        bool deferLoading = false,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            includeProperties,
            preFilter: filter,
            projectionClass: null,
            postFilter: null,
            orderBy,
            paginationSettings,
            asNoTracking,
            asSplitQuery,
            deferLoading,
            cancellationToken);
    }

    public virtual async Task<IEnumerable<TResult>> GetAsync<TResult>(
        string[] includeProperties = null!,
        Expression<Func<TEntity, bool>>? preFilter = null,
        Expression<Func<TEntity, TResult>>? projection = null,
        Expression<Func<TResult, bool>>? postFilter = null,
        Func<IQueryable<TResult>, IOrderedQueryable<TResult>>? orderBy = null,
        (int pageSize, int pageIndex) paginationSettings = default,
        bool asNoTracking = false,
        bool deferLoading = false,
        CancellationToken cancellationToken = default)
    {
        var query = await PrepareQuery(asNoTracking, includeProperties, preFilter, projection, postFilter, orderBy);
        return await ExecuteQuery(query, paginationSettings.pageSize, paginationSettings.pageIndex, deferLoading, cancellationToken);
    }

    public virtual async Task<IEnumerable<TResult>> GetAsync<TResult>(
        string[] includeProperties = null!,
        Expression<Func<TEntity, bool>>? preFilter = null,
        Expression<Func<TEntity, TResult>>? projectionClass = null,
        Expression<Func<TResult, bool>>? postFilter = null,
        Func<IQueryable<TResult>, IOrderedQueryable<TResult>>? orderBy = null,
        (int pageSize, int pageIndex) paginationSettings = default,
        bool asNoTracking = false,
        bool asSplitQuery = false,
        bool deferLoading = false,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        var query = await PrepareQuery(asNoTracking, includeProperties, preFilter, projectionClass, postFilter, orderBy);
        if (asSplitQuery) query = query.AsSplitQuery();
        return await ExecuteQuery(query, paginationSettings.pageSize, paginationSettings.pageIndex, deferLoading, cancellationToken);
    }

    private async Task<IQueryable<TResult>> PrepareQuery<TResult>(
        bool asNoTracking = false,
        string[] includeProperties = null!,
        Expression<Func<TEntity, bool>>? preFilter = null,
        Expression<Func<TEntity, TResult>>? projection = null,
        Expression<Func<TResult, bool>>? postFilter = null,
        Func<IQueryable<TResult>, IOrderedQueryable<TResult>>? orderBy = null)
    {
        IQueryable<TEntity> tables = dbSet;

        if (asNoTracking)
            tables = tables.AsNoTracking();

        if (includeProperties != null)
        {
            foreach (var includeProperty in includeProperties)
            {
                tables = tables.Include(includeProperty);
            }
        }

        if (preFilter != null)
            tables = tables.Where(preFilter);

        IQueryable<TResult> query;

        if (projection != null)
        {
            query = tables.Select(projection);
        }
        else
        {
            if (typeof(TResult) != typeof(TEntity))
            {
                throw new InvalidOperationException("Projection required if TResult differs from TEntity.");
            }

            query = (IQueryable<TResult>)tables;
        }

        if (postFilter != null)
            query = query.Where(postFilter);

        if (orderBy != null)
            query = orderBy(query);

        return query;
    }

    private static async Task<IEnumerable<TResult>> ExecuteQuery<TResult>(
        IQueryable<TResult> query,
        int pageSize, int pageIndex,
        bool deferLoading,
        CancellationToken cxlTkn)
    {
        if (pageSize > 0 && pageIndex > 0)
        {
            var count = await query.CountAsync(cxlTkn);

            var skip = (pageIndex - 1) * pageSize;
            if (skip >= count)
            {
                var maxPage = (int)Math.Ceiling(count / (double)pageSize);
                pageIndex = Math.Max(maxPage, 1);
                skip = (pageIndex - 1) * pageSize;
            }
            query = query.Skip(skip)
                         .Take(pageSize);

            if (deferLoading)
                return new PaginatedEnumerable<TResult>(query.AsEnumerable(), count, pageSize, pageIndex);
            else
                return new PaginatedEnumerable<TResult>(await query.ToListAsync(cxlTkn), count, pageSize, pageIndex);
        }

        if (deferLoading)
            return query.AsEnumerable();
        else
            return await query.ToListAsync(cxlTkn);
    }

    public virtual async Task<TEntity?> FindByIdAsync(object id, CancellationToken cancellationToken = default)
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

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        return filter != null
            ? await dbSet.AnyAsync(filter, cancellationToken)
            : await dbSet.AnyAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        return filter != null
            ? await dbSet.CountAsync(filter, cancellationToken)
            : await dbSet.CountAsync(cancellationToken);
    }
}
