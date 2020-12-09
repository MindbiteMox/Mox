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
        public DbSet<Models.MagicLinkToken> MagicLinkTokens { get; set; }

        public DbSet<Models.MoxUserBaseImpl> MoxUserImpl { get; set; }

        public DbSet<Models.RoleGroup> RoleGroups { get; set; }
        public DbSet<Models.RoleGroupRole> RoleGroupRoles { get; set; }

        public bool IncludeDeletedUsers { get; set; } = false;

        public MoxIdentityDbContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Mox");

            modelBuilder.Entity<Models.MoxUser>().Property(x => x.Name).HasDefaultValue("Namn");
            modelBuilder.Entity<Models.PasswordReset>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            modelBuilder.Entity<Models.MagicLinkToken>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);

            modelBuilder.Entity<Models.RoleGroup>().HasMany(x => x.Roles).WithOne(x => x.RoleGroup).HasForeignKey(x => x.RoleGroupId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
            modelBuilder.Entity<Models.RoleGroup>().HasMany(x => x.Users).WithOne(x => x.RoleGroup).HasForeignKey(x => x.RoleGroupId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

            modelBuilder.Entity<Models.MoxUser>().HasQueryFilter(x => this.IncludeDeletedUsers || !x.IsDeleted);
            modelBuilder.Entity<Models.RoleGroup>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Models.RoleGroupRole>().HasQueryFilter(x => !x.IsDeleted);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentityRole>().Property(x => x.Name).HasMaxLength(450);
            modelBuilder.Entity<IdentityRole>().Property(x => x.NormalizedName).HasMaxLength(450);
        }
    }
}
