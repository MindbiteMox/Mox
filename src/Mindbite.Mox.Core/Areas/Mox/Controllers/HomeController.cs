using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Configuration;
using Mindbite.Mox.Extensions;

namespace Mindbite.Mox.Controllers
{
    [Area("Mox")]
    public class HomeController : Controller
    {
        private readonly Config _moxConfig;
        private readonly Services.IUserRolesFetcher _rolesFetcher;

        public HomeController(IOptions<Config> moxConfig, Services.IUserRolesFetcher rolesFetcher)
        {
            this._moxConfig = moxConfig.Value;
            this._rolesFetcher = rolesFetcher;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var firstApp = this._moxConfig.Apps.FirstOrDefault();
            if (firstApp != null)
            {
                var userId = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var roles = await this._rolesFetcher.GetRolesAsync(userId);
                return Redirect(firstApp.Menu.Build(this.Url, roles.AsEnumerable()).First().Url);
            }

            return View(this._moxConfig);
        }
    }
}