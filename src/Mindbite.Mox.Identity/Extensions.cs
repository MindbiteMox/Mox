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
using Mindbite.Mox.Identity;
using Mindbite.Mox.Identity.AuthorizeFilters;
using Mindbite.Mox.Identity.Data;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.Identity.Services;
using Mindbite.Mox.Services;
using Mindbite.Mox.Utils.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
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

        public static IMvcBuilder AddMoxIdentity<AppDbContext_T>(this IMvcBuilder mvc, IWebHostEnvironment webHostEnvironment, IConfigurationRoot appConfiguration, string moxPath = "Mox", string staticRequestPath = "") where AppDbContext_T : MoxIdentityDbContext, IDbContext
        {
            return AddMoxIdentity<AppDbContext_T, MoxUserManager>(mvc, webHostEnvironment, appConfiguration, moxPath, staticRequestPath);
        }

        public static IMvcBuilder AddMoxIdentity<AppDbContext_T, UserManager_T>(this IMvcBuilder mvc, IWebHostEnvironment webHostEnvironment, IConfigurationRoot appConfiguration, string moxPath = "Mox", string staticRequestPath = "") where AppDbContext_T : MoxIdentityDbContext, IDbContext where UserManager_T : MoxUserManager
        {
            var thisAssembly = typeof(IdentityExtensions).Assembly;
            mvc.AddApplicationPart(thisAssembly);
            var viewsDLLName = thisAssembly.GetName().Name + ".Views.dll";
            var viewsDLLDirectory = Path.GetDirectoryName(thisAssembly.Location);
            var viewsDLLPath = Path.Combine(viewsDLLDirectory, viewsDLLName);
            if(File.Exists(viewsDLLPath)){
                var viewAssembly = Assembly.LoadFile(viewsDLLPath);
                var viewAssemblyPart = new CompiledRazorAssemblyPart(viewAssembly);
                mvc.ConfigureApplicationPartManager(manager => manager.ApplicationParts.Add(viewAssemblyPart));
            } else {
                var viewAssemblyPart = new CompiledRazorAssemblyPart(thisAssembly);
                mvc.ConfigureApplicationPartManager(manager => manager.ApplicationParts.Add(viewAssemblyPart));
            }

            mvc.Services.AddIdentity<MoxUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext_T>()
                .AddDefaultTokenProviders()
                .AddUserManager<UserManager_T>()
                .AddSignInManager<MoxSignInManager>()
                .AddUserStore<MoxUserStore<AppDbContext_T>>();

            mvc.Services.Configure<MvcOptions>(options => {
                var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     //.RequireRole(Configuration.Constants.MoxRole)
                     .Build();
                options.Filters.Add(new MoxAuthorizeFilter(policy, moxPath));
                options.Filters.Add<Identity.Services.RefreshLoginMiddleware.RefreshLoginActionFilter>();
            });

            mvc.Services.Configure<LocalizationSources>(options =>
            {
                options.ResouceTypes.Add(typeof(Localization));
            });

            mvc.Services.Configure<IdentityOptions>(options =>
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

            mvc.Services.ConfigureApplicationCookie(options => {
                options.Cookie.Name = "MoxAuthCookie";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.LoginPath = $"{moxPath}/LogIn".TrimStart('/').Insert(0, "/");
                options.LogoutPath = $"{moxPath}/LogOut".TrimStart('/').Insert(0, "/");
            });

            mvc.Services.Configure<RazorViewEngineOptions>(c =>
            {
                //c.FileProviders.Add(new EmbeddedFilesInAssemblyFileProvider(typeof(IdentityExtensions).GetTypeInfo().Assembly, hostingEnvironment));
            });

            mvc.Services.Configure<Configuration.Config>(c =>
            {
                var settingsApp = c.Apps.FirstOrDefault(x => x.AppId == Constants.SettingsAppId);

                if (settingsApp == null)
                {
                    settingsApp = c.Apps.Add(Constants.SettingsAppName, Constants.SettingsAppId);
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
                                //.Role(Constants.EditMyOwnAccountRole)
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

            mvc.Services.Configure<Configuration.StaticIncludes.IncludeConfig>(c =>
            {
                c.StaticRoot = staticRequestPath;
                c.Files.Add(Configuration.StaticIncludes.StaticFile.Style("mox/static/identity/css/mox_base.css"));
                c.Files.Add(Configuration.StaticIncludes.StaticFile.Style("mox/static/identity/css/mox_base_mobile.css", maxWidth: 960));
            });

            mvc.Services.Configure<SettingsOptions>(c => { });

            mvc.Services.Configure<MoxIdentityOptions>(x =>
            {
                x.HookTypes.Add<Identity.Services.RefreshLoginMiddleware.RefreshLoginUserChanges>();

                x.AdditionalAllowedStaticFileLocations.Add("/Mox/Static/Fonts/Inter");

                x.LoginStaticFiles.Add(Configuration.StaticIncludes.StaticFile.Style("mox/static/fonts/inter/inter.css"));
                x.LoginStaticFiles.Add(Configuration.StaticIncludes.StaticFile.Style("mox/static/identity/login/css/base.css"));
                x.LoginStaticFiles.Add(Configuration.StaticIncludes.StaticFile.Style("mox/static/identity/login/css/base_mobile.css", maxWidth: 960));
            });

            var userRolesFetcher = mvc.Services.FirstOrDefault(x => x.ServiceType == typeof(IUserRolesFetcher));
            if (userRolesFetcher != null)
            {
                mvc.Services.Remove(userRolesFetcher);
            }

            mvc.Services.AddScoped<IUserRolesFetcher, UserRolesFetcher>();
            mvc.Services.AddScoped<IPasswordResetManager, PasswordResetManager>();
            mvc.Services.AddScoped<IMagicLinkManager, MagicLinkManager>();
            mvc.Services.AddScoped<Identity.Services.RefreshLoginMiddleware.RefreshLoginUserChanges>();
            mvc.Services.AddScoped<Identity.Services.RefreshLoginMiddleware.RefreshLoginService>();
            mvc.Services.AddScoped<Identity.Services.RoleGroupManager>();

            mvc.Services.AddTransient<IBackDoor, BackDoor>();

            mvc.Services.Configure<Verification.Services.VerificationOptions>(c =>
            {
                c.Verificators.Add(new Identity.Verification.RolesCreatedVerificator(Constants.AdminRole));
                c.Verificators.Add(new Identity.Verification.AdminRoleGroupCreatedVerificator());
                c.Verificators.Add(new Identity.Verification.BackDoorVerificator());
                c.Verificators.Add(new Identity.Verification.EmailConfigSetVerificator());
            });

            return mvc;
        }

        public static void MapMoxIdentityRoutes(this IEndpointRouteBuilder endpoints, string moxPath = "Mox")
        {
            endpoints.MapControllerRoute("IdentityLogIn", $"{moxPath}/LogIn/{{action=Index}}".TrimStart('/'), new { Controller = "LogIn" });
            endpoints.MapControllerRoute("IdentityLogOut", $"{moxPath}/LogOut/{{action=Index}}".TrimStart('/'), new { Controller = "LogOut" });
            endpoints.MapControllerRoute("IdentityForgotPassword", $"{moxPath}/Forgot/{{action=Index}}".TrimStart('/'), new { Controller = "ForgotPassword" });
            endpoints.MapAreaControllerRoute("Identity", Constants.SettingsArea, $"{moxPath}/Settings/Identity/{{controller=MyAccount}}/{{action=Index}}/{{id?}}".TrimStart('/'));
        }

        public static void UseMoxIdentityStaticFiles(this IApplicationBuilder app, IWebHostEnvironment webHostEnvironment, string requestPath = "")
        {
            app.AddStaticFileFileProvider(typeof(IdentityExtensions), webHostEnvironment, requestPath);
        }

        public static void UseMoxStaticFileAuthorization(this IApplicationBuilder app, IWebHostEnvironment webHostEnvironment)
        {
            if(app.ApplicationServices.GetService<Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware>() != null)
            {
                throw new Exception("Put me before UseStaticFiles(), after UseAuthentication()");
            }

            var config = app.ApplicationServices.GetRequiredService<IOptions<MoxIdentityOptions>>().Value;
            var staticFilesOptions = app.ApplicationServices.GetRequiredService<IOptions<StaticFileOptions>>().Value;

            app.Use(async (context, next) =>
            {
                var filePath = System.Web.HttpUtility.UrlDecode(context.Request.Path);

                if (context.User.Identity.IsAuthenticated)
                {
                    goto Success;
                }

                var allowed = new[]
                {
                    $"{(staticFilesOptions.RequestPath.ToString().Trim('/') == string.Empty ? "/" : $"/{staticFilesOptions.RequestPath.ToString().Trim('/')}/")}public", // TODO: cleanup
                    $"{(staticFilesOptions.RequestPath.ToString().Trim('/') == string.Empty ? "/" : $"/{staticFilesOptions.RequestPath.ToString().Trim('/')}/")}mox/static/identity/login", // TODO: cleanup
                    $"{(staticFilesOptions.RequestPath.ToString().Trim('/') == string.Empty ? "/" : $"/{staticFilesOptions.RequestPath.ToString().Trim('/')}/")}.well-known" // TODO: cleanup
                }.Concat(config.AdditionalAllowedStaticFileLocations);

                if (allowed.Any(x => context.Request.Path.StartsWithSegments(x)))
                {
                    goto Success;
                }

                var staticFileProviders = context.RequestServices.GetService<IOptions<Configuration.StaticIncludes.StaticFileProviderOptions>>().Value.FileProviders;
                staticFileProviders = staticFileProviders.Append(context.RequestServices.GetRequiredService<IOptions<StaticFileOptions>>().Value.FileProvider ?? webHostEnvironment.WebRootFileProvider).ToList();

                var fileInfo = staticFileProviders.Select(x => x.GetFileInfo(filePath)).FirstOrDefault(x => x.Exists);
                if (fileInfo?.Exists ?? false)
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                Success:
                await next();
            });
        }

        public static (string name, IEnumerable<string> groups) SplitIntoLocalizedGroups(this IdentityRole role, IStringLocalizer localizer)
        {
            var group = role.SplitIntoGroups();
            return (localizer[$"role_{group.name}"].ToString(), group.groups.Select(x => localizer[$"rolegroup_{x}"].ToString()));
        }

        public static (string name, IEnumerable<string> groups) SplitIntoGroups(this IdentityRole role)
        {
            var parts = role.Name.Split('/');
            var name = parts.Last();
            var groups = parts.SkipLast(1);

            return (name, groups);
        }

        public class RoleTreeNode
        {
            public int Depth { get; set; }
            public bool IsLeaf { get; set; }
            public string DisplayName { get; set; }
            public string RoleName { get; set; }
            public IEnumerable<RoleTreeNode> Children { get; set; }
        }

        public static IEnumerable<RoleTreeNode> BuildLocalizedTree(this IEnumerable<IdentityRole> roles, IStringLocalizer localizer)
        {
            IEnumerable<RoleTreeNode> buildNodes(IEnumerable<(IdentityRole role, string name, IEnumerable<string> groups)> groups, string parentName = null, int depth = 0)
            {
                foreach(var group in groups.GroupBy(x => x.groups.FirstOrDefault()))
                {
                    var isLeaf = group.Key == null;

                    if(isLeaf)
                    {
                        foreach (var x in group)
                        {
                            yield return new RoleTreeNode
                            {
                                Depth = depth,
                                IsLeaf = true,
                                RoleName = x.role.Name,
                                DisplayName = localizer[$"role_{x.name}"].ToString(),
                                Children = Enumerable.Empty<RoleTreeNode>()
                            };
                        }
                    }
                    else
                    {
                        var roleName = parentName != null ? $"{parentName}/{group.Key}" : group.Key;
                        yield return new RoleTreeNode
                        {
                            Depth = depth,
                            IsLeaf = false,
                            DisplayName = localizer[$"rolegroup_{group.Key}"].ToString(),
                            RoleName = roleName,
                            Children = buildNodes(group.Select(x => (x.role, x.name, x.groups.Skip(1))), roleName, depth + 1).OrderBy(x => !x.IsLeaf).ThenBy(x => x.DisplayName)
                        };
                    }
                }
            }

            var groupedRoles = roles.Select(x => (role: x, group: x.SplitIntoGroups())).Select(x => (x.role, x.group.name, x.group.groups));
            return buildNodes(groupedRoles).OrderBy(x => !x.IsLeaf).ThenBy(x => x.DisplayName);
        }

        public static IEnumerable<RoleTreeNode> Flatten(this IEnumerable<RoleTreeNode> tree)
        {
            foreach(var node in tree)
            {
                yield return node;
                foreach(var child in Flatten(node.Children))
                {
                    yield return child;
                }
            }
        }
    }
}
