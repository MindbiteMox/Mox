using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Mindbite.Mox.UI
{
    public class MessageRenderer
    {
        private readonly MoxHtmlExtensionCollection _htmlExtensions;

        public MessageRenderer(MoxHtmlExtensionCollection htmlExtensions)
        {
            this._htmlExtensions = htmlExtensions;
        }

        public IHtmlContent Render()
        {
            var sb = new StringBuilder();
            var viewMessaging = this._htmlExtensions.HtmlHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<Services.ViewMessaging>();

            if (viewMessaging.AnyMessages)
            {
                var message = viewMessaging.PopMessage();

                sb.AppendLine($"<div class=\"mox-message {HttpUtility.HtmlEncode(message.CssClass)}\">");
                sb.AppendLine("<p>");
                sb.AppendLine($"<strong>{HttpUtility.HtmlEncode(message.Title)}</strong>");
                if (message.AdditionalLines.Any())
                {
                    sb.AppendLine("<br/>");
                    foreach (var line in message.AdditionalLines)
                    {
                        sb.AppendLine($"{HttpUtility.HtmlEncode(line)}<br/>");
                    }
                }
                sb.AppendLine("</p>");
                sb.AppendLine("</div>");
            }

            return new HtmlString(sb.ToString());
        }
    }
}
