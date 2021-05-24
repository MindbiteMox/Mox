using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.AspNetCore.Identity;
using Mindbite.Mox.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Mindbite.Mox.Identity.Data
{
    public class MoxIdentityDbContext : IdentityDbContext<Models.MoxUser>, Core.Data.IDbContext
    {
        public DbSet<Models.PasswordReset> PasswordReset { get; set; }
        public DbSet<Models.MagicLinkToken> MagicLinkTokens { get; set; }

        public DbSet<Models.MoxUserBaseImpl> MoxUserImpl { get; set; }

        public DbSet<Models.RoleGroup> RoleGroups { get; set; }
        public DbSet<Models.RoleGroupRole> RoleGroupRoles { get; set; }

        public bool IncludeDeletedUsers { get; set; } = false;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public MoxIdentityDbContext(DbContextOptions options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            this._httpContextAccessor = httpContextAccessor;
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

        public void AddWithoutTracking<TEntity>(TEntity entity) where TEntity : Core.Data.Models.ISoftDeleted
        {
            entity.CreatedOn = DateTime.Now;
            entity.ModifiedOn = DateTime.Now;
            entity.CreatedById = this._httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            entity.ModifiedById = this._httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public void AddRangeWithoutTracking<TEntity>(IEnumerable<TEntity> entities) where TEntity : Core.Data.Models.ISoftDeleted
        {
            foreach (var entity in entities)
            {
                this.AddWithoutTracking(entity);
            }
        }

        public void UpdateRangeWithoutTracking<TEntity>(IEnumerable<TEntity> entities) where TEntity : Core.Data.Models.ISoftDeleted
        {
            foreach (var entity in entities)
            {
                this.UpdateWithoutTracking(entity);
            }
        }

        public void UpdateWithoutTracking<TEntity>(TEntity entity) where TEntity : Core.Data.Models.ISoftDeleted
        {
            entity.ModifiedOn = DateTime.Now;
            entity.ModifiedById = this._httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public void DeleteWithoutTracking<TEntity>(TEntity entity) where TEntity : Core.Data.Models.ISoftDeleted
        {
            entity.IsDeleted = true;
            entity.DeletedOn = DateTime.Now;
            entity.DeletedById = this._httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public override EntityEntry<TEntity> Remove<TEntity>(TEntity entity)
        {
            switch (entity)
            {
                case Core.Data.Models.ISoftDeleted deleted:
                    this.DeleteWithoutTracking(deleted);
                    return base.Update((TEntity)deleted);
                default:
                    return base.Remove(entity);
            }
        }

        public override EntityEntry<TEntity> Add<TEntity>(TEntity entity)
        {
            switch (entity)
            {
                case Core.Data.Models.ISoftDeleted added:
                    this.AddWithoutTracking(added);
                    return base.Add((TEntity)added);
                default:
                    return base.Add(entity);
            }
        }

        public override EntityEntry<TEntity> Update<TEntity>(TEntity entity)
        {
            var result = default(EntityEntry<TEntity>);

            switch (entity)
            {
                case Core.Data.Models.ISoftDeleted updated:
                    this.UpdateWithoutTracking(updated);
                    result = base.Update((TEntity)updated);
                    break;
                default:
                    result = base.Update(entity);
                    break;
            }

            return result;
        }

        public EntityEntry<TEntity> UpdateWithoutSettingsModifiedProperties<TEntity>(TEntity entity) where TEntity : class
        {
            return base.Update(entity);
        }
    }
}
