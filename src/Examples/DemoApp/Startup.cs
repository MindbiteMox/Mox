﻿using System;
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
using Mindbite.Mox.Identity.AzureAD;
using Mindbite.Mox.NotificationCenter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Mindbite.Mox.Reporting;
using Mindbite.Mox.Images;
using Mindbite.Mox.DemoApp.Areas.FormTest;

namespace Mindbite.Mox.DemoApp
{
    public class Startup
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public Startup(IWebHostEnvironment webHostEnvironment)
        {
            this._webHostEnvironment = webHostEnvironment;

            var builder = new ConfigurationBuilder()
                .SetBasePath(webHostEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{webHostEnvironment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddRouting();
            services.AddOptions();
            services.AddMemoryCache();
            services.AddSession();
            services.AddLocalization(x => x.ResourcesPath = "Resources");
            services.AddLogging(x =>
            {
                x.AddConsole();
            });

            var mvc = services.AddMvc()
                .AddViewLocalization()
                .AddMox<AppDbContext>(this._webHostEnvironment)
                .AddDesignDemoMoxApp(this._webHostEnvironment, this.Configuration)
                .AddMoxNotificationCenter(this._webHostEnvironment, this.Configuration)
                //.AddMoxIdentityAzureADAuthentication(this.Configuration)
                .AddMoxImages()
                .AddTestFormApp()
                .AddMoxReportingApp(this.Configuration)
                .AddMoxIdentity<AppDbContext>(this._webHostEnvironment, this.Configuration);

            services.Configure<Verification.Services.VerificationOptions>(c =>
            {
                c.Verificators.Add(new Identity.Verification.RolesCreatedVerificator("SomeTestRole"));
            });

            //services.AddDbContext<AppDbContext>(options =>
            //{
            //    options.UseSqlServer(this.Configuration.GetConnectionString("DefaultConnection"), x => x.MigrationsAssembly(this._webHostEnvironment.ApplicationName));
            //});

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("DemoApp");
            });

            services.Configure<MoxIdentityOptions>(this.Configuration.GetSection("MoxIdentityOptions"));
            services.Configure<MoxIdentityOptions>(config =>
            {
                //config.Groups.DisableGroupSettingsCallback = (serviceProvider, user) =>
                //{
                //    return Task.FromResult(true);
                //};

                //config.Groups.GroupSettingsMovedToThisUrl = (serviceProvider, user, url) =>
                //{
                //    var settingsOptions = serviceProvider.GetRequiredService<IOptions<SettingsOptions>>().Value;
                //    return Task.FromResult(url.Action("EditOther", new { user.Id, View = settingsOptions.AdditionalEditUserViews.First().ViewName }));
                //};
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Mox/Error/500");
                app.UseStatusCodePagesWithReExecute("/Mox/Error/{0}");
            }

            app.UseAuthentication();

            app.UseMoxStaticFileAuthorization(env);
            
            var staticRoot = "";
            app.UseStaticFiles(staticRoot);
            app.UseMoxStaticFiles(env, staticRoot);
            app.UseDesignDemoStaticFiles(env, staticRoot);
            app.UseMoxIdentityStaticFiles(env, staticRoot);
            app.UseMoxNotificationCenterStaticFiles(env, staticRoot);
            app.UseMoxImagesStaticFiles(env, staticRoot);

            app.UseRouting();
            app.UseCors();

            app.UseSession();
            app.UseAuthorization();

            var supportedCultures = new List<System.Globalization.CultureInfo>
            {
                new System.Globalization.CultureInfo("sv"),
                new System.Globalization.CultureInfo("en"),
            };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("sv"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMoxRoutes();
                endpoints.MapDesignDemoRoutes();
                endpoints.MapMoxIdentityRoutes();
                endpoints.MapTestFormAppRoutes();
                endpoints.MapMoxNotificationCenterRoutes();
                endpoints.MapMoxIdentityAzureADRoutes();
                endpoints.MapMoxReportingAppRoutes();
                endpoints.MapMoxImagesRoutes();
                endpoints.MapRedirectToMoxRoutes();
            });

            Verification.Startup.VerifyAsync(app.ApplicationServices).Wait();
        }
    }
}
