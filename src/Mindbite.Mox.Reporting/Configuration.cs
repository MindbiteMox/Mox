using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Mindbite.Mox.Reporting
{
    public class MoxReportingOptions
    {
        public string ReportDirectory { get; set; }
        public Uri ServerUrl { get; set; }
        public string SharedSecret { get; set; }
        public Func<Services.ReportingService.Report, Microsoft.AspNetCore.Http.HttpContext, bool> FilterReportingAppList { get; set; } = (x, y) => x.ShowInList;
        /// <summary>
        /// Parameters will be forwarded to the reportviewer. Parameter names are formatted with $"p{index + 1}"
        /// </summary>
        public Func<Services.ReportingService.Report, Microsoft.AspNetCore.Http.HttpContext, IEnumerable<object>> GetReportParams { get; set; } = (x, y) => Enumerable.Empty<object>();
        public Func<Services.ReportingService.Report, Microsoft.AspNetCore.Http.HttpContext, IEnumerable<(string name, object value)>> GetReportNamedParams { get; set; } = (x, y) => Enumerable.Empty<(string, object)>();
        public bool EnableAuthorization { get; set; }
        public int AuthorizationTimeoutInMinutes { get; set; } = 5;
        public Func<Microsoft.AspNetCore.Http.HttpContext, bool> AuthorizeUser { get; set; } = (x) => true;
        public bool EnableReportingApp { get; set; } = true;
    }

    public static class Configuration
    {
        public const string AppName = "Rapporter";
        public const string AppId = "Reports";
        public const string MainArea = "Reports";

        public static IMvcBuilder AddMoxReportingApp(this IMvcBuilder mvc, IConfiguration config, Action<MoxReportingOptions>? setupAction = null)
        {
            mvc.Services.AddMoxReportingServices(config.GetSection("Reporting"), setupAction);

            var appEnabled = true;

            mvc.Services.Configure<MoxReportingOptions>(c =>
            {
                appEnabled = c.EnableReportingApp;
            });

            mvc.Services.Configure<Mox.Configuration.Config>(c =>
            {
                if (appEnabled)
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
                            .Title("Rapporter")
                            .Id("Reports")
                            .AreaAction(MainArea, "Report");
                    });
                }
            });

            return mvc;
        }

        public static void MapMoxReportingAppRoutes(this IEndpointRouteBuilder endpoints, string moxPath = "Mox")
        {
            endpoints.MapAreaControllerRoute(AppId, MainArea, $"{moxPath}/{MainArea}/{{controller}}/{{action=Index}}/{{id?}}");
        }

        public static IServiceCollection AddMoxReportingServices(this IServiceCollection services, IConfiguration? config = null, Action<MoxReportingOptions>? setupAction = null)
        {
            services.AddScoped<Services.ReportingService>();

            if(config != null)
            {
                services.Configure<MoxReportingOptions>(config);
            }

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return services;
        }
    }
}