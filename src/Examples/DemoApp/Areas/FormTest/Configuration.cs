using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.DemoApp.Areas.FormTest
{
    public static class Configuration
    {
        public const string AppId = "TestForm";
        public const string AppName = "Test Form";
        public const string MainArea = "TestForm";

        public static IMvcBuilder AddTestFormApp(this IMvcBuilder mvc)
        {
            mvc.Services.Configure<Mindbite.Mox.Configuration.Config>(c =>
            {
                var app = c.Apps.FirstOrDefault(x => x.AppId == AppId);
                if (app == null)
                {
                    app = c.Apps.Add(AppName, AppId);
                }

                app.Areas.Add(MainArea);
                app.Menu.Items(items =>
                {
                    items.Add()
                        .Title("Forms")
                        .AreaAction(MainArea, "TestForm");
                });
            });

            return mvc;
        }

        public static void MapTestFormAppRoutes(this IEndpointRouteBuilder routes, string moxPath = "Mox")
        {
            routes.MapAreaControllerRoute(AppId, MainArea, $"{moxPath}/{MainArea}/{{controller}}/{{action=Index}}/{{id?}}");
        }
    }
}
