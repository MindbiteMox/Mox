using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Extensions;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.Identity.ViewModels;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.Controllers
{
    [Area(Constants.SettingsArea)]
    public class MyAccountController : Controller
    {
        private readonly Data.MoxIdentityDbContext _context;
        private readonly UserManager<MoxUser> _userManager;
        private readonly SignInManager<MoxUser> _signinManager;
        private readonly SettingsOptions _settingsExtension;
        private readonly ViewMessaging _viewMessaging;
        private readonly IStringLocalizer _localizer;
        private readonly MoxIdentityOptions _identityOptions;
        private readonly IServiceProvider _serviceProvider;

        public MyAccountController(IDbContextFetcher dbContextFetcher, UserManager<MoxUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<MoxUser> signInManager, IOptions<SettingsOptions> settingsExtension, ViewMessaging viewMessaging, IStringLocalizer localizer, IOptions<MoxIdentityOptions> identityOptions, IServiceProvider serviceProvider)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.MoxIdentityDbContext>();
            this._userManager = userManager;
            this._signinManager = signInManager;
            this._settingsExtension = settingsExtension.Value;
            this._viewMessaging = viewMessaging;
            this._localizer = localizer;
            this._identityOptions = identityOptions.Value;
            this._serviceProvider = serviceProvider;
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return NotFound();
            }

            var user = await this._context.Users.SingleOrDefaultAsync(x => x.Id == userId);
            if (user == null)
            {
                return NotFound();
            }

            var hasPassword = await this._userManager.HasPasswordAsync(user);

            return View(new EditMyAccountViewModel(user, hasPassword));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditMyAccountViewModel editUser)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await this._context.Users.SingleOrDefaultAsync(x => x.Id == userId);

            if (ModelState.IsValid)
            {
                user.UserName = editUser.Email;
                user.Email = editUser.Email;
                user.Name = editUser.Name;

                if (await this._userManager.IsInRoleAsync(user, Constants.AdminRole))
                {
                    user.RoleGroupId = editUser.RoleGroupId.Value;
                }

                // Validate and set password
                if (editUser.WantsPassword && !string.IsNullOrWhiteSpace(editUser.Password))
                {
                    var validationResult = await Task.WhenAll(this._userManager.PasswordValidators.AsEnumerable().Select(x => x.ValidateAsync(this._userManager, user, editUser.Password)));
                    if (validationResult.Any(x => !x.Succeeded))
                    {
                        foreach (var validationError in validationResult.Where(x => !x.Succeeded).SelectMany(x => x.Errors))
                        {
                            ModelState.AddModelError(string.Empty, validationError.Description);
                        }
                        return View(editUser);
                    }

                    if (await this._userManager.HasPasswordAsync(user))
                    {
                        await this._userManager.RemovePasswordAsync(user);
                    }
                    var result = await this._userManager.AddPasswordAsync(user, editUser.Password);
                }
                else if (!editUser.WantsPassword)
                {
                    if (await this._userManager.HasPasswordAsync(user))
                    {
                        await this._userManager.RemovePasswordAsync(user);
                    }
                }

                var updateResult = await this._userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(editUser);
                }

                await this._signinManager.RefreshSignInAsync(user);

                _viewMessaging.DisplayMessage("Ändringarna sparades!");
                return RedirectToAction("Edit");
            }

            return View(editUser);
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> EditOther([FromQuery] string view)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var extendedView = this._settingsExtension.AdditionalEditUserViews.FirstOrDefault(x => x.ViewName == view);

            if (extendedView == null)
                return RedirectToAction("Edit");

            var extension = (SettingsOptions.ISettingsExtension)HttpContext.RequestServices.GetRequiredService(extendedView.ExtensionType);
            var model = default(object);

            if (HttpContext.Request.Method == "POST")
            {
                model = await extension.TryUpdateModel((o, t) => this.TryUpdateModelAsync(o, t, ""));
                if (this.ModelState.IsValid)
                {
                    await extension.Save(userId, model, ModelState);
                    if (ModelState.IsValid)
                    {
                        return RedirectToAction("EditOther", new { view });
                    }
                }
            }
            else
            {
                model = await extension.GetViewModel(userId);
            }

            return View(extendedView.ViewName, model);
        }
    }
}
