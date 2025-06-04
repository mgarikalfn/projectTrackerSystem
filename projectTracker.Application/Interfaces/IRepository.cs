using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(string id);
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(T entity);
        Task<bool> ExistsAsync(string id);
        Task<T> FindAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>> includes = null,  // Added optional param
            CancellationToken cancellationToken = default);       // Added cancellation token
        Task<IReadOnlyList<T>> GetWhereAsync(Expression<Func<T, bool>> predicate);
        Task SaveChangesAsync();
    }
}
