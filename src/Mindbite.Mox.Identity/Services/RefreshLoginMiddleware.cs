using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Mindbite.Mox.Identity.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Mindbite.Mox.Identity.Services.RefreshLoginMiddleware
{
    public class RefreshLoginUserChanges : UserChanges
    {
        private readonly RefreshLoginService _refreshLoginService;

        public RefreshLoginUserChanges(RefreshLoginService refreshLoginService)
        {
            this._refreshLoginService = refreshLoginService;
        }

        public override Task OnUpdatedAsync(MoxUser user)
        {
            this._refreshLoginService.UserChanged(user.Id);
            return Task.CompletedTask;
        }
    }

    public class RefreshLoginService
    {
        private readonly UserManager<MoxUser> _userManager;
        private readonly SignInManager<MoxUser> _signInManager;
        private readonly MoxIdentityOptions _options;
        private readonly IServiceProvider _serviceProvider;

        private static readonly ConcurrentDictionary<string, object> _refreshedUsers = new ConcurrentDictionary<string, object>();

        public RefreshLoginService(UserManager<MoxUser> userManager, SignInManager<MoxUser> signInManager, IOptions<MoxIdentityOptions> options, IServiceProvider serviceProvider)
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._options = options.Value;
            this._serviceProvider = serviceProvider;
        }

        public async Task<bool> RefreshLoginAsync(string userId)
        {
            if (!_refreshedUsers.TryGetValue(userId, out var _))
            {
                var user = await this._userManager.FindByIdAsync(userId);
                if(user == null)
                {
                    return false;
                }

                await this._signInManager.RefreshSignInAsync(user);
                _refreshedUsers.TryAdd(userId, string.Empty);

                foreach(var hook in this.Hooks)
                {
                    await hook.OnLoginRefreshedAsync(user);
                }

                return true;
            }

            return false;
        }

        public void UserChanged(string userId)
        {
            _refreshedUsers.TryRemove(userId, out var _);
        }

        private List<UserChanges> _hooks;
        private List<UserChanges> Hooks
        {
            get
            {
                if (this._hooks == null)
                {
                    this._hooks = new List<UserChanges>();
                    foreach (var x in this._options.HookTypes.HookTypes)
                    {
                        _hooks.Add((UserChanges)_serviceProvider.GetService(x));
                    }
                }

                return _hooks;
            }
        }
    }

    public class RefreshLoginActionFilter : IAsyncActionFilter
    {
        private readonly RefreshLoginService _refreshLoginService;

        public RefreshLoginActionFilter(RefreshLoginService refreshLoginService)
        {
            this._refreshLoginService = refreshLoginService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var didRefreshLogin = false;
            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                didRefreshLogin = await this._refreshLoginService.RefreshLoginAsync(userId);
            }

            await next();

            if(didRefreshLogin && !(context.HttpContext.Response.StatusCode >= 200 && context.HttpContext.Response.StatusCode < 300))
            {
                var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                this._refreshLoginService.UserChanged(userId);
            }
        }
    }
}
