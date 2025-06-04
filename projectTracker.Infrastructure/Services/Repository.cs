using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using projectTracker.Application.Interfaces;
using ProjectTracker.Infrastructure.Data;

namespace projectTracker.Infrastructure.Services
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly AppDbContext _context;

        public Repository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<T> GetByIdAsync(string id)
            => await _context.Set<T>().FindAsync(id);

        public async Task<IReadOnlyList<T>> GetAllAsync()
            => await _context.Set<T>().ToListAsync();

        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(T entity)
        {
            _context.Set<T>().Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ExistsAsync(string id)
            => await _context.Set<T>().FindAsync(id) != null;

        public async Task<T> FindAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>> includes = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _context.Set<T>();
            if (includes != null) query = includes(query);
            return await query.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task<IReadOnlyList<T>> GetWhereAsync(Expression<Func<T, bool>> predicate)
            => await _context.Set<T>().Where(predicate).ToListAsync();

        async Task IRepository<T>.SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new Exception("Concurrency conflict occurred", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Database update failed", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error saving changes", ex);
            }
        }
    }
}
