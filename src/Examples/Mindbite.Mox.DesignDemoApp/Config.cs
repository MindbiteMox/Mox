using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Mindbite.Mox.Utils.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Mindbite.Mox.UI.Menu;
using Mindbite.Mox.Extensions;

namespace Mindbite.Mox.DesignDemoApp.Configuration
{
    public static class Constants
    {
        public const string MainArea = "DesignDemoMoxApp";
    }

    public static class ConfigExtensions
    {
        public static void AddDesignDemoMoxApp(this IServiceCollection services, IHostingEnvironment hostingEnvironment, IConfigurationRoot appConfiguration)
        {
            /*services.AddDbContext<Data.DesignDbContext>(options =>
            {
                options.UseSqlServer(appConfiguration.GetConnectionString("DefaultConnection"), x => x.MigrationsAssembly(hostingEnvironment.ApplicationName));
            });*/

            services.Configure<Mox.Configuration.Config>(c =>
            {
                var app = c.Apps.Add("Designs demo app", "designs");
                app.Areas.Add(Constants.MainArea);
                app.Menu.Items(items =>
                {
                    items.Add()
                        .Title("Mina designs")
                        .AreaAction(Constants.MainArea, "Designs");
                });
            });

            services.Configure<RazorViewEngineOptions>(c =>
            {
                c.FileProviders.Add(new EmbeddedFilesInAssemblyFileProvider(typeof(ConfigExtensions).GetTypeInfo().Assembly, hostingEnvironment));
            });
        }

        public static void MapDesignDemoRoutes(this IRouteBuilder routes, string moxPath = "Mox")
        {
            routes.MapAreaRoute("Design", Constants.MainArea, $"{moxPath}/Designs/{{controller}}/{{action=Index}}/{{id?}}");
        }

        public static void UseDesignDemoStaticFiles(this IApplicationBuilder app, IHostingEnvironment hostingEnvironment, string requestPath = "/static")
        {
            app.AddStaticFileFileProvider(typeof(ConfigExtensions), hostingEnvironment, requestPath);
        }
    }
}
