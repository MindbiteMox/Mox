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
    [Authorize(Roles = Constants.EditMyOwnAccountRole)]
    public class MyAccountController : Controller
    {
        private readonly Data.MoxIdentityDbContext _context;
        private readonly UserManager<MoxUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Services.UserRolesFetcher _rolesFetcher;
        private readonly SignInManager<MoxUser> _signinManager;
        private readonly SettingsOptions _settingsExtension;
        private readonly ViewMessaging _viewMessaging;
        private readonly IStringLocalizer _localizer;
        private readonly MoxIdentityOptions _identityOptions;
        private readonly IServiceProvider _serviceProvider;

        public MyAccountController(IDbContextFetcher dbContextFetcher, UserManager<MoxUser> userManager, RoleManager<IdentityRole> roleManager, IUserRolesFetcher rolesFetcher, SignInManager<MoxUser> signInManager, IOptions<SettingsOptions> settingsExtension, ViewMessaging viewMessaging, IStringLocalizer localizer, IOptions<MoxIdentityOptions> identityOptions, IServiceProvider serviceProvider)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.MoxIdentityDbContext>();
            this._userManager = userManager;
            this._roleManager = roleManager;
            this._rolesFetcher = rolesFetcher as Services.UserRolesFetcher;
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
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return NotFound();
            }

            var user = await this._context.Users.SingleOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await this._roleManager.Roles.ToListAsync();
            var tree = roles.BuildLocalizedTree(this._localizer);
            var flatTree = tree.Flatten();

            var userRoles = await this._userManager.GetRolesAsync(user);
            var hasPassword = await this._userManager.HasPasswordAsync(user);

            var disableRoles = false;
            if (this._identityOptions.Groups.DisableGroupSettingsCallback != null)
            {
                disableRoles = await this._identityOptions.Groups.DisableGroupSettingsCallback(this._serviceProvider, user);
            }

            var rolesDisabledLink = default(string);
            if (disableRoles && this._identityOptions.Groups.GroupSettingsMovedToThisUrl != null)
            {
                rolesDisabledLink = await this._identityOptions.Groups.GroupSettingsMovedToThisUrl(this._serviceProvider, user, Url);
            }

            return View(new EditMyAccountViewModel(flatTree, userRoles, user, hasPassword, disableRoles, rolesDisabledLink) { Id = user.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditMyAccountViewModel editUser)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (ModelState.IsValid)
            {
                var user = await this._context.Users.SingleOrDefaultAsync(x => x.Id == editUser.Id);

                var roles = await this._roleManager.Roles.ToListAsync();
                var tree = roles.BuildLocalizedTree(this._localizer);
                var flatTree = tree.Flatten();

                var myUserRoles = await this._userManager.GetRolesAsync(user);

                user.UserName = editUser.Email;
                user.Email = editUser.Email;
                user.Name = editUser.Name;

                if (!myUserRoles.Contains(Constants.AdminRole))
                {
                    var hasPassword = await this._userManager.HasPasswordAsync(user);

                    var disableRoles = false;
                    if (this._identityOptions.Groups.DisableGroupSettingsCallback != null)
                    {
                        disableRoles = await this._identityOptions.Groups.DisableGroupSettingsCallback(this._serviceProvider, user);
                    }

                    var rolesDisabledLink = default(string);
                    if (disableRoles && this._identityOptions.Groups.GroupSettingsMovedToThisUrl != null)
                    {
                        rolesDisabledLink = await this._identityOptions.Groups.GroupSettingsMovedToThisUrl(this._serviceProvider, user, Url);
                    }

                    var _dummy = new EditMyAccountViewModel(flatTree, myUserRoles, user, hasPassword, disableRoles, rolesDisabledLink);
                    editUser.Roles = _dummy.Roles;
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

                var removeRolesResult = await this._userManager.RemoveFromRolesAsync(user, await this._userManager.GetRolesAsync(user));
                var addRolesResult = await this._userManager.AddToRolesAsync(user, editUser.Roles.Where(x => x.Checked && !x.IsParent).Select(x => x.Id));

                if (removeRolesResult.Succeeded && addRolesResult.Succeeded)
                {
                    if (this._rolesFetcher != null)
                    {
                        this._rolesFetcher.ClearCache(user.Id);
                    }

                    await this._signinManager.RefreshSignInAsync(user);

                    _viewMessaging.DisplayMessage("Ändringarna sparades!");
                    return RedirectToAction("Edit");
                }
                else
                {
                    foreach (var error in removeRolesResult.Errors.Concat(addRolesResult.Errors).Select(x => x.Description).Distinct())
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                }
            }
            else
            {
                var user = await this._context.Users.SingleOrDefaultAsync(x => x.Id == editUser.Id);

                var roles = await this._roleManager.Roles.ToListAsync();
                var tree = roles.BuildLocalizedTree(this._localizer);
                var flatTree = tree.Flatten();

                var userRoles = await this._context.UserRoles.ToListAsync();
                var myUserRoles = await this._userManager.GetRolesAsync(user);
                var hasPassword = await this._userManager.HasPasswordAsync(user);


                var disableRoles = false;
                if (this._identityOptions.Groups.DisableGroupSettingsCallback != null)
                {
                    disableRoles = await this._identityOptions.Groups.DisableGroupSettingsCallback(this._serviceProvider, user);
                }

                var rolesDisabledLink = default(string);
                if (disableRoles && this._identityOptions.Groups.GroupSettingsMovedToThisUrl != null)
                {
                    rolesDisabledLink = await this._identityOptions.Groups.GroupSettingsMovedToThisUrl(this._serviceProvider, user, Url);
                }

                var _dummy = new EditMyAccountViewModel(flatTree, myUserRoles, user, hasPassword, disableRoles, rolesDisabledLink);

                //editUser.Roles = _dummy.Roles;
                editUser.IsAdmin = myUserRoles.Contains(Constants.AdminRole);
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
                if (this.ModelState.IsValid)
                {
                    model = await extension.TryUpdateModel((o, t) => this.TryUpdateModelAsync(o, t, ""));
                    await extension.Save(userId, model);
                    return RedirectToAction("EditOther", new { view });
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
