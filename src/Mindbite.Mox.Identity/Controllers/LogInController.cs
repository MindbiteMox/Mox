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
        private readonly UserManager<Data.Models.MoxUser> _userManager;
        private readonly IStringLocalizer _localizer;
        private readonly Services.IMagicLinkManager _magicLinkManager;

        public LogInController(SignInManager<Data.Models.MoxUser> signInManager, UserManager<Data.Models.MoxUser> userManager, IStringLocalizer localizer, Services.IMagicLinkManager magicLinkManager)
        {
            this._signInManager = signInManager;
            this._userManager = userManager;
            this._localizer = localizer;
            this._magicLinkManager = magicLinkManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(LogInViewModel model, string returnUrl = null)
        {
            if(ModelState.IsValid)
            {
                return RedirectToAction("PasswordOrMagicLink", new PasswordOrMagicLinkViewModel
                {
                    Email = model.Email,
                    RememberMe = model.RememberMe,
                    ReturnUrl = returnUrl
                });
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult PasswordOrMagicLink(PasswordOrMagicLinkViewModel model)
        {
            if(model == null || string.IsNullOrWhiteSpace(model.Email))
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMagicLink(PasswordOrMagicLinkViewModel model)
        {
            var user = await this._userManager.FindByEmailAsync(model.Email);
            var (success, error) = await this._magicLinkManager.GenerateAndSendMagicLinkAsync(ControllerContext, user, model.ReturnUrl);

            if(success)
            {
                return RedirectToAction("MagicLinkSent");
            }

            ModelState.AddModelError(string.Empty, this._localizer[Utils.Utils.DisplayName(error)]);
            return View("PasswordOrMagicLink", model);
        }

        [HttpGet]
        public IActionResult MagicLinkSent()
        {
            return View();
        }

        [HttpGet]
        public IActionResult PasswordLogin(string email, bool rememberMe)
        {
            return View(new PasswordViewModel
            {
                Email = email,
                RememberMe = rememberMe
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PasswordLogIn(PasswordViewModel model, string returnUrl = null)
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

        [HttpGet]
        public async Task<IActionResult> MagicLinkLogIn(MagicLinkEmailViewModel model)
        {
            var (isValid, user) = await this._magicLinkManager.ValidateMagicTokenAsync(model.MagicToken);

            if(!isValid)
            {
                return RedirectToAction("Index");
            }

            await this._signInManager.SignInAsync(user, model.RememberMe);

            return RedirectToLocal(model.ReturnUrl);
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
