using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
                return Redirect(firstApp.ResolveActiveMenu(ControllerContext).Build(this.Url, roles.AsEnumerable()).First().Url);
            }

            return View(this._moxConfig);
        }

        [AllowAnonymous]
        public IActionResult Error(string errorCode)
        {
            this.ViewData["ErrorCode"] = errorCode;
            this.ViewData["IsAuthenticated"] = User?.Identity?.IsAuthenticated ?? false;

            var startsWithMoxPath = this.HttpContext.Request.Path.StartsWithSegments(new Microsoft.AspNetCore.Http.PathString($"/{this._moxConfig.Path.Trim('/')}"), StringComparison.OrdinalIgnoreCase);
            if (startsWithMoxPath)
            {
                return View(viewName: "Error");
            }
            else
            {
                return View(viewName: "Public/Error");
            }
        }
    }
}