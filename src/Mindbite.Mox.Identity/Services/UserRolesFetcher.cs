using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Mindbite.Mox.Identity.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.Services
{
    public class UserRolesFetcher : Mindbite.Mox.Services.IUserRolesFetcher
    {
        public static string CacheKey(string userId) => $"Mox.Services.UserRolesFetcher.UserId.{userId}";

        private UserManager<MoxUser> _userManager;
        private ISet<string> _roles;
        private IMemoryCache _cache;

        public UserRolesFetcher(UserManager<MoxUser> userManager, IMemoryCache cache)
        {
            this._userManager = userManager;
            this._cache = cache;
        }

        public async Task<ISet<string>> GetRolesAsync(string userId)
        {
            if (this._roles != null)
                return _roles;

            var cacheKey = UserRolesFetcher.CacheKey(userId);
            if (!this._cache.TryGetValue(cacheKey, out this._roles))
            {
                var user = await this._userManager.FindByIdAsync(userId);
                this._roles = new HashSet<string>(await this._userManager.GetRolesAsync(user));

                var cacheEntryOptions = new MemoryCacheEntryOptions();
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(5));

                this._cache.Set(cacheKey, this._roles, cacheEntryOptions);
            }
                
            return _roles;
        }

        public void ClearCache(string userId)
        {
            this._cache.Remove(UserRolesFetcher.CacheKey(userId));
        }
    }
}
