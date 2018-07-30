using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mindbite.Mox.Identity;
using Mindbite.Mox.Identity.AuthorizeFilters;
using Mindbite.Mox.Identity.Data;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.Services;
using Mindbite.Mox.Utils.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mindbite.Mox.Extensions
{
    public static class IdentityExtensions
    {
        public static void UseMoxIdentity(this IApplicationBuilder app, string moxPath = "Mox")
        {
        }

        public static void AddMoxIdentity<AppDbContext_T>(this IServiceCollection services, IHostingEnvironment hostingEnvironment, IConfigurationRoot appConfiguration, string moxPath = "Mox", string staticRequestPath = "/static") where AppDbContext_T : MoxIdentityDbContext, IDbContext
        {
            services.AddIdentity<MoxUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext_T>()
                .AddDefaultTokenProviders()
                .AddUserManager<MoxUserManager>();

            services.Configure<MvcOptions>(options => {
                var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .RequireRole(Constants.MoxRole)
                     .Build();
                options.Filters.Add(new MoxAuthorizeFilter(policy, moxPath));
            });

            services.Configure<LocalizationSources>(options =>
            {
                options.ResouceTypes.Add(typeof(Localization));
            });

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = false;

                // User settings
                options.User.RequireUniqueEmail = true;
            });

            services.ConfigureApplicationCookie(options => {
                options.Cookie.Name = "MoxAuthCookie";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.Expiration = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(150);
                options.LoginPath = $"{moxPath}/LogIn".TrimStart('/').Insert(0, "/");
                options.LogoutPath = $"{moxPath}/LogOut".TrimStart('/').Insert(0, "/");
            });

            services.Configure<RazorViewEngineOptions>(c =>
            {
                c.FileProviders.Add(new EmbeddedFilesInAssemblyFileProvider(typeof(IdentityExtensions).GetTypeInfo().Assembly, hostingEnvironment));
            });

            services.Configure<Mindbite.Mox.Configuration.Config>(c =>
            {
                var settingsApp = c.Apps.FirstOrDefault(x => x.AppId == "MoxSettings");

                if (settingsApp == null)
                {
                    settingsApp = c.Apps.Add("Inställningar", "MoxSettings");
                }
                settingsApp.Areas.Add(Constants.SettingsArea);
                settingsApp.Menu.Items(items =>
                {
                    items.Add()
                        .Title("Användare")
                        .Items(identityItems =>
                        {
                            identityItems.Add()
                                .Title("Mitt konto")
                                .Role(Constants.EditMyOwnAccountRole)
                                .AreaAction(Constants.SettingsArea, "MyAccount", "Edit");
                            identityItems.Add()
                                .Title("Inloggningskonton")
                                .Role(Constants.AdminRole)
                                .AreaAction(Constants.SettingsArea, "UserManagement");
                            identityItems.Add()
                                .Title("Behörigheter")
                                .Role(Constants.AdminRole)
                                .AreaAction(Constants.SettingsArea, "RoleManagement");
                        });
                });

                var identityApp = c.Apps.Add("Inloggning", "MoxIdentity");
                identityApp.HeaderPartial = new Configuration.Apps.AppPartial() { Name = "Mox/Identity/_Header", Position = 1000 };
            });

            services.Configure<Configuration.StaticIncludes.IncludeConfig>(c =>
            {
                c.StaticRoot = staticRequestPath;
                c.Styles.Add(new Configuration.StaticIncludes.Style("identity/css/mox_base.css"));
                c.Styles.Add(new Configuration.StaticIncludes.Style("identity/css/mox_base_mobile.css", maxWidth = 960));
            });

            services.Configure<SettingsOptions>(c => { });

            var userRolesFetcher = services.FirstOrDefault(x => x.ServiceType == typeof(IUserRolesFetcher));
            if (userRolesFetcher != null)
            {
                services.Remove(userRolesFetcher);
            }

            services.AddScoped<IUserRolesFetcher, Identity.Services.UserRolesFetcher>();
            services.AddScoped<Identity.Services.IPasswordResetManager, Identity.Services.PasswordResetManager>();

            services.AddTransient<Identity.Services.IBackDoor, Identity.Services.BackDoor>();

            services.Configure<Verification.Services.VerificationOptions>(c =>
            {
                c.Verificators.Add(new Identity.Verification.BackDoorVerificator());
                c.Verificators.Add(new Identity.Verification.EmailConfigSetVerificator());
                c.Verificators.Add(new Identity.Verification.RolesCreatedVerificator(Constants.MoxRole));
                c.Verificators.Add(new Identity.Verification.RolesCreatedVerificator(Constants.AdminRole));
                c.Verificators.Add(new Identity.Verification.RolesCreatedVerificator(Constants.EditMyOwnAccountRole));
            });
        }

        public static void MapMoxIdentityRoutes(this IRouteBuilder routes, string moxPath = "Mox")
        {
            routes.MapRoute("IdentityLogIn", $"{moxPath}/LogIn/{{action=Index}}".TrimStart('/'), new { Controller = "LogIn" });
            routes.MapRoute("IdentityLogOut", $"{moxPath}/LogOut/{{action=Index}}".TrimStart('/'), new { Controller = "LogOut" });
            routes.MapRoute("IdentityForgotPassword", $"{moxPath}/Forgot/{{action=Index}}".TrimStart('/'), new { Controller = "ForgotPassword" });
            routes.MapAreaRoute("Identity", Constants.SettingsArea, $"{moxPath}/Settings/Identity/{{controller=MyAccount}}/{{action=Index}}/{{id?}}".TrimStart('/'));
        }

        public static void UseMoxIdentityStaticFiles(this IApplicationBuilder app, IHostingEnvironment hostingEnvironment, string requestPath = "/static")
        {
            app.AddStaticFileFileProvider(typeof(IdentityExtensions), hostingEnvironment, requestPath);
        }
    }
}
