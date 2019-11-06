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
using System.IO;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Mindbite.Mox.NotificationCenter
{
    public static class Constants
    {
        public const string MainArea = "NotificationCenter";
    }

    public static class Configuration
    {
        public static IMvcBuilder AddMoxNotificationCenter(this IMvcBuilder mvc, IHostingEnvironment hostingEnvironment, IConfigurationRoot appConfiguration, string moxPath = "Mox", string staticRequestPath = "/static")
        {
            var thisAssembly = typeof(Configuration).Assembly;
            mvc.AddApplicationPart(thisAssembly);
            var viewsDLLName = thisAssembly.GetName().Name + ".Views.dll";
            var viewsDLLDirectory = Path.GetDirectoryName(thisAssembly.Location);
            var viewsDLLPath = Path.Combine(viewsDLLDirectory, viewsDLLName);
            if(File.Exists(viewsDLLPath)){
                var viewAssembly = Assembly.LoadFile(viewsDLLPath);
                var viewAssemblyPart = new CompiledRazorAssemblyPart(viewAssembly);
                mvc.ConfigureApplicationPartManager(manager => manager.ApplicationParts.Add(viewAssemblyPart));
            } else {
                var viewAssemblyPart = new CompiledRazorAssemblyPart(thisAssembly);
                mvc.ConfigureApplicationPartManager(manager => manager.ApplicationParts.Add(viewAssemblyPart));
            }

            mvc.Services.AddScoped<INotificationSender, NotificationCenterSender>();
            mvc.Services.AddScoped<INotificationReciever, NotificationCenterReciever>();
            mvc.Services.AddScoped<INotificationSubscriber, NotificationCenterSubscriber>();

            mvc.Services.AddScoped<EmailMessenger>();
            mvc.Services.AddScoped<PalomaTextMessenger>();

            mvc.Services.AddScoped<IMessengerProvider, MessengerProvider>(options => {
                return new MessengerProvider(mvc.Services.BuildServiceProvider(), m => {
                    m.Add(typeof(EmailMessenger));
                    m.Add(typeof(PalomaTextMessenger));
                });
            });

            mvc.Services.Configure<Mox.Configuration.Config>(c =>
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

            mvc.Services.Configure<RazorViewEngineOptions>(c =>
            {
                //c.FileProviders.Add(new EmbeddedFilesInAssemblyFileProvider(typeof(Configuration).Assembly, hostingEnvironment));
            });

            mvc.Services.Configure<Mox.Configuration.StaticIncludes.IncludeConfig>(c =>
            {
                c.StaticRoot = staticRequestPath;
                c.Files.Add(Mox.Configuration.StaticIncludes.StaticFile.Style("notificationcenter/css/mox_base.css"));
                c.Files.Add(Mox.Configuration.StaticIncludes.StaticFile.Script("notificationcenter/js/notification_center.js"));
            });

            mvc.Services.Configure<Mox.Communication.EmailOptions>(appConfiguration.GetSection("EmailSender"));
            mvc.Services.AddScoped<Mox.Communication.EmailSender>();

            return mvc;
        }

        public static void MapMoxNotificationCenterRoutes(this IEndpointRouteBuilder endpoints, string moxPath = "Mox")
        {
            endpoints.MapAreaControllerRoute("Notifications", Constants.MainArea, $"{moxPath}/{Constants.MainArea}/{{controller}}/{{action=Index}}/{{id?}}");
        }

        public static void UseMoxNotificationCenterStaticFiles(this IApplicationBuilder app, IHostingEnvironment hostingEnvironment, string requestPath = "/static")
        {
            app.AddStaticFileFileProvider(typeof(Configuration), hostingEnvironment, requestPath);
        }
    }
}
