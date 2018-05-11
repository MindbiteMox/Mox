using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Configuration;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Mindbite.Mox.Areas.Mox.Components
{
    [ViewComponent(Name = "Mox/AppHeaderPartials")]
    public class AppHeaderPartialsViewComponent : ViewComponent
    {
        private readonly Config _moxConfig;
        private readonly IUserRolesFetcher _rolesFetcher;

        public AppHeaderPartialsViewComponent(IOptions<Config> moxConfig, IUserRolesFetcher rolesFetcher)
        {
            this._moxConfig = moxConfig.Value;
            this._rolesFetcher = rolesFetcher;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = await this._rolesFetcher.GetRolesAsync(userId);
            var apps = this._moxConfig.Apps.Where(x => x.CanViewWithRoles(roles));
            var headerPartials = apps.Where(x => x.HeaderPartial != null).Select(x => x.HeaderPartial).OrderBy(x => x.Position);
            return View(headerPartials);
        }
    }
}
