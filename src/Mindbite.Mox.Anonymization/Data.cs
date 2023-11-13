using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mindbite.Mox.Anonymization.Data
{
    public class AnonymizationSaveChangesInterceptor<TDbContext> : SaveChangesInterceptor where TDbContext : DbContext
    {
        private AnonymizationOptions<TDbContext> _options;

        public AnonymizationSaveChangesInterceptor(AnonymizationOptions<TDbContext> options)
        {
            this._options = options;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged && x.State != EntityState.Detached))
            {
                if (this._options.ImmediateFilters.TryGetValue(entry.Entity.GetType(), out var policies))
                {
                    foreach (var policy in policies)
                    {
                        await policy.ExecuteAsync(this._options, (TDbContext)eventData.Context, entry);
                    }
                }
                else if (this._options.ImmediateDefaultFilter != null)
                {
                    await this._options.ImmediateDefaultFilter.ExecuteAsync(this._options, (TDbContext)eventData.Context, entry);
                }
            }

            return result;
        }
    }

    public interface IDeferredAnonymizeable
    {
        DateTime? AnonymizedOn { get; set; }
    }
}
