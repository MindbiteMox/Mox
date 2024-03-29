﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.Identity.Services.RefreshLoginMiddleware;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace Mindbite.Mox.Identity.Services
{
    public interface IRoleGroupManager
    {
        IQueryable<RoleGroup> RoleGroups { get; }

        Task AssignToGroupAsync(MoxUser user, RoleGroup group);
        Task CreateAsync(RoleGroup group, IEnumerable<string> roles);
        Task<RoleGroup?> FindByIdAsync(int id);
        Task<RoleGroup?> FindByNameAsync(string name);
        Task UpdateAsync(RoleGroup group, IEnumerable<string> roles);
    }

    public class RoleGroupManager : IRoleGroupManager
    {
        private readonly Data.MoxIdentityDbContext _context;
        private readonly UserManager<MoxUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserRolesFetcher _rolesFetcher;
        private readonly RefreshLoginService _refreshLoginService;
        private readonly IUserStore<MoxUser> _userStore;
        private readonly MoxIdentityOptions _options;

        public IQueryable<RoleGroup> RoleGroups => this._context.RoleGroups.Include(x => x.Roles);

        public RoleGroupManager(IDbContextFetcher dbContextFetcher, IUserStore<MoxUser> userStore, UserManager<MoxUser> userManager, RoleManager<IdentityRole> roleManager, IUserRolesFetcher rolesFetcher, RefreshLoginService refreshLoginService, IOptions<MoxIdentityOptions> options)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.MoxIdentityDbContext>();
            this._userStore = userStore;
            this._userManager = userManager;
            this._roleManager = roleManager;
            this._rolesFetcher = rolesFetcher;
            this._refreshLoginService = refreshLoginService;
            this._options = options.Value;
        }

        public async Task AssignToGroupAsync(MoxUser user, RoleGroup group)
        {
            if (this._options.HandleUserRolesManually)
            {
                return;
            }

            var roles = await this._userManager.GetRolesAsync(user);
            if (!roles.OrderBy(x => x).SequenceEqual(group.Roles.Select(x => x.Role).OrderBy(x => x)))
            {
                var roleStore = (IUserRoleStore<MoxUser>)this._userStore;

                foreach (var role in roles)
                {
                    var normalizedRole = this._userManager.NormalizeName(role);
                    await roleStore.RemoveFromRoleAsync(user, normalizedRole, default);
                }

                foreach (var role in group.Roles.Select(x => x.Role))
                {
                    var normalizedRole = this._userManager.NormalizeName(role);
                    await roleStore.AddToRoleAsync(user, normalizedRole, default);
                }

                (this._rolesFetcher as UserRolesFetcher)?.ClearCache(user.Id);
                this._refreshLoginService.UserChanged(user.Id);

                await this._context.SaveChangesAsync();
            }
        }

        public async Task<RoleGroup?> FindByIdAsync(int id)
        {
            return await RoleGroups.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<RoleGroup?> FindByNameAsync(string name)
        {
            return await RoleGroups.Include(x => x.Roles).FirstOrDefaultAsync(x => x.GroupName == name);
        }

        public async Task CreateAsync(RoleGroup group, IEnumerable<string> roles)
        {
            this._context.Add(group);
            await this._context.SaveChangesAsync();

            await UpdateAsync(group, roles);
        }

        public async Task UpdateAsync(RoleGroup group, IEnumerable<string> roles)
        {
            var identityRoles = await this._roleManager.Roles.Where(x => roles.Contains(x.Name)).ToListAsync();
            var existingRoles = await this._context.RoleGroupRoles.Where(x => x.RoleGroupId == group.Id).ToListAsync();

            foreach (var role in identityRoles.Select(x => x.Name).Concat(existingRoles.Select(x => x.Role)))
            {
                var existingRole = existingRoles.FirstOrDefault(x => x.Role == role);
                var keepRole = identityRoles.Any(x => x.Name == role);

                if (existingRole == null && keepRole)
                {
                    this._context.Add(new RoleGroupRole
                    {
                        Role = role,
                        RoleGroupId = group.Id,
                    });
                }
                else if (existingRole != null && !keepRole)
                {
                    this._context.Remove(existingRole);
                }
            }

            await this._context.SaveChangesAsync();

            var users = await this._context.Users.Where(x => x.RoleGroupId == group.Id).ToListAsync();
            foreach (var user in users)
            {
                await this.AssignToGroupAsync(user, group);
            }
        }
    }
}
