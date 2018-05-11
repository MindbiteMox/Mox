using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mindbite.Mox.Identity.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.Services
{
    public interface IBackDoor
    {
        Task Build(string email, string password);

        Task<IdentityResult> CreateRole(string role);
    }

    public class BackDoor : IBackDoor
    {
        private readonly Identity.Data.MoxIdentityDbContext _context;
        private readonly UserManager<MoxUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public BackDoor(Mox.Services.IDbContextFetcher dbContextFetcher, UserManager<MoxUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this._context = dbContextFetcher.FetchDbContext<Identity.Data.MoxIdentityDbContext>();
            this._userManager = userManager;
            this._roleManager = roleManager;
        }

        public async Task Build(string email, string password)
        {
            var adminID = await EnsureUser(password, email);
            await this.EnsureRole(adminID, Constants.MoxRole);
            await this.EnsureRole(adminID, Constants.AdminRole);
            await this.EnsureRole(adminID, Constants.EditMyOwnAccountRole);

            await this._context.SaveChangesAsync();
        }

        public async Task<IdentityResult> CreateRole(string role)
        {
            if (!await this._roleManager.RoleExistsAsync(role))
            {
                return await this._roleManager.CreateAsync(new IdentityRole(role));
            }

            return IdentityResult.Success;
        }

        private async Task<string> EnsureUser(string testUserPw, string UserName)
        {
            var user = await this._userManager.FindByNameAsync(UserName);
            if (user == null)
            {
                user = new MoxUserBaseImpl { UserName = UserName, Email = UserName, Name = "Back Door" };
                await this._userManager.CreateAsync(user, testUserPw);
            }

            return user.Id;
        }

        private async Task<IdentityResult> EnsureRole(string uid, string role)
        {
            var createRoleResult = await this.CreateRole(role);

            if (!createRoleResult.Succeeded)
                return createRoleResult;

            var user = await this._userManager.FindByIdAsync(uid);
            return await this._userManager.AddToRoleAsync(user, role);
        }
    }
}
