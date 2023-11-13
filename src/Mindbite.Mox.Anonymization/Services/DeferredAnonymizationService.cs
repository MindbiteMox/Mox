using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Anonymization.Data;
using Mindbite.Mox.Anonymization.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Anonymization.Services
{
    public interface IDeferredAnonymizationService
    {
        Task AnonymizeDeferredAsync();
    }

    public class DeferredAnonymizationService<TDbContext> : IDeferredAnonymizationService where TDbContext : DbContext
    {
        private readonly TDbContext _context;
        private readonly AnonymizationOptions<TDbContext> _options;

        public DeferredAnonymizationService(TDbContext dbContext, IOptions<AnonymizationOptions<TDbContext>> options)
        {
            this._context = dbContext;
            this._options = options.Value;
        }

        private async Task ExecuteFilterAsync<TEntity>(IDeferredAnonymizationFilter<TDbContext> filter) where TEntity : class, IDeferredAnonymizeable
        {
            var entities = await filter.GetEntriesToFilterAsync<TEntity>(this._context);

            foreach (var entity in entities)
            {
                entity.AnonymizedOn = DateTime.Now;
                await filter.ExecuteAsync(this._options, this._context, this._context.Entry(entity));
            }

            await this._context.SaveChangesAsync();
        }

        public async Task AnonymizeDeferredAsync()
        {
            var executeFilterMethod = this.GetType().GetMethod(nameof(ExecuteFilterAsync), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            foreach (var type in this._context.Model.GetEntityTypes().Where(x => x.ClrType.IsAssignableTo(typeof(IDeferredAnonymizeable))))
            {
                var genericExecuteFilterMethod = executeFilterMethod!.MakeGenericMethod(type.ClrType);

                if (this._options.DeferredFilters.TryGetValue(type.ClrType, out var filters))
                {
                    foreach (var filter in filters)
                    {
                        await (Task)genericExecuteFilterMethod.Invoke(this, new object[] { filter })!;
                    }
                }
                else if (this._options.DeferredDefaultFilter != null)
                {
                    await (Task)genericExecuteFilterMethod.Invoke(this, new object[] { this._options.DeferredDefaultFilter })!;
                }
            }
        }
    }
}
