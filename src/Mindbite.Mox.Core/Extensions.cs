using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Mindbite.Mox.UI;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Html;
using System.Text;
using System.IO;
using Mindbite.Mox.Utils.FileProviders;
using Mindbite.Mox.Utils.ViewLocationExpanders;
using Mindbite.Mox.Services;
using Mindbite.Mox.Configuration.StaticIncludes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

namespace Mindbite.Mox.Extensions
{
    public static partial class MoxExtensions
    {
        public static ExpandoObject ToExpando(this object anonymousObject)
        {
            IDictionary<string, object> anonymousDictionary = new RouteValueDictionary(anonymousObject);
            IDictionary<string, object> expando = new ExpandoObject();
            foreach (var item in anonymousDictionary)
                expando.Add(item);
            return (ExpandoObject)expando;
        }

        public static void AddMoxWithoutDb(this IServiceCollection services, IHostingEnvironment hostingEnvironment, string path = "Mox", string siteTitle = "Mox", string staticRequestPath = "/static")
        {
            if (hostingEnvironment == null)
                throw new ArgumentNullException($"Parameter {nameof(hostingEnvironment)} cannot be null!");

            services.Configure<Configuration.Config>(c =>
            {
                c.Path = c.Path ?? path;
                c.SiteTitle = c.SiteTitle ?? siteTitle;
            });

            services.Configure<IncludeConfig>(c =>
            {
                c.StaticRoot = staticRequestPath;
                c.Files.Add(StaticFile.Style("mox/css/base.css"));
                c.Files.Add(StaticFile.Style("mox/css/base_mobile.css", maxWidth: 960));
                c.Files.Add(StaticFile.Script("mox/js/utils.js"));
                c.Files.Add(StaticFile.Script("mox/js/MoxUI.js"));
            });

            services.Configure<RazorViewEngineOptions>(c =>
            {
                c.FileProviders.Add(new EmbeddedFilesInAssemblyFileProvider(typeof(MoxExtensions).GetTypeInfo().Assembly, hostingEnvironment));
                c.ViewLocationExpanders.Add(new AlwaysLookForSharedLocationExpander());
            });

            services.AddScoped<IViewRenderService, ViewRenderService>();
            services.AddScoped<IStringLocalizer, MoxStringLocalizer>();
            services.AddTransient<IConfigureOptions<MvcDataAnnotationsLocalizationOptions>, MoxDataAnnotationsLocalizationOptionsSetup>();
            //services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<MvcDataAnnotationsLocalizationOptions>, MoxDataAnnotationsLocalizationOptionsSetup>());

            var userRolesFetcher = services.FirstOrDefault(x => x.ServiceType == typeof(IUserRolesFetcher));
            if (userRolesFetcher == null)
            {
                services.AddScoped<IUserRolesFetcher, DummyUserRolesFetcher>();
            }

            services.Configure<StaticFileProviderOptions>(c =>
            {
                c.FileProviders.Add(new Microsoft.Extensions.FileProviders.PhysicalFileProvider(hostingEnvironment.WebRootPath));
            });
        }

        public static void AddMox<AppDbContext_T>(this IServiceCollection services, IHostingEnvironment hostingEnvironment, string path = "Mox") where AppDbContext_T : DbContext, IDbContext
        {
            services.AddMoxWithoutDb(hostingEnvironment, path);
            services.AddScoped<IDbContextFetcher, DbContextFetcher<AppDbContext_T>>();
        }

        public static void MapMoxRoutes(this IRouteBuilder routes, string path = "Mox")
        {
            routes.MapAreaRoute("Mox Start", "Mox", path, new
            {
                Area = "Mox",
                Controller = "Home",
                Action = "Index",
            });
        }

        public static void MapRedirectToMoxRoutes(this IRouteBuilder routes)
        {
            routes.MapAreaRoute(nameof(MapRedirectToMoxRoutes), "Mox", "{*wildcard}", new
            {
                Area = "Mox",
                Controller = "RedirectToMox",
                Action = "Index",
            });
        }

        public static void UseMoxStaticFiles(this IApplicationBuilder app, IHostingEnvironment hostingEnvironment, string requestPath = "/static")
        {
            app.AddStaticFileFileProvider(typeof(MoxExtensions), hostingEnvironment, requestPath);
        }

