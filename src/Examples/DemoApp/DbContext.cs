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

namespace Mindbite.Mox.DemoApp
{
    public class AppDbContext : MoxIdentityDbContext, IDesignDbContext, INotificationCenterDbContext
    {
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Design> Designs { get; set; }
        public DbSet<Image> Images { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new NotificationMapping());
            modelBuilder.ApplyConfiguration<Design>(new DesignMapping());
            modelBuilder.ApplyConfiguration<Image>(new DesignMapping());
            modelBuilder.ApplyConfiguration(new SubscriptionMapping());

            base.OnModelCreating(modelBuilder);
        }
    }
}
