using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.NotificationCenter.Data.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Notifications
{
    public interface INotificationSender
    {
        Task SendAsync(MoxUser me, string subject, string shortDescription, string url, int? entityId = null);
        Task SendToUserAsync(MoxUser me, MoxUser reciever, string subject, string shortDescription, string url);
        Task SendToEveryoneAsync(MoxUser me, string subject, string shortDescription, string url);
    }

    public interface INotificationReciever
    {
        IQueryable<Notification> FetchUnread(MoxUser me);
        IQueryable<Notification> FetchHistory(MoxUser me);
        Task MarkAllReadAsync(MoxUser me);
    }

    public interface INotificationSubscriber
    {
        Task SubscribeToAsync(MoxUser me, string subject, int? entityId = null, MoxUser sender = null);
        Task UnsubscribeFromAsync(MoxUser me, string subject, int? entityId = null, MoxUser sender = null);
        IQueryable<Subscription> Subscriptions(string subject, int? entityId = null, MoxUser sender = null);
        IQueryable<Subscription> Subscriptions(MoxUser me);
    }

    public interface INotificationBackgroundWorker
    {
        Task DoWork();
    }
}
