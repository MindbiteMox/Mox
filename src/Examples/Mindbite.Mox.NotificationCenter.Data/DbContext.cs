using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mindbite.Mox.NotificationCenter.Data
{
    public interface INotificationCenterDbContext : IDbContext
    {
        DbSet<Models.Notification> Notifications { get; }
        DbSet<Models.Subscription> Subscriptions { get; }
    }
}
