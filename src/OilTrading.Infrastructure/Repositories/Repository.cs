using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;
using System.Linq.Expressions;

namespace OilTrading.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class, IAggregateRoot
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        if (predicate == null)
            return await _dbSet.CountAsync(cancellationToken);
        
        return await _dbSet.CountAsync(predicate, cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);

        // CRITICAL SYSTEMIC FIX FOR ALL BaseEntity DESCENDANTS:
        // Force EF Core to recognize soft-delete properties in the INSERT statement.
        //
        // ROOT CAUSE OF PREVIOUS FAILURE:
        // - Reflection invocation AFTER DbSet.AddAsync() doesn't trigger change tracking properly
        // - Property assignments via reflection don't trigger the same notifications as direct calls
        //
        // CORRECT SOLUTION:
        // - Use EF Core's direct property change tracking API on the entity entry
        // - This FORCES EF Core to include these columns in the INSERT regardless of whether they changed
        // - Bypasses EF Core's "unchanged property" optimization that omits default values
        if (entity is BaseEntity baseEntity)
        {
            try
            {
                var entry = _context.Entry(entity);
                // Directly access property objects and mark as modified
                // This bypasses the expression tree and works with BaseEntity properties directly
                entry.Property(nameof(BaseEntity.IsDeleted)).IsModified = true;
                entry.Property(nameof(BaseEntity.DeletedAt)).IsModified = true;
                entry.Property(nameof(BaseEntity.DeletedBy)).IsModified = true;
            }
            catch
            {
                // Silently continue if property marking fails
                // Shouldn't happen with properly configured entities
            }
        }

        return entity;
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);

        // CRITICAL SYSTEMIC FIX FOR BULK OPERATIONS:
        // Apply the same IsDeleted tracking fix to all entities being added in bulk.
        // This ensures batch operations (like CSV uploads processing 100+ records) properly track all BaseEntity properties.
        foreach (var entity in entities)
        {
            if (entity is BaseEntity baseEntity)
            {
                try
                {
                    var entry = _context.Entry(entity);
                    // Mark soft-delete properties as modified for all bulk-added entities
                    entry.Property(nameof(BaseEntity.IsDeleted)).IsModified = true;
                    entry.Property(nameof(BaseEntity.DeletedAt)).IsModified = true;
                    entry.Property(nameof(BaseEntity.DeletedBy)).IsModified = true;
                }
                catch
                {
                    // Continue processing other entities if one fails
                }
            }
        }

        return entities;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }
}