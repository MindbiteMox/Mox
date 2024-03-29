﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
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
            var userId = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var roles = await this._rolesFetcher.GetRolesAsync(userId);

            var firstAppUrl = this._moxConfig.Apps.Select(x => x.ResolveActiveMenu(ControllerContext).Build(this.Url, roles.AsEnumerable()).FirstOrDefault()?.Url).FirstOrDefault(x => x != null);
            if(!string.IsNullOrWhiteSpace(firstAppUrl))
            {
                return Redirect(firstAppUrl);
            }

            return View(this._moxConfig);
        }

        [AllowAnonymous]
        public IActionResult Error(string errorCode)
        {
            this.ViewData["ErrorCode"] = errorCode;
            this.ViewData["IsAuthenticated"] = User?.Identity?.IsAuthenticated ?? false;

            var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var statusCode = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

            var startsWithMoxPath = (exception?.Path ?? statusCode?.OriginalPath).StartsWith($"/{this._moxConfig.Path.Trim('/')}", StringComparison.OrdinalIgnoreCase);
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