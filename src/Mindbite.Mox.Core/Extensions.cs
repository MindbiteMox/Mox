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
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Hosting;

namespace Mindbite.Mox.Extensions
{
    public static class Conversions
    {
        public static ExpandoObject ToExpando(this object anonymousObject)
        {
            IDictionary<string, object> anonymousDictionary = new RouteValueDictionary(anonymousObject);
            IDictionary<string, object> expando = new ExpandoObject();
            
            foreach (var item in anonymousDictionary)
            {
                expando.Add(item);
            }

            return (ExpandoObject)expando;
        }

        public static int? TryToInt(this object input)
        {
            if (int.TryParse(input?.ToString(), out var result))
            {
                return result;
            }

            return null;
        }

        public static DateTime? TryToDateTime(this object input)
        {
            if (DateTime.TryParse(input?.ToString(), out var result))
            {
                return result;
            }

            return null;
        }

        public static T? TryCast<T>(this object input) where T : struct
        {
            try
            {
                return (T)input;
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="trimAndReplaceCommas">Trim spaces and replace ',' with '.'</param>
        /// <param name="format">Defaults to System.Globalization.CultureInfo.InvariantCulture if this parameter is null.</param>
        /// <returns></returns>
        public static double? TryToDouble(this object input, bool trimAndReplaceCommas = true, IFormatProvider format = null)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var _input = input.ToString();
            var _format = format ?? System.Globalization.CultureInfo.InvariantCulture;
            var _numberStyle = System.Globalization.NumberStyles.Any;

            if (trimAndReplaceCommas)
            {
                _input = _input?.Replace(',', '.')?.Replace(" ", "");
            }

            if (double.TryParse(_input, _numberStyle, _format, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Tries to convert an int to <typeparamref name="T"/>, and returns null if it cannot be converted.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T? TryToEnum<T>(this int value) where T : struct, Enum
        {
            if (Enum.IsDefined(typeof(T), value))
            {
                return (T)Enum.ToObject(typeof(T), value);
            }

            return null;
        }

        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());

            var attributes = (DisplayAttribute[])field.GetCustomAttributes(typeof(DisplayAttribute), false);

            return attributes?.FirstOrDefault()?.Name ?? value.ToString();
        }

        public static string GetShortName(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());

            var displayAttribute = field.GetCustomAttribute<DisplayAttribute>(false);

            return displayAttribute.ShortName;
        }
    }

    public static partial class MoxExtensions
    {
        public static IMvcBuilder AddMoxWithoutDb(this IMvcBuilder mvc, IWebHostEnvironment webHostEnvironment, string path = "Mox", string siteTitle = "Mox", string staticRequestPath = "")
        {
            if (webHostEnvironment == null)
            {
                throw new ArgumentNullException($"Parameter {nameof(webHostEnvironment)} cannot be null!");
            }

            var thisAssembly = typeof(MoxExtensions).Assembly;
            mvc.AddApplicationPart(thisAssembly);
            var viewsDLLName = thisAssembly.GetName().Name + ".Views.dll";
            var viewsDLLDirectory = Path.GetDirectoryName(thisAssembly.Location);
            var viewsDLLPath = Path.Combine(viewsDLLDirectory, viewsDLLName);
            if (File.Exists(viewsDLLPath))
            {
                var viewAssembly = Assembly.LoadFile(viewsDLLPath);
                var viewAssemblyPart = new CompiledRazorAssemblyPart(viewAssembly);
                mvc.ConfigureApplicationPartManager(manager => manager.ApplicationParts.Add(viewAssemblyPart));
            }
            else
            {
                var viewAssemblyPart = new CompiledRazorAssemblyPart(thisAssembly);
                mvc.ConfigureApplicationPartManager(manager => manager.ApplicationParts.Add(viewAssemblyPart));
            }

            mvc.Services.Configure<Configuration.Config>(c =>
            {
                c.Path ??= path;
                c.SiteTitle ??= siteTitle;
            });

            mvc.Services.Configure<IncludeConfig>(c =>
            {
                c.StaticRoot = staticRequestPath;
                c.Files.Add(StaticFile.Style("mox/static/css/base.css"));
                c.Files.Add(StaticFile.Style("mox/static/css/base_mobile.css", maxWidth: 960));
                c.Files.Add(StaticFile.Style("mox/static/fonts/inter/inter.css"));
                c.Files.Add(StaticFile.Script("mox/static/js/utils.js"));
                c.Files.Add(StaticFile.Script("mox/static/js/MoxUI.js"));
            });

            mvc.Services.Configure<RazorViewEngineOptions>(c =>
            {
                c.ViewLocationExpanders.Add(new AlwaysLookForSharedLocationExpander());
            });

            mvc.Services.AddScoped<IViewRenderService, ViewRenderService>();
            mvc.Services.AddScoped<IStringLocalizer, MoxStringLocalizer>();
            mvc.Services.AddTransient<IConfigureOptions<MvcDataAnnotationsLocalizationOptions>, MoxDataAnnotationsLocalizationOptionsSetup>();

            mvc.Services.AddScoped<ViewMessaging>();

            var userRolesFetcher = mvc.Services.FirstOrDefault(x => x.ServiceType == typeof(IUserRolesFetcher));
            if (userRolesFetcher == null)
            {
                mvc.Services.AddScoped<IUserRolesFetcher, DummyUserRolesFetcher>();
            }

            mvc.Services.Configure<StaticFileProviderOptions>(c =>
            {
                c.FileProviders.Add(new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webHostEnvironment.WebRootPath));
            });

            mvc.Services.Configure<MvcOptions>(c =>
            {
                c.ModelBinderProviders.Insert(0, new Utils.ModelBinders.DoubleBinderProvider());
            });

            return mvc;
        }

