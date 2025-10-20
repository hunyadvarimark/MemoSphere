using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Core.Interfaces.Repositories;
using Data.Context;

namespace Data.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly IDbContextFactory<MemoSphereDbContext> _factory;

        public GenericRepository(IDbContextFactory<MemoSphereDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<T> GetByIdAsync(int id)
        {
            using var context = _factory.CreateDbContext();
            return await context.Set<T>().FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            using var context = _factory.CreateDbContext();
            return await context.Set<T>().ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            using var context = _factory.CreateDbContext();
            await context.Set<T>().AddAsync(entity);
            await context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            using var context = _factory.CreateDbContext();
            await context.Set<T>().AddRangeAsync(entities);
            await context.SaveChangesAsync();
        }

        public void Remove(T entity)
        {
            using var context = _factory.CreateDbContext();
            context.Set<T>().Remove(entity);
            context.SaveChanges();
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            using var context = _factory.CreateDbContext();
            context.Set<T>().RemoveRange(entities);
            context.SaveChanges();
        }

        public async Task<IEnumerable<T>> GetFilteredAsync(Expression<Func<T, bool>> filter = null,
                                            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
                                            string includeProperties = null)
        {
            using var context = _factory.CreateDbContext();
            IQueryable<T> query = context.Set<T>();

            // 1. Szűrés (WHERE)
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // 2. Kapcsolatok Betöltése (INCLUDE)
            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }

            // 3. Rendezés (ORDER BY)
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // 4. Végrehajtás
            return await query.Distinct().AsNoTracking().ToListAsync();  // AsNoTracking hozzáadva teljesítményért
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            using var context = _factory.CreateDbContext();
            return await context.Set<T>().AnyAsync(predicate);
        }

        public void Update(T entity)
        {
            using var context = _factory.CreateDbContext();
            context.Set<T>().Attach(entity);
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }

        public async Task ReloadAsync(T entity)
        {
            using var context = _factory.CreateDbContext();
            await context.Entry(entity).ReloadAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            using var context = _factory.CreateDbContext();
            return await context.Set<T>()
                .Where(predicate)
                .ToListAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            using var context = _factory.CreateDbContext();
            return await context.Set<T>().CountAsync(predicate);
        }
    }
}