using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.DesignDemoApp.Configuration;
using Microsoft.Extensions.Configuration;
using Mindbite.Mox.Identity;
using Mindbite.Mox.NotificationCenter;
using Microsoft.EntityFrameworkCore;

namespace Mindbite.Mox.DemoApp
{
    public class Startup
    {
        private IHostingEnvironment HostingEnvironment;

        public Startup(IHostingEnvironment hostingEnvironment)
        {
            this.HostingEnvironment = hostingEnvironment;

            var builder = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddOptions();
            services.AddMemoryCache();
            services.AddMvc()
                .AddViewLocalization();
            
            services.AddLocalization(x => x.ResourcesPath = "Resources");

            services.AddMox<AppDbContext>(this.HostingEnvironment);
            services.AddDesignDemoMoxApp(this.HostingEnvironment, this.Configuration);
            services.AddMoxNotificationCenter(this.HostingEnvironment, this.Configuration);
            services.AddMoxIdentity<AppDbContext>(this.HostingEnvironment, this.Configuration);

            services.Configure<Verification.Services.VerificationOptions>(c =>
            {
                c.Verificators.Add(new Identity.Verification.RolesCreatedVerificator("SomeTestRole"));
            });

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(this.Configuration.GetConnectionString("DefaultConnection"), x => x.MigrationsAssembly(this.HostingEnvironment.ApplicationName));
            });

            //services.AddDbContext<AppDbContext>(options =>
            //{
            //    options.UseInMemoryDatabase("DemoApp");
            //});

            services.Configure<MoxIdentityOptions>(this.Configuration.GetSection("MoxIdentityOptions"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseStatusCodePages();
            }

            var staticRoot = "/static";
            app.UseStaticFiles(staticRoot);
            app.UseMoxStaticFiles(env, staticRoot);
            app.UseDesignDemoStaticFiles(env, staticRoot);
            app.UseMoxIdentityStaticFiles(env, staticRoot);
            app.UseMoxNotificationCenterStaticFiles(env, staticRoot);

            app.UseAuthentication();

            var supportedCultures = new List<System.Globalization.CultureInfo>
            {
                new System.Globalization.CultureInfo("sv-SE"),
                new System.Globalization.CultureInfo("en-US"),
            };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("sv-SE"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            app.UseMvc(routes =>
            {
                routes.MapMoxRoutes();
                routes.MapDesignDemoRoutes();
                routes.MapMoxIdentityRoutes();
                routes.MapMoxNotificationCenterRoutes();
            });

            Verification.Startup.VerifyAsync(app.ApplicationServices).Wait();
        }
    }
}
