using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mindbite.Mox.NotificationCenter.Data;
using Mindbite.Mox.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mindbite.Mox.NotificationCenter.Communication;
using Microsoft.AspNetCore.Mvc.Razor;
using Mindbite.Mox.Utils.FileProviders;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Mindbite.Mox.Extensions;

namespace Mindbite.Mox.NotificationCenter
{
    public static class Constants
    {
        public const string MainArea = "NotificationCenter";
    }

    public static class Configuration
    {
        public static void AddMoxNotificationCenter(this IServiceCollection services, IHostingEnvironment hostingEnvironment, IConfigurationRoot appConfiguration, string moxPath = "Mox", string staticRequestPath = "/static")
        {
            services.AddScoped<INotificationSender, NotificationCenterSender>();
            services.AddScoped<INotificationReciever, NotificationCenterReciever>();
            services.AddScoped<INotificationSubscriber, NotificationCenterSubscriber>();

            services.AddScoped<EmailMessenger>();
            services.AddScoped<PalomaTextMessenger>();

            services.AddScoped<IMessengerProvider, MessengerProvider>(options => {
                return new MessengerProvider(services.BuildServiceProvider(), m => {
                    m.Add(typeof(EmailMessenger));
                    m.Add(typeof(PalomaTextMessenger));
                });
            });

            services.Configure<Mox.Configuration.Config>(c =>
            {
                var app = c.Apps.Add("Notifikationscenter", "notificationcenter");
                app.Areas.Add(Constants.MainArea);
                app.Menu.Items(items =>
                {
                    items.Add()
                        .Title("Notiser")
                        .AreaAction(Constants.MainArea, "Notifications");
                    items.Add()
                        .Title("Prenumerationer")
                        .AreaAction(Constants.MainArea, "Subscriptions");
                });
                app.HeaderPartial = new Mox.Configuration.Apps.AppPartial() { Name = "Mox/NotificationCenter/_Header", Position = 500 };
            });

            services.Configure<RazorViewEngineOptions>(c =>
            {
                c.FileProviders.Add(new EmbeddedFilesInAssemblyFileProvider(typeof(Configuration).GetTypeInfo().Assembly, hostingEnvironment));
            });

            services.Configure<Mox.Configuration.StaticIncludes.IncludeConfig>(c =>
            {
                c.StaticRoot = staticRequestPath;
                c.Styles.Add(new Mox.Configuration.StaticIncludes.Style("notificationcenter/css/mox_base.css"));
                c.Scripts.Add(staticRequestPath.TrimEnd('/') + "/notificationcenter/js/notification_center.js");
            });

            services.Configure<Mox.Communication.EmailOptions>(appConfiguration.GetSection("EmailSender"));
            services.AddScoped<Mox.Communication.EmailSender>();
        }

        public static void MapMoxNotificationCenterRoutes(this IRouteBuilder routes, string moxPath = "Mox")
        {
            routes.MapAreaRoute("Notifications", Constants.MainArea, $"{moxPath}/{Constants.MainArea}/{{controller}}/{{action=Index}}/{{id?}}");
        }

        public static void UseMoxNotificationCenterStaticFiles(this IApplicationBuilder app, IHostingEnvironment hostingEnvironment, string requestPath = "/static")
        {
            app.AddStaticFileFileProvider(typeof(Configuration), hostingEnvironment, requestPath);
        }
    }
}
