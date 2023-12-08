using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mindbite.Mox.Extensions
{
    public static class MoxDirectoryListingExtensions
    {
        public class DocumentServicesBuilder
        {
            private readonly IServiceCollection _serviceCollection;

            public DocumentServicesBuilder(IServiceCollection serviceCollection)
            {
                this._serviceCollection = serviceCollection;
            }

            public void AddDocumentServiceForType<TDocument, TDirectory>() where TDocument : DirectoryListing.Data.Document<TDirectory>, new() where TDirectory : DirectoryListing.Data.DocumentDirectory<TDocument, TDirectory>, new()
            {
                this._serviceCollection.AddScoped<DirectoryListing.Services.DocumentService<TDocument, TDirectory>>();
            }
        }

        public static IMvcBuilder AddMoxDirectoryListing(this IMvcBuilder mvc, Action<DocumentServicesBuilder> configureDocumentTypes, string moxPath = "Mox")
        {
            var thisAssembly = typeof(MoxDirectoryListingExtensions).Assembly;
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
                c.Files.Add(Configuration.StaticIncludes.StaticFile.Style("mox/Static/DirectoryListing/css/base.css"));

                c.Files.Add(Configuration.StaticIncludes.StaticFile.Script("mox/Static/DirectoryListing/js/global.js"));
            });

            mvc.Services.Configure<DirectoryListing.DocumentServiceOptions>(_ => { });

            mvc.Services.Configure<LocalizationSources>(options =>
            {
                options.ResouceTypes.Add(typeof(Mindbite.Mox.DirectoryListing.Localization));
            });

            var configurer = new DocumentServicesBuilder(mvc.Services);
            configureDocumentTypes(configurer);

            return mvc;
        }

        public static void MapMoxDirectoryListingRoutes(this IEndpointRouteBuilder endpoints, string moxPath = "Mox", Action<IEndpointConventionBuilder>? endpointAdded = null)
        {
            endpointAdded ??= _ => { };
        }

        public static void UseMoxDirectoryListingStaticFiles(this IApplicationBuilder app, IWebHostEnvironment webHostEnvironment, string requestPath = "")
        {
            app.AddStaticFileFileProvider(typeof(MoxDirectoryListingExtensions), webHostEnvironment, requestPath);
        }
    }
}
