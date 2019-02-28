using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.NotificationCenter.Communication;
using Mindbite.Mox.NotificationCenter.Data;
using Mindbite.Mox.Notifications;
using Mindbite.Mox.Services;
using Mindbite.Mox.NotificationCenter.Data.Models;
using Mindbite.Mox.Communication;
using System.Net.Mail;

namespace Mindbite.Mox.NotificationCenter
{
    public class NotificationCenterSender : INotificationSender
    {
        private readonly IMessengerProvider _messengerProvider;
        private readonly INotificationSubscriber _subscriber;
        private readonly INotificationCenterDbContext _context;
        private readonly EmailSender _emailSender;
        public NotificationCenterSender(IMessengerProvider messengerProvider, INotificationSubscriber subscriber, IDbContextFetcher dbContextFetcher, EmailSender emailSender)
        {
            this._messengerProvider = messengerProvider;
            this._subscriber = subscriber;
            this._context = dbContextFetcher.FetchDbContext<INotificationCenterDbContext>();
            this._emailSender = emailSender;
        }

        public async Task SendAsync(MoxUser me, string subject, string shortDescription, string url, int? entityId = null)
        {
            var subscribers = this._subscriber.Subscriptions(subject, entityId, me);

            foreach(var subscription in await subscribers.Include(x => x.Subscriber).ToListAsync())
            {
                var notification = new Notification(subject, entityId, subscription.SubscriberId, shortDescription, url);
                this._context.Add(notification);
                await Task.WhenAll(this._messengerProvider.Messengers.Select(x => x.Send(notification, subscription.Subscriber)));
            }
            await this._context.SaveChangesAsync();
        }

        public Task SendToEveryoneAsync(MoxUser me, string subject, string shortDescription, string url)
        {
            throw new NotImplementedException();
        }

        public Task SendToUserAsync(MoxUser me, MoxUser reciever, string subject, string shortDescription, string url)
        {
            throw new NotImplementedException();
        }
    }

    public class NotificationCenterReciever : INotificationReciever
    {
        private readonly INotificationCenterDbContext _context;
        public NotificationCenterReciever(IDbContextFetcher dbContextFetcher)
        {
            this._context = dbContextFetcher.FetchDbContext<INotificationCenterDbContext>();
        }

        public IQueryable<Notification> FetchHistory(MoxUser me)
        {
            return this._context.Notifications.Where(x => x.RecieverId == me.Id).OrderByDescending(x => x.SentTime);
        }

        public IQueryable<Notification> FetchUnread(MoxUser me)
        {
            return this._context.Notifications.Where(x => x.RecieverId == me.Id && !x.IsRead).OrderByDescending(x => x.SentTime);
        }

        public async Task MarkAllReadAsync(MoxUser me)
        {
            var notifications = this._context.Notifications.Where(x => x.RecieverId == me.Id && !x.IsRead);
            foreach(var notification in notifications)
            {
                notification.IsRead = true;
            }
            await this._context.SaveChangesAsync();
        }
    }

    public class NotificationCenterSubscriber : INotificationSubscriber
    {
        private readonly INotificationCenterDbContext _context;
        public NotificationCenterSubscriber(IDbContextFetcher dbContextFetcher)
        {
            this._context = dbContextFetcher.FetchDbContext<INotificationCenterDbContext>();
        }

        public async Task SubscribeToAsync(MoxUser me, string subjectId, int? entityId = null, MoxUser userToWatch = null)
        {
            if(await this._context.Subscriptions.AnyAsync(x => x.SubscriberId == me.Id && x.SubjectId == subjectId))
            {
                return;
            }

            var sub = new Data.Models.Subscription()
            {
                SubscriberId = me.Id,
                SubjectId = subjectId,
                EntityId = entityId,
                SenderId = userToWatch?.Id
            };
            this._context.Add(sub);
            await this._context.SaveChangesAsync();
        }

        public IQueryable<Subscription> Subscriptions(string subjectId, int? entityId = null, MoxUser sender = null)
        {
            var s = this._context.Subscriptions.Where(x => x.SubjectId == subjectId);

            if(entityId.HasValue)
            {
                s = s.Where(x => x.EntityId == null || x.EntityId == entityId);
            }
            else
            {
                s = s.Where(x => x.EntityId == null);
            }

            if(sender != null)
            {
                s = s.Where(x => x.SenderId == null || x.SenderId == sender.Id);
            }
            else
            {
                s = s.Where(x => x.SenderId == null);
            }

            return s;
        }

        public IQueryable<Subscription> Subscriptions(MoxUser me)
        {
            return this._context.Subscriptions.Where(x => x.SubscriberId == me.Id);
        }

        public Task UnsubscribeFromAsync(MoxUser me, string subjectId, int? entityId = null, MoxUser user = null)
        {
            throw new NotImplementedException();
        }
    }
}
