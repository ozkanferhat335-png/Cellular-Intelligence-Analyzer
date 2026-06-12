using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CIA.Data.Context;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace CIA.Data.Repositories
{
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly CiaDbContext Context;
        protected readonly DbSet<T> DbSet;
        protected readonly ILogger Logger;

        protected BaseRepository(CiaDbContext context)
        {
            Context = context;
            DbSet = context.Set<T>();
            Logger = LogManager.GetLogger(GetType().FullName);
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await DbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await DbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await DbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await DbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
        {
            if (predicate == null)
                return await DbSet.CountAsync();
            return await DbSet.CountAsync(predicate);
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await DbSet.AnyAsync(predicate);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await DbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await DbSet.AddRangeAsync(entities);
        }

        public virtual Task UpdateAsync(T entity)
        {
            DbSet.Update(entity);
            return Task.CompletedTask;
        }

        public virtual Task DeleteAsync(T entity)
        {
            DbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public virtual Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            DbSet.RemoveRange(entities);
            return Task.CompletedTask;
        }

        public virtual async Task<int> SaveChangesAsync()
        {
            return await Context.SaveChangesAsync();
        }
    }
}