        public static void AddStaticFileFileProvider<T>(this IApplicationBuilder app, IHostingEnvironment hostingEnvironment, string requestPath)
        {
            app.AddStaticFileFileProvider(typeof(T), hostingEnvironment, requestPath);
        }

        public static void AddStaticFileFileProvider(this IApplicationBuilder app, Type type, IHostingEnvironment hostingEnvironment, string requestPath)
        {
            var fileProvider = new StaticFilesInAssemblyFileProvider(type.GetTypeInfo().Assembly, hostingEnvironment);
            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = requestPath,
                FileProvider = fileProvider
            });

            var staticFileProviders = app.ApplicationServices.GetService<IOptions<StaticFileProviderOptions>>();
            staticFileProviders.Value.FileProviders.Add(fileProvider);
        }

        public static string AppAction(this IUrlHelper url, Configuration.Apps.App app, IEnumerable<string> userRoles, object routeValues = null)
        {
            return url.MenuAction(app.Menu.Build(url, userRoles), userRoles, routeValues);
        }

        public static string MenuAction(this IUrlHelper url, IEnumerable<UI.Menu.MenuItem> menu, IEnumerable<string> userRoles, object routeValues = null)
        {
            if (!menu.Any())
                return "/error";

            return menu.First().Url;
        }

        public static string MenuAction(this IUrlHelper url, UI.Menu.MenuItem menuItem, object routeValues = null)
        {
            if (menuItem == null)
                return "/error";

            return menuItem.Url;

            /*dynamic menuRouteValues = new ExpandoObject();

            if(menuItem.Area != null)
                menuRouteValues.Area = menuItem.Area;

            object values = Utils.Dynamics.Merge(menu.RouteValues, Utils.Dynamics.Merge(routeValues, menuRouteValues));

            return url.Action(menu.Action, menu.Controller, values);*/
        }

        public static string MenuAction(this IUrlHelper url, UI.Menu.MenuItem menuItem, IEnumerable<string> userRoles, object routeValues = null)
        {
            return menuItem.Url;
            /*if (menuItem.Controller == null)
            {
                return url.MenuAction(menu.Items.FirstOrDefault(x => userRoles == null || userRoles.Intersect(x.Roles).Count() == x.Roles.Count()), routeValues);
            }

            return url.MenuAction(menu, routeValues);*/
        }

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string ordering)
        {
            var type = typeof(T);
            var orderByExp = Utils.Dynamics.GetLambdaExpression(type, ordering.Split('.'));
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "OrderBy", new Type[] { type, orderByExp.ReturnType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        public static IQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string ordering)
        {
            var type = typeof(T);
            var orderByExp = Utils.Dynamics.GetLambdaExpression(type, ordering.Split('.'));
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "OrderByDescending", new Type[] { type, orderByExp.ReturnType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        public static IQueryable<T> ThenBy<T>(this IQueryable<T> source, string ordering)
        {
            var type = typeof(T);
            var orderByExp = Utils.Dynamics.GetLambdaExpression(type, ordering.Split('.'));
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "ThenBy", new Type[] { type, orderByExp.ReturnType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        public static IQueryable<T> ThenByDescending<T>(this IQueryable<T> source, string ordering)
        {
            var type = typeof(T);
            var orderByExp = Utils.Dynamics.GetLambdaExpression(type, ordering.Split('.'));
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "ThenByDescending", new Type[] { type, orderByExp.ReturnType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        /// <summary>
        /// Returns an Ok(json) with dataTable page items result if request header "Content-Type" == "application/json"
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public static IActionResult ViewOrOk(this Controller controller, IDataTable dataTable)
        {
            if (controller.Request.Headers["Content-Type"] == "application/json")
            {
                return controller.PartialView("Mox/UI/DataTable", dataTable);
            }
            else
            {
                return controller.View(dataTable);
            }
        }
    }

    public static class HttpRequestExtensions
    {
        private const string RequestedWithHeader = "X-Requested-With";
        private const string XmlHttpRequest = "XMLHttpRequest";

        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (request.Headers != null)
            {
                return request.Headers[RequestedWithHeader] == XmlHttpRequest;
            }

            return false;
        }
    }
}

