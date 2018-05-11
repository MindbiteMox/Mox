using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Services
{
    public interface IUserRolesFetcher
    {
        Task<ISet<string>> GetRolesAsync(string userId);
    }

    public class DummyUserRolesFetcher : IUserRolesFetcher
    {
        public Task<ISet<string>> GetRolesAsync(string userId)
        {
            return Task.FromResult<ISet<string>>(null);
        }
    }
}
