using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Mindbite.Mox.HtmlTable;
using Mindbite.Mox.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mindbite.Mox.HtmlTable
{
    public static class HtmlTableExtensions
    {
        public static IMvcBuilder AddMoxHtmlTable(this IMvcBuilder mvc)
        {
            var thisAssembly = typeof(HtmlTableExtensions).Assembly;
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
                c.Files.Add(Configuration.StaticIncludes.StaticFile.Style("mox/static/htmltable/css/htmltable.min.css"));
            });

            return mvc;
        }

        public static void UseMoxHtmlTableStaticFiles(this IApplicationBuilder app, IWebHostEnvironment webHostEnvironment, string requestPath = "")
        {
            app.AddStaticFileFileProvider(typeof(HtmlTableExtensions), webHostEnvironment, requestPath);
        }

        public static ICell ToCell(this string value, CellOptions? options = null)
        {
            return IHtmlTable.NewCell(value, options);
        }
    }
}