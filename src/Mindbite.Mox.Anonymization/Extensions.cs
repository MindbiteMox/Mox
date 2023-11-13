using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Anonymization.Data;
using Mindbite.Mox.Anonymization.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Mindbite.Mox.Anonymization
{
    public static class AnonymizationExtensions
    {
        /// <summary>
        /// Adds anonymization services. Configure how it works with services.ConfigureMoxAnonymization(...)
        /// Don't forget MapMoxAnonymizationRoutes and to schedule
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="mvc"></param>
        /// <returns></returns>
        public static IMvcBuilder AddMoxAnonymization<TDbContext>(this IMvcBuilder mvc) where TDbContext : DbContext
        {
            var thisAssembly = typeof(AnonymizationExtensions).Assembly;
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

            mvc.Services.AddScoped<IDeferredAnonymizationService, DeferredAnonymizationService<TDbContext>>();

            return mvc;
        }

        public static IServiceCollection ConfigureMoxAnonymization<TDbContext>(this IServiceCollection services, Action<AnonymizationOptions<TDbContext>> configure) where TDbContext : DbContext
        {
            services.Configure<AnonymizationOptions<TDbContext>>(c =>
            {
                configure(c);
            });

            return services;
        }

        /// <summary>
        /// Apply in startup.cs when configuring options with services.AddDbContext<DbContext>()
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="optionsBuilder"></param>
        /// <returns></returns>
        public static DbContextOptionsBuilder UseMoxImmediateAnonymization<TDbContext>(this DbContextOptionsBuilder optionsBuilder) where TDbContext : DbContext
        {
            var extensions = optionsBuilder.Options.GetExtension<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>();
            var options = extensions.ApplicationServiceProvider!.GetRequiredService<IOptions<AnonymizationOptions<TDbContext>>>();
            optionsBuilder.AddInterceptors(new AnonymizationSaveChangesInterceptor<TDbContext>(options.Value));

            return optionsBuilder;
        }

        public static void MapMoxAnonymizationRoutes(this IEndpointRouteBuilder endpoints, string moxPath = "Mox", Action<IEndpointConventionBuilder>? endpointAdded = null)
        {
            endpointAdded ??= _ => { };

            endpointAdded(endpoints.MapControllerRoute("MoxAnonymization", $"{moxPath}/DeferredAnonymization/{{Action}}".TrimStart('/'), new { Controller = "DeferredAnonymization" }));
        }
    }
}