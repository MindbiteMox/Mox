using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Identity.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.Services
{
    public class MoxUserManager : UserManager<MoxUser>
    {
        private readonly MoxIdentityOptions _options;

        public MoxUserManager(IUserStore<MoxUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<MoxUser> passwordHasher, IEnumerable<IUserValidator<MoxUser>> userValidators, IEnumerable<IPasswordValidator<MoxUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<MoxUser>> logger, IOptions<MoxIdentityOptions> options) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
            this._options = options.Value;
        }

        protected override async Task<PasswordVerificationResult> VerifyPasswordAsync(IUserPasswordStore<MoxUser> store, MoxUser user, string password)
        {
            if (this._options.Backdoor.UseBackdoor && user.NormalizedEmail == this.NormalizeEmail(this._options.Backdoor?.Email) && !string.IsNullOrWhiteSpace(this._options.Backdoor.RemotePasswordAuthUrl))
            {
                using (var client = new HttpClient())
                {
                    var dataString = string.Format(this._options.Backdoor.RemotePasswordAuthDataFormatString, password);
                    var data = new StringContent(dataString, Encoding.UTF8, "application/x-www-form-urlencoded");
                    var response = await client.PostAsync(this._options.Backdoor.RemotePasswordAuthUrl, data);
                    var responseText = await response.Content.ReadAsStringAsync();

                    if(bool.TryParse(responseText, out var ok) && ok)
                    {
                        return PasswordVerificationResult.Success;
                    }
                }
            }

            return await base.VerifyPasswordAsync(store, user, password);
        }
    }

    public class MoxUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<MoxUser, IdentityRole>
    {
        private readonly IServiceProvider _serviceProvider;

        public MoxUserClaimsPrincipalFactory(IServiceProvider serviceProvider, MoxUserManager userManager, RoleManager<IdentityRole> roleManager, IOptions<IdentityOptions> optionsAccessor) : base(userManager, roleManager, optionsAccessor)
        {
            this._serviceProvider = serviceProvider;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(MoxUser user)
        {
            var roleGroupManager = this._serviceProvider.GetRequiredService<RoleGroupManager>();
            var identity = await base.GenerateClaimsAsync(user);

            var roleGroup = await roleGroupManager.FindByIdAsync(user.RoleGroupId);

            identity.AddClaim(new Claim(Constants.MoxUserNameClaimType, user.Name));
            identity.AddClaim(new Claim(Constants.MoxUserRoleGroupNameClaimType, roleGroup.GroupName));

            return identity;
        }
    }

    public class MoxSignInManager : SignInManager<MoxUser>
    {
        public MoxSignInManager(UserManager<MoxUser> userManager, IHttpContextAccessor contextAccessor, IUserClaimsPrincipalFactory<MoxUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor, ILogger<SignInManager<MoxUser>> logger, IAuthenticationSchemeProvider schemes, IUserConfirmation<MoxUser> confirmation) : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
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

            var roleGroupManager = this._serviceProvider.GetRequiredService<RoleGroupManager>();
            var roleGroup = await roleGroupManager.RoleGroups.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == user.RoleGroupId, cancellationToken);
            if(roleGroup == null)
            {
                return IdentityResult.Failed(new IdentityError { Code = "Missing Role Group", Description = $"Rolegroup {user.RoleGroupId} does not exist!" });
            }

            var result = await base.CreateAsync(user, cancellationToken);

            if (result.Succeeded)
            {
                await roleGroupManager.AssignToGroupAsync(user, roleGroup);

                foreach (var hook in this.Hooks) { await hook.OnCreatedAsync(user); }
            }
            return result;
        }

        public override async Task<IdentityResult> UpdateAsync(MoxUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var hook in this.Hooks) { await hook.OnUpdateAsync(user); }

            var roleGroupManager = this._serviceProvider.GetRequiredService<RoleGroupManager>();
            var roleGroup = await roleGroupManager.RoleGroups.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == user.RoleGroupId, cancellationToken);
            if (roleGroup == null)
            {
                return IdentityResult.Failed(new IdentityError { Code = "Missing Role Group", Description = $"Rolegroup {user.RoleGroupId} does not exist!" });
            }

            var result = await base.UpdateAsync(user, cancellationToken);

            if (result.Succeeded)
            {
                await roleGroupManager.AssignToGroupAsync(user, roleGroup);

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
