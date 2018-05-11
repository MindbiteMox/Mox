using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Mindbite.Mox.Identity.ViewModels.AccountViewModel;
using Microsoft.Extensions.Localization;

namespace Mindbite.Mox.Identity.Controllers
{
    [AllowAnonymous]
    public class LogInController : Controller
    {
        private readonly SignInManager<Data.Models.MoxUser> _signInManager;
        private readonly IStringLocalizer _localizer;

        public LogInController(SignInManager<Data.Models.MoxUser> signInManager, IStringLocalizer localizer)
        {
            this._signInManager = signInManager;
            this._localizer = localizer;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LogInViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await this._signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }

                ModelState.AddModelError(string.Empty, this._localizer["Det gick inte logga in, kontrollera e-post och lösenord."]);
                return View(model);
            }

            return View(model);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home", new { Area = "Mox" });
            }
        }
    }
}
