using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.Notifications;
using Mindbite.Mox.NotificationCenter.Data.Models;
using Mindbite.Mox.Communication;
using System.Net.Mail;
using System.Linq;

namespace Mindbite.Mox.NotificationCenter.Communication
{
    public interface IMessenger
    {
        string Identitifyer { get; }
        Task Send(Notification notification, MoxUser reciever);
    }

    public class EmailMessenger : IMessenger
    {
        public string Identitifyer => nameof(EmailMessenger);
        private readonly EmailSender _emailSender;
        public EmailMessenger(EmailSender emailSender)
        {
            this._emailSender = emailSender;
        }

        public async Task Send(Notification notification, MoxUser reciever)
        {
            var mailMessage = new MailMessage
            {
                Subject = notification.SubjectId,
                Body = $"{notification.ShortDescription}, {notification.URL}"
            };
            mailMessage.To.Add(reciever.Email);
            await this._emailSender.SendAsync(mailMessage);
        }
    }

    public class PalomaTextMessenger : IMessenger
    {
        public string Identitifyer => nameof(PalomaTextMessenger);
        public async Task Send(Notification notification, MoxUser reciever)
        {
            //throw new Exception("(#/)asjdalwjdlaiwjo");
        }
    }

    public interface IMessengerProvider
    {
        IEnumerable<IMessenger> Messengers { get; }
    }

    public class MessengerProvider : IMessengerProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<Type> _messengers;
        public IEnumerable<IMessenger> Messengers => this._messengers.Select(x => (IMessenger)this._serviceProvider.GetService(x));

        public MessengerProvider(IServiceProvider serviceProvider, Action<List<Type>> messengerOptions)
        {
            this._serviceProvider = serviceProvider;
            this._messengers = new List<Type>();
            messengerOptions(this._messengers);
        }
    }
}