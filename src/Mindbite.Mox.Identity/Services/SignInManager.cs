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
        public MoxUserStore(AppDbContext_T context) : base(context) { }

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
    }
}
