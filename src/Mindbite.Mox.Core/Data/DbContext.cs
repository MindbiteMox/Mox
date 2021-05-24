using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Mindbite.Mox.Core.Data
{
    public interface IDbContext
    {
        DatabaseFacade Database { get; }
        ChangeTracker ChangeTracker { get; }
        IModel Model { get; }
        EntityEntry Add(object entity);
        EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
        ValueTask<EntityEntry> AddAsync(object entity, CancellationToken cancellationToken = default(CancellationToken));
        ValueTask<EntityEntry<TEntity>> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default(CancellationToken)) where TEntity : class;
        void AddRange(IEnumerable<object> entities);
        void AddRange(params object[] entities);
        Task AddRangeAsync(params object[] entities);
        Task AddRangeAsync(IEnumerable<object> entities, CancellationToken cancellationToken = default(CancellationToken));
        EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;
        EntityEntry Attach(object entity);
        void AttachRange(IEnumerable<object> entities);
        void AttachRange(params object[] entities);
        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
        EntityEntry Entry(object entity);
        object Find(Type entityType, params object[] keyValues);
        TEntity Find<TEntity>(params object[] keyValues) where TEntity : class;
        ValueTask<object> FindAsync(Type entityType, object[] keyValues, CancellationToken cancellationToken);
        ValueTask<TEntity> FindAsync<TEntity>(object[] keyValues, CancellationToken cancellationToken) where TEntity : class;
        ValueTask<TEntity> FindAsync<TEntity>(params object[] keyValues) where TEntity : class;
        ValueTask<object> FindAsync(Type entityType, params object[] keyValues);
        EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;
        EntityEntry Remove(object entity);
        void RemoveRange(IEnumerable<object> entities);
        void RemoveRange(params object[] entities);
        int SaveChanges();
        int SaveChanges(bool acceptAllChangesOnSuccess);
        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken));
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
        EntityEntry Update(object entity);
        void UpdateRange(params object[] entities);
        void UpdateRange(IEnumerable<object> entities);
    }

    public static class Extensions
    {
        /// <summary>
        /// Applies HasQueryFilter(x => !x.IsDeleted) to every registered entity type implementing ISoftDeleted.
        /// </summary>
        /// <param name="modelBuilder"></param>
        /// <param name="predicate"></param>
        public static void AddIsDeletedQueryFilter(this ModelBuilder modelBuilder, Predicate<IMutableEntityType> predicate = null)
        {
            predicate ??= _ => true;

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(Models.ISoftDeleted).IsAssignableFrom(entityType.ClrType) && entityType.ClrType.BaseType == typeof(object) && predicate(entityType))
                {
                    var IsDeleted = entityType.FindProperty("IsDeleted");
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "x");
                    var parameterExpression = System.Linq.Expressions.Expression.Property(parameter, IsDeleted.PropertyInfo);
                    var notExpression = System.Linq.Expressions.Expression.Not(parameterExpression);
                    var filter = System.Linq.Expressions.Expression.Lambda(notExpression, parameter);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }
        }
    }
}
