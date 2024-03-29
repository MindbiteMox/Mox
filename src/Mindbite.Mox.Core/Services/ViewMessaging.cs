﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindbite.Mox.Services
{
    public class ViewMessaging
    {
        public class Message
        {
            public string Title { get; set; }
            public IEnumerable<string> AdditionalLines { get; set; }
            public string CssClass { get; set; }

            public string Serialize()
            {
                return System.Text.Json.JsonSerializer.Serialize(this);
            }

            public static Message Deserialize(string serializedMessage)
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<Message>(serializedMessage);
                }
                catch
                {
                    return new Message();
                }
            }
        }

        private readonly IStringLocalizer _localizer;
        private readonly ISession _session;
        private Message CurrentMessage { get; set; }

        public bool AnyMessages => this.CurrentMessage != null;

        public ViewMessaging(IStringLocalizer localizer, IHttpContextAccessor httpContextAccessor)
        {
            this._localizer = localizer;
            this._session = httpContextAccessor.HttpContext.Session;

            var serializedMessage = this._session.GetString("Mox.Message");
            if (!string.IsNullOrWhiteSpace(serializedMessage))
            {
                this.CurrentMessage = Message.Deserialize(serializedMessage);
            }
        }

        public Message PopMessage()
        {
            if (CurrentMessage == null)
                throw new Exception($"There are no messages check {AnyMessages} before trying to pop a message!");
            var message = this.CurrentMessage;

            this.CurrentMessage = null;
            this._session.Remove("Mox.Message");

            return message;
        }

        public void DisplayMessage(string title, params string[] additionalLines)
        {
            this.CurrentMessage = new Message
            {
                Title = this._localizer[title],
                AdditionalLines = additionalLines?.Select(x => this._localizer[x].ToString()) ?? Enumerable.Empty<string>()
            };

            this._session.SetString("Mox.Message", this.CurrentMessage.Serialize());
        }

        public void DisplayError(string title, params string[] additionalLines)
        {
            this.CurrentMessage = new Message
            {
                Title = this._localizer[title],
                AdditionalLines = additionalLines?.Select(x => this._localizer[x].ToString()) ?? Enumerable.Empty<string>(),
                CssClass = "mox-message-error"
            };

            this._session.SetString("Mox.Message", this.CurrentMessage.Serialize());
        }
    }
}
