using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Anonymization.Filters;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mindbite.Mox.Anonymization
{
    public class AnonymizationOptions<TDbContext> where TDbContext : DbContext
    {
        private readonly Dictionary<Type, List<IImmediateAnonymizationFilter<TDbContext>>> _immediateFilters = new();
        private readonly Dictionary<Type, List<IDeferredAnonymizationFilter<TDbContext>>> _deferredFilters = new();

        public IImmediateAnonymizationFilter<TDbContext>? ImmediateDefaultFilter { get; set; }
        public IDeferredAnonymizationFilter<TDbContext>? DeferredDefaultFilter { get; set; }
        public IReadOnlyDictionary<Type, List<IImmediateAnonymizationFilter<TDbContext>>> ImmediateFilters => this._immediateFilters;
        public IReadOnlyDictionary<Type, List<IDeferredAnonymizationFilter<TDbContext>>> DeferredFilters => this._deferredFilters;
        public Func<PropertyInfo, object?>? GetAnonymizedValue { get; set; }

        /// <summary>
        /// These filters run immediately before DbContext.SaveChanges() executes.
        /// </summary>
        /// <typeparam name="TEntity">Entity Framework Model this filter applies to</typeparam>
        /// <param name="filter">Filter instance</param>
        public void AddImmediateFilter<TEntity>(IImmediateAnonymizationFilter<TDbContext> filter)
        {
            if (_immediateFilters.TryGetValue(typeof(TEntity), out var filters))
            {
                filters.Add(filter);
            }
            else
            {
                _immediateFilters[typeof(TEntity)] = new() { filter };
            }
        }

        /// <summary>
        /// These filters run by a scheduler, once every day for example.
        /// </summary>
        /// <typeparam name="TEntity">Entity Framework Model this filter applies to</typeparam>
        /// <param name="filter">Filter instance</param>
        public void AddDeferredFilter<TEntity>(IDeferredAnonymizationFilter<TDbContext> filter)
        {
            if (_deferredFilters.TryGetValue(typeof(TEntity), out var filters))
            {
                filters.Add(filter);
            }
            else
            {
                _deferredFilters[typeof(TEntity)] = new() { filter };
            }
        }
    }
}
