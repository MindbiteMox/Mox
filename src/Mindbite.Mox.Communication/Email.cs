using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Mindbite.Mox.Communication
{
    public class EmailOptions
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string DefaultFrom { get; set; }
        public string DefaultFromName { get; set; }

        public EmailOptions()
        {
            this.SmtpPort = 0;
            this.EnableSsl = false;
            this.SmtpUsername = "";
            this.SmtpPassword = "";
        }
    }

    public class EmailSender
    {
        private readonly EmailOptions _emailOptions;
        private readonly SmtpClient _smtpClient;

        public EmailSender(IOptions<EmailOptions> emailOptions)
        {
            this._emailOptions = emailOptions.Value;

            if (this._emailOptions == null)
                throw new Exception("EmailSender requested but EmailOptions is not configured.");

            this._smtpClient = new SmtpClient(this._emailOptions.SmtpServer);
            if (this._emailOptions.SmtpPort > 0) { this._smtpClient.Port = this._emailOptions.SmtpPort; }
            if (this._emailOptions.EnableSsl)
            {
                this._smtpClient.EnableSsl = true;
                this._smtpClient.Credentials = new NetworkCredential(this._emailOptions.SmtpUsername, this._emailOptions.SmtpPassword);
            }
        }

        public async Task SendAsync(MailMessage mailMessage)
        {
            mailMessage.From = mailMessage.From ?? new MailAddress(this._emailOptions.DefaultFrom, this._emailOptions.DefaultFromName);

            await this._smtpClient.SendMailAsync(mailMessage);
        }
    }
}
