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
using System.Threading.Tasks;
using System.Threading;

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

        private readonly HashSet<Core.Data.Models.ISoftDeleted> _ignoreWhenSavingChanges = new();

        public MoxIdentityDbContext(DbContextOptions options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            this._httpContextAccessor = httpContextAccessor;

            this.SavingChanges += OnSavingChanges;
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

        protected virtual void OnSavingChanges(object sender, SavingChangesEventArgs args)
        {
            this.UpdateSoftDeletePropertiesFromToEntityState();
        }

        protected virtual void UpdateSoftDeletePropertiesFromToEntityState()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is Core.Data.Models.ISoftDeleted deletableEntity)
                {
                    if(this._ignoreWhenSavingChanges.Remove(deletableEntity))
                    {
                        continue;
                    }

                    if (entry.State == EntityState.Deleted)
                    {
                        entry.State = EntityState.Modified;
                        deletableEntity.IsDeleted = true;
                        deletableEntity.DeletedOn = DateTime.Now;
                        deletableEntity.DeletedById = this._httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                    }
                    else if (entry.State == EntityState.Added)
                    {
                        deletableEntity.CreatedOn = DateTime.Now;
                        deletableEntity.CreatedById = this._httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                        deletableEntity.ModifiedOn = DateTime.Now;
                        deletableEntity.ModifiedById = this._httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        deletableEntity.ModifiedOn = DateTime.Now;
                        deletableEntity.ModifiedById = this._httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                    }
                }
            }
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

        public override EntityEntry<TEntity> Remove<TEntity>(TEntity entity)
        {
            if(entity is Core.Data.Models.ISoftDeleted deletableEntity)
            {
                this.DeleteWithoutTracking(deletableEntity);
                return base.Update(entity);
            }
            else
            {
                return base.Remove(entity);
            }
        }

        public void DeleteWithoutTracking<TEntity>(TEntity entity) where TEntity : Core.Data.Models.ISoftDeleted
        {
            entity.IsDeleted = true;
            entity.DeletedOn = DateTime.Now;
            entity.DeletedById = this._httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public EntityEntry<TEntity> UpdateWithoutSettingsModifiedProperties<TEntity>(TEntity entity) where TEntity : class, Core.Data.Models.ISoftDeleted
        {
            this._ignoreWhenSavingChanges.Add(entity);
            return base.Update(entity);
        }
    }
}
