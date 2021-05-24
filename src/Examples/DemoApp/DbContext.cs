using Mindbite.Mox.Identity.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.NotificationCenter.Data.Models;
using Mindbite.Mox.DesignDemoApp.Data.Models;
using Mindbite.Mox.DesignDemoApp.Data;
using Mindbite.Mox.NotificationCenter.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Mindbite.Mox.DemoApp
{
    public class AppDbContext : MoxIdentityDbContext, IDesignDbContext, INotificationCenterDbContext, Images.Data.IImagesDbContext
    {
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Design> Designs { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<UserImage> UserImages { get; set; }
        public DbSet<Images.Data.Models.Image> AllImages { get; set; }
        public DbSet<Images.Data.Models.File> AllFiles { get; set; }

        private readonly DesignDbContextActions _designDbContextActions;

        public AppDbContext(DbContextOptions<AppDbContext> options, DesignDbContextActions designDbContextActions, IHttpContextAccessor httpContextAccessor) : base(options, httpContextAccessor)
        {
            this._designDbContextActions = designDbContextActions;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new NotificationMapping());
            modelBuilder.ApplyConfiguration<Design>(new DesignMapping());
            modelBuilder.ApplyConfiguration<Image>(new DesignMapping());
            modelBuilder.ApplyConfiguration<UserImage>(new DesignMapping());
            modelBuilder.ApplyConfiguration(new SubscriptionMapping());
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(Mindbite.Mox.Images.Data.IImagesDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

        public override EntityEntry<TEntity> Remove<TEntity>(TEntity entity)
        {
            this._designDbContextActions.Remove(this, entity);
            return base.Remove(entity);
        }
    }
}
