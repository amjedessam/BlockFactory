using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Linq.Expressions;

namespace BlockFactory.Core.Interfaces
{
    public interface IRepository<T> where T : class
    {
        // ─── القراءة ───────────────────────────────
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate);

        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate);

        Task<bool> AnyAsync(
            Expression<Func<T, bool>> predicate);

        Task<int> CountAsync(
            Expression<Func<T, bool>>? predicate = null);

        // ─── الكتابة ───────────────────────────────
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void Delete(T entity);
        void DeleteRange(IEnumerable<T> entities);

        // ─── IQueryable للاستعلامات المعقدة ─────────
        IQueryable<T> Query();
    }
}