        public static IMvcBuilder AddMox<AppDbContext_T>(this IMvcBuilder mvc, IWebHostEnvironment webHostEnvironment, string path = "Mox") where AppDbContext_T : DbContext, Core.Data.IDbContext
        {
            mvc.AddMoxWithoutDb(webHostEnvironment, path);
            mvc.Services.AddScoped<IDbContextFetcher, DbContextFetcher<AppDbContext_T>>();

            return mvc;
        }

#nullable enable
        public static void MapMoxRoutes(this IEndpointRouteBuilder endpoints, string path = "Mox", Action<IEndpointConventionBuilder>? endpointAdded = null)
        {
            endpointAdded ??= _ => { };

            endpointAdded(endpoints.MapAreaControllerRoute("Mox Start", "Mox", path, new
            {
                Area = "Mox",
                Controller = "Home",
                Action = "Index",
            }));
            endpointAdded(endpoints.MapAreaControllerRoute("Mox Error", "Mox", "Mox/Error/{errorCode}", new
            {
                Area = "Mox",
                Controller = "Home",
                Action = "Error",
            }));
        }

        public static void MapRedirectToMoxRoutes(this IEndpointRouteBuilder endpoints, string path = "Mox", Action<IEndpointConventionBuilder>? endpointAdded = null)
        {
            endpointAdded ??= _ => { };

            endpointAdded(endpoints.MapAreaControllerRoute(nameof(MapRedirectToMoxRoutes), "Mox", "{*wildcard}", new
            {
                Area = "Mox",
                Controller = "RedirectToMox",
                Action = "Index",
            }));
        }

        public static void UseMoxExceptionPage(this IApplicationBuilder app)
        {
            app.UseExceptionHandler("/Mox/Error/500");
            app.UseStatusCodePagesWithReExecute("/Mox/Error/{0}");
        }

        public static void UseMoxStaticFiles(this IApplicationBuilder app, IWebHostEnvironment webHostEnvironment, string requestPath = "")
        {
            app.AddStaticFileFileProvider(typeof(MoxExtensions), webHostEnvironment, requestPath);
        }

        public static void AddStaticFileFileProvider<T>(this IApplicationBuilder app, IWebHostEnvironment webHostEnvironment, string requestPath)
        {
            app.AddStaticFileFileProvider(typeof(T), webHostEnvironment, requestPath);
        }

        public static void AddStaticFileFileProvider(this IApplicationBuilder app, Type type, IWebHostEnvironment webHostEnvironment, string requestPath)
        {
            var fileProvider = new StaticFilesInAssemblyFileProvider(type.GetTypeInfo().Assembly, webHostEnvironment);
            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = requestPath,
                FileProvider = fileProvider,
                OnPrepareResponse = ctx =>
                {
                    if (webHostEnvironment.IsDevelopment())
                    {
                        return;
                    }

                    ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age=2592000");
                }
            });

            var staticFileProviders = app.ApplicationServices.GetService<IOptions<StaticFileProviderOptions>>();
            staticFileProviders.Value.FileProviders.Add(fileProvider);
        }

        public static string AppAction(this IUrlHelper url, Configuration.Apps.App app, IEnumerable<string> userRoles, object routeValues = null)
        {
            return url.MenuAction(app.ResolveActiveMenu(url.ActionContext).Build(url, userRoles), userRoles, routeValues);
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
        }

        public static string MenuAction(this IUrlHelper url, UI.Menu.MenuItem menuItem, IEnumerable<string> userRoles, object routeValues = null)
        {
            return menuItem.Url;
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

        public static void DisplayMessage(this Controller controller, string message, params string[] additionalLines)
        {
            var viewMessaging = controller.HttpContext.RequestServices.GetRequiredService<ViewMessaging>();
            viewMessaging.DisplayMessage(message, additionalLines);
        }

        public static void DisplayError(this Controller controller, string message, params string[] additionalLines)
        {
            var viewMessaging = controller.HttpContext.RequestServices.GetRequiredService<ViewMessaging>();
            viewMessaging.DisplayError(message, additionalLines);
        }
    }

    public static class QueryableExtensions
    {
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string ordering)
        {
            var type = typeof(T);
            var orderByExp = Utils.Dynamics.GetLambdaExpression(type, ordering.Split('.'));
            var resultExp = Expression.Call(typeof(Queryable), "OrderBy", new Type[] { type, orderByExp.ReturnType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        public static IQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string ordering)
        {
            var type = typeof(T);
            var orderByExp = Utils.Dynamics.GetLambdaExpression(type, ordering.Split('.'));
            var resultExp = Expression.Call(typeof(Queryable), "OrderByDescending", new Type[] { type, orderByExp.ReturnType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        public static IQueryable<T> ThenBy<T>(this IQueryable<T> source, string ordering)
        {
            var type = typeof(T);
            var orderByExp = Utils.Dynamics.GetLambdaExpression(type, ordering.Split('.'));
            var resultExp = Expression.Call(typeof(Queryable), "ThenBy", new Type[] { type, orderByExp.ReturnType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        public static IQueryable<T> ThenByDescending<T>(this IQueryable<T> source, string ordering)
        {
            var type = typeof(T);
            var orderByExp = Utils.Dynamics.GetLambdaExpression(type, ordering.Split('.'));
            var resultExp = Expression.Call(typeof(Queryable), "ThenByDescending", new Type[] { type, orderByExp.ReturnType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }
    }

    public static class HttpRequestExtensions
    {
        private const string RequestedWithHeader = "X-Requested-With";
        private const string XmlHttpRequest = "XMLHttpRequest";

        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Headers != null)
            {
                return request.Headers[RequestedWithHeader] == XmlHttpRequest;
            }

            return false;
        }
    }
}

