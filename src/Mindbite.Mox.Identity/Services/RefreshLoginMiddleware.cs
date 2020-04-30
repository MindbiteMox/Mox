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
        
        private static readonly ConcurrentDictionary<string, object> _refreshedUsers = new ConcurrentDictionary<string, object>();

        public RefreshLoginService(UserManager<MoxUser> userManager, SignInManager<MoxUser> signInManager)
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
        }

        public async Task RefreshLoginAsync(string userId)
        {
            if (!_refreshedUsers.TryGetValue(userId, out var _))
            {
                var user = await this._userManager.FindByIdAsync(userId);
                if(user == null)
                {
                    return;
                }

                await this._signInManager.RefreshSignInAsync(user);
                _refreshedUsers.TryAdd(userId, string.Empty);
            }
        }

        public void UserChanged(string userId)
        {
            _refreshedUsers.TryRemove(userId, out var _);
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
            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                await this._refreshLoginService.RefreshLoginAsync(userId);
            }

            await next();
        }
    }
}
