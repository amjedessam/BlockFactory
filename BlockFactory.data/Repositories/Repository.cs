using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BlockFactory.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        // ─── القراءة ───────────────────────────────

        public async Task<T?> GetByIdAsync(int id)
            => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync()
            => await _dbSet.ToListAsync();

        public async Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate)
            => await _dbSet.Where(predicate).ToListAsync();

        public async Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate)
            => await _dbSet.FirstOrDefaultAsync(predicate);

        public async Task<bool> AnyAsync(
            Expression<Func<T, bool>> predicate)
            => await _dbSet.AnyAsync(predicate);

        public async Task<int> CountAsync(
            Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();

            return await _dbSet.CountAsync(predicate);
        }

        public IQueryable<T> Query()
            => _dbSet.AsQueryable();

        // ─── الكتابة ───────────────────────────────

        public async Task AddAsync(T entity)
            => await _dbSet.AddAsync(entity);

        public async Task AddRangeAsync(IEnumerable<T> entities)
            => await _dbSet.AddRangeAsync(entities);

        public void Update(T entity)
            => _dbSet.Update(entity);

        public void UpdateRange(IEnumerable<T> entities)
            => _dbSet.UpdateRange(entities);

        public void Delete(T entity)
            => _dbSet.Remove(entity);

        public void DeleteRange(IEnumerable<T> entities)
            => _dbSet.RemoveRange(entities);
    }
}
