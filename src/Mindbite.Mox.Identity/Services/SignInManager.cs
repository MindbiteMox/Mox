using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Identity.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.Services
{
    public class MoxSignInManager : SignInManager<MoxUser>
    {
        public MoxSignInManager(UserManager<MoxUser> userManager, IHttpContextAccessor contextAccessor, IUserClaimsPrincipalFactory<MoxUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor, ILogger<SignInManager<MoxUser>> logger, IAuthenticationSchemeProvider schemes) : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes)
        {
        }

        public override Task SignInAsync(MoxUser user, AuthenticationProperties authenticationProperties, string authenticationMethod = null)
        {
            if (user != null && !user.IsDeleted)
            {
                return base.SignInAsync(user, authenticationProperties, authenticationMethod);
            }

            return Task.CompletedTask;
        }
    }

    public class MoxUserStore<AppDbContext_T> : UserStore<MoxUser, IdentityRole, AppDbContext_T> where AppDbContext_T : DbContext
    {
        private readonly MoxIdentityOptions _options;
        private readonly IServiceProvider _serviceProvider;

        public MoxUserStore(AppDbContext_T context, IOptions<MoxIdentityOptions> options, IServiceProvider serviceProvider) : base(context)
        {
            this._options = options.Value;
            this._serviceProvider = serviceProvider;
        }

        public override IQueryable<MoxUser> Users => base.Users.Where(x => !x.IsDeleted);

        public override async Task<MoxUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var user = await base.FindByIdAsync(userId, cancellationToken);

            if (user != null && !user.IsDeleted)
            {
                return user;
            }
            else
            {
                return null;
            }
        }

        public override async Task<IdentityResult> DeleteAsync(MoxUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var hook in this.Hooks) { await hook.OnDeleteAsync(user); }

            user.IsDeleted = true;
            user.Email = new string($"DEL_{user.Email}_{user.Id}".Take(250).ToArray());
            user.UserName = new string($"DEL_{user.UserName}_{user.Id}".Take(250).ToArray());
            user.NormalizedEmail = user.Id;
            user.NormalizedUserName = user.Id;
            var result = await this.UpdateAsync(user, cancellationToken);

            if (result.Succeeded)
            {
                foreach (var hook in this.Hooks) { await hook.OnDeletedAsync(user); }
            }

            return result;
        }

        public override async Task<IdentityResult> CreateAsync(MoxUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var hook in this.Hooks) { await hook.OnCreateAsync(user); }

            var result = await base.CreateAsync(user, cancellationToken);

            if (result.Succeeded)
            {
                foreach (var hook in this.Hooks) { await hook.OnCreatedAsync(user); }
            }
            return result;
        }

        public override async Task<IdentityResult> UpdateAsync(MoxUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var hook in this.Hooks) { await hook.OnUpdateAsync(user); }

            var result = await base.UpdateAsync(user, cancellationToken);

            if (result.Succeeded)
            {
                foreach (var hook in this.Hooks) { await hook.OnUpdatedAsync(user); }
            }
            return result;
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
}
