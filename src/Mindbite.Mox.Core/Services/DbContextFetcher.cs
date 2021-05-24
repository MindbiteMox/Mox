using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mindbite.Mox.Services
{
    public interface IDbContextFetcher
    {
        T FetchDbContext<T>() where T : Core.Data.IDbContext;
    }

    public class DbContextFetcher<T> : IDbContextFetcher where T : Core.Data.IDbContext
    {
        private Core.Data.IDbContext _context;
        public DbContextFetcher(T context)
        {
            this._context = context;
        }

        G IDbContextFetcher.FetchDbContext<G>()
        {
            return (G)this._context;
        }
    }
}
