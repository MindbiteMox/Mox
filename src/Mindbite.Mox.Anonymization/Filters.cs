using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mindbite.Mox.Anonymization.Data;
using Mindbite.Mox.Anonymization.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Anonymization.Filters
{
    public interface IAnonymizationFilter<TDbContext> where TDbContext : DbContext
    {
        Task ExecuteAsync(AnonymizationOptions<TDbContext> options, TDbContext dbContext, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry);
    }

    public interface IImmediateAnonymizationFilter<TDbContext> : IAnonymizationFilter<TDbContext> where TDbContext : DbContext
    { }

    public interface IDeferredAnonymizationFilter<TDbContext> : IAnonymizationFilter<TDbContext> where TDbContext : DbContext
    {
        Task<IQueryable<TEntity>> GetEntriesToFilterAsync<TEntity>(TDbContext dbContext) where TEntity : class, IDeferredAnonymizeable;
    }

    public class SoftDeletedImmediateAnonymizationFilter<TDbContext> : IImmediateAnonymizationFilter<TDbContext> where TDbContext : DbContext
    {
        public Task ExecuteAsync(AnonymizationOptions<TDbContext> options, TDbContext dbContext, EntityEntry entry)
        {
            if (entry.Entity is Core.Data.Models.ISoftDeleted softDeletedEntry && softDeletedEntry.IsDeleted)
            {
                AnonymizationUtils.AnonymizePersonalDataProperties(softDeletedEntry, options.GetAnonymizedValue);

                if (entry.Entity is IDeferredAnonymizeable anonymizable)
                {
                    anonymizable.AnonymizedOn = DateTime.Now;
                }
            }

            return Task.CompletedTask;
        }
    }

    public class GenericSoftDeletedDeferredAnonymizationFilter<TDbContext> : IDeferredAnonymizationFilter<TDbContext> where TDbContext : DbContext
    {
        public TimeSpan AnonymizationDelay { get; init; }

        public GenericSoftDeletedDeferredAnonymizationFilter(TimeSpan anonymizationDelay)
        {
            this.AnonymizationDelay = anonymizationDelay;
        }

        public Task<IQueryable<TEntity>> GetEntriesToFilterAsync<TEntity>(TDbContext dbContext) where TEntity : class, IDeferredAnonymizeable
        {
            if (!typeof(TEntity).IsAssignableTo(typeof(Core.Data.Models.ISoftDeleted)))
            {
                return Task.FromResult(Enumerable.Empty<TEntity>().AsQueryable());
            }

            var deletedBefore = DateTime.Now - AnonymizationDelay;

            var set = dbContext.Set<TEntity>()
                .IgnoreQueryFilters()
                .Where(x => x.AnonymizedOn == null)
                .Cast<Core.Data.Models.ISoftDeleted>()
                .Where(x => x.IsDeleted && x.DeletedOn < deletedBefore);

            return Task.FromResult(set.Cast<TEntity>());
        }

        public Task ExecuteAsync(AnonymizationOptions<TDbContext> options, TDbContext dbContext, EntityEntry entry)
        {
            if (entry.Entity is Core.Data.Models.ISoftDeleted softDeletedEntry && softDeletedEntry.IsDeleted)
            {
                AnonymizationUtils.AnonymizePersonalDataProperties(softDeletedEntry, options.GetAnonymizedValue);
            }

            return Task.CompletedTask;
        }
    }
}
