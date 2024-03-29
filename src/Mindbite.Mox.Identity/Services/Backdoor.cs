﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Identity.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.Services
{
    public interface IBackDoor
    {
        Task Build(string email, string password = null);
    }

    public class BackDoor : IBackDoor
    {
        private readonly Identity.Data.MoxIdentityDbContext _context;
        private readonly UserManager<MoxUser> _userManager;
        private readonly IRoleGroupManager _roleGroupManager;
        private readonly MoxIdentityOptions _options;

        public BackDoor(Mox.Services.IDbContextFetcher dbContextFetcher, UserManager<MoxUser> userManager, IRoleGroupManager roleGroupManager, IOptions<MoxIdentityOptions> options)
        {
            this._context = dbContextFetcher.FetchDbContext<Identity.Data.MoxIdentityDbContext>();
            this._userManager = userManager;
            this._roleGroupManager = roleGroupManager;
            this._options = options.Value;
        }

        public async Task Build(string email, string password = null)
        {
            var administratorGroup = await this._roleGroupManager.FindByNameAsync(this._options.AdministratorGroupName);
            if (administratorGroup == null)
            {
                throw new Exception($"Role group \"{this._options.AdministratorGroupName}\" could not be found!");
            }

            await EnsureUser(email, password, administratorGroup);

            await this._context.SaveChangesAsync();
        }

        private async Task<MoxUser> EnsureUser(string email, string password, RoleGroup group)
        {
            var user = await this._userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new MoxUserBaseImpl { UserName = email, Email = email, Name = "Back Door", IsHidden = true, RoleGroupId = group.Id };

                if(!string.IsNullOrWhiteSpace(password))
                {
                    await this._userManager.CreateAsync(user, password);
                }
                else
                {
                    await this._userManager.CreateAsync(user);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(password) && await this._userManager.HasPasswordAsync(user))
                {
                    await this._userManager.RemovePasswordAsync(user);
                }
                else if(!string.IsNullOrWhiteSpace(password) && !await this._userManager.HasPasswordAsync(user))
                {
                    await this._userManager.AddPasswordAsync(user, password);
                }
            }

            return user;
        }
    }
}
