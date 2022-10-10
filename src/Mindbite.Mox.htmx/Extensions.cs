using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Mindbite.Mox.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mindbite.Mox.Extensions
{
    public static class HtmxExtensions
    {
        public class UpdatePanel : IDisposable
        {
            private readonly IHtmlHelper _html;

            public UpdatePanel(IHtmlHelper html, string prefix, string? id = null)
            {
                this._html = html;

                this._html.ViewContext.Writer.WriteLine($"<div id=\"{id}\" hx-target=\"this\" hx-swap=\"outerHTML\" hx-ext=\"ajax-header, mox-prefix\" prefix=\"{prefix}\">");
            }

            public void Dispose()
            {
                this._html.ViewContext.Writer.WriteLine("</div>");
            }
        }

        public static UpdatePanel BeginUpdatePanel(this IHtmlHelper helper, out string htmxPostUrl, string? id = null)
        {
            var linkGenerator = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();
            htmxPostUrl = linkGenerator.GetPathByAction(helper.ViewContext.HttpContext, action: "UpdateEditorTemplate", controller: "Htmx", values: new
            {
                prefix = helper.ViewData.TemplateInfo.HtmlFieldPrefix,
                template = helper.ViewContext.View.Path,
                Area = ""
            }) ?? throw new Exception("Route not found");
            return new UpdatePanel(helper, helper.ViewData.TemplateInfo.HtmlFieldPrefix, id);
        }
    }
}

namespace Mindbite.Mox.Htmx
{
    public static class HtmxExtensions
    {
        public static IMvcBuilder AddMoxHtmx(this IMvcBuilder mvc)
        {
            var thisAssembly = typeof(HtmxExtensions).Assembly;
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

            mvc.Services.Configure<Configuration.StaticIncludes.IncludeConfig>(c =>
            {
                c.Files.Add(Configuration.StaticIncludes.StaticFile.Script("mox/static/htmx/js/htmx.min.js"));
            });

            return mvc;
        }

        public static void MapMoxHtmxRoutes(this IEndpointRouteBuilder endpoints, string moxPath = "Mox")
        {
            endpoints.MapControllerRoute("HtmxController", $"{moxPath}/Htmx/{{action}}".TrimStart('/'), defaults: new { Controller = "Htmx" });
        }

        public static void UseMoxHtmxStaticFiles(this IApplicationBuilder app, IWebHostEnvironment webHostEnvironment, string requestPath = "")
        {
            app.AddStaticFileFileProvider(typeof(HtmxExtensions), webHostEnvironment, requestPath);
        }
    }
}