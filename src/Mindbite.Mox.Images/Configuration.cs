using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Mindbite.Mox.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mindbite.Mox.Images
{
    public static class Configuration
    {
        public const string MainArea = "MoxImages";

        public static IMvcBuilder AddMoxImages(this IMvcBuilder mvc, string staticRequestPath = "")
        {
            var thisAssembly = typeof(Configuration).Assembly;
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

            mvc.Services.AddScoped<Services.ImageService>();

            mvc.Services.Configure<Mox.Configuration.StaticIncludes.IncludeConfig>(c =>
            {
                c.StaticRoot = staticRequestPath;
                c.Files.Add(Mox.Configuration.StaticIncludes.StaticFile.Script("mox/static/images/js/images.js"));
                c.Files.Add(Mox.Configuration.StaticIncludes.StaticFile.Style("mox/static/images/css/images.css"));
            });

            return mvc;
        }

        public static void MapMoxImagesRoutes(this IEndpointRouteBuilder endpoints, string moxPath = "Mox")
        {
            endpoints.MapAreaControllerRoute(MainArea, MainArea, $"{moxPath}/{MainArea}/{{controller}}/{{action=Index}}/{{id?}}");
        }

        public static void UseMoxImagesStaticFiles(this IApplicationBuilder app, IWebHostEnvironment webHostEnvironment, string requestPath = "")
        {
            app.AddStaticFileFileProvider(typeof(Configuration), webHostEnvironment, requestPath);
        }
    }
}
