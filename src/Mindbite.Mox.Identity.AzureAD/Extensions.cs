using System;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Graph;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Mindbite.Mox.Identity.AzureAD
{
    public static class OidcConstants
    {
        public const string AdditionalClaims = "claims";
        public const string ScopeOfflineAccess = "offline_access";
        public const string ScopeProfile = "profile";
        public const string ScopeOpenId = "openid";
    }

    public static class Extensions
    {
        private static async Task<IEnumerable<object>> ToCompleteListAsync<T>(this T self, Func<T, Task<T>> next) where T : IEnumerable<object>
        {
            var list = new List<object>();
            list.AddRange(self);

            var lastResponse = self;
            var n = default(Task<T>);
            while ((n = next(lastResponse)) != null)
            {
                var result = await n;
                list.AddRange(result);
                lastResponse = result;
            }

            return list;
        }

        public static IServiceCollection AddMoxIdentityAzureADAuthentication(this IMvcBuilder mvc, IConfiguration configuration)
        {
            var thisAssembly = typeof(Extensions).Assembly;
            mvc.AddApplicationPart(thisAssembly);
            var viewsDLLName = thisAssembly.GetName().Name + ".Views.dll";
            var viewsDLLDirectory = Path.GetDirectoryName(thisAssembly.Location);
            var viewsDLLPath = Path.Combine(viewsDLLDirectory, viewsDLLName);
            if (System.IO.File.Exists(viewsDLLPath))
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

            var services = mvc.Services;

            services.AddAuthentication()
                .AddAzureAD(options => configuration.Bind("AzureAd", options));

            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, (OpenIdConnectOptions options) =>
            {
                options.Authority = options.Authority + "/v2.0/";
                options.SignInScheme = "Identity.Application";
                options.GetClaimsFromUserInfoEndpoint = true;
                options.TokenValidationParameters.ValidateIssuer = false;
                options.SaveTokens = true;
                options.ResponseType = "id_token token";
                options.TokenValidationParameters.NameClaimType = "preferred_username";

                options.Events.OnTicketReceived = async context =>
                {
                    var accessToken = context.Properties.GetTokenValue("access_token");
                    var client = new GraphServiceClient(new DelegateAuthenticationProvider(request =>
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", accessToken);
                        return Task.CompletedTask;
                    }));

                    var groups = (await (await client.Groups.Request().GetAsync()).ToCompleteListAsync(x => x.NextPageRequest?.GetAsync())).Cast<Group>();
                    var result = (await (await client.Me.GetMemberGroups(false).Request().PostAsync()).ToCompleteListAsync(x => x.NextPageRequest?.PostAsync())).Cast<string>();

                    var groupedGroups = result.Select(x => groups.First(y => y.Id == x));

                    var userEmail = context.Principal.Claims.First(x => x.Type == "preferred_username").Value;
                    var userManager = context.HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Identity.UserManager<Identity.Data.Models.MoxUser>>();
                    var signInManager = context.HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Identity.SignInManager<Identity.Data.Models.MoxUser>>();
                    var user = await userManager.FindByEmailAsync(userEmail);
                    context.Principal = await signInManager.ClaimsFactory.CreateAsync(user);

                };
            });

            return services;
        }
    }
}