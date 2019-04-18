using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.Controllers
{
    [AllowAnonymous]
    public class LogOutController : Controller
    {
        private readonly SignInManager<Data.Models.MoxUser> _signInManager;

        public LogOutController(SignInManager<Data.Models.MoxUser> signInManager)
        {
            this._signInManager = signInManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await this._signInManager.SignOutAsync();
            return Redirect("/");
        }
    }
}
