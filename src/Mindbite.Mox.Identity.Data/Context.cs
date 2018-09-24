using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.AspNetCore.Identity;
using Mindbite.Mox.Services;

namespace Mindbite.Mox.Identity.Data
{
    public class MoxIdentityDbContext : IdentityDbContext<Models.MoxUser>, IDbContext
    {
        public DbSet<Models.PasswordReset> PasswordReset { get; set; }

        public DbSet<Models.MoxUserBaseImpl> MoxUserImpl { get; set; }

        public MoxIdentityDbContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Mox");

            modelBuilder.Entity<Models.MoxUser>().Property(x => x.Name).HasDefaultValue("Namn");
            modelBuilder.Entity<Models.PasswordReset>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);

            modelBuilder.Entity<Models.MoxUser>().HasQueryFilter(x => !x.IsDeleted);

            base.OnModelCreating(modelBuilder);
        }
    }
}
