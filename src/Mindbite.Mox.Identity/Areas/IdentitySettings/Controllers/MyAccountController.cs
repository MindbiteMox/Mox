using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public MyAccountController(IDbContextFetcher dbContextFetcher, UserManager<MoxUser> userManager, RoleManager<IdentityRole> roleManager, IUserRolesFetcher rolesFetcher, SignInManager<MoxUser> signInManager)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.MoxIdentityDbContext>();
            this._userManager = userManager;
            this._roleManager = roleManager;
            this._rolesFetcher = rolesFetcher as Services.UserRolesFetcher;
            this._signinManager = signInManager;
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
            var userRoles = await this._userManager.GetRolesAsync(user);

            return View(new EditMyAccountViewModel(roles, userRoles, user) { Id = user.Id });
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
                var userRoles = await this._context.UserRoles.ToListAsync();
                var myUserRoles = await this._userManager.GetRolesAsync(user);

                user.UserName = editUser.Email;
                user.Email = editUser.Email;
                user.Name = editUser.Name;

                if (!myUserRoles.Contains(Constants.AdminRole))
                {
                    var _dummy = new EditMyAccountViewModel(roles, myUserRoles, user);
                    editUser.Roles = _dummy.Roles;
                }

                // Validate and set password
                if (!string.IsNullOrWhiteSpace(editUser.Password))
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

                var updateResult = await this._userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(editUser);
                }

                var removeRolesResult = await this._userManager.RemoveFromRolesAsync(user, editUser.Roles.Where(x => !x.Checked && userRoles.Any(y => y.UserId == user.Id && y.RoleId == x.Id)).Select(x => x.Name));
                var addRolesResult = await this._userManager.AddToRolesAsync(user, editUser.Roles.Where(x => x.Checked && !userRoles.Any(y => y.UserId == user.Id && y.RoleId == x.Id)).Select(x => x.Name));

                if (removeRolesResult.Succeeded && addRolesResult.Succeeded)
                {
                    if (this._rolesFetcher != null)
                    {
                        this._rolesFetcher.ClearCache(user.Id);
                    }

                    await this._signinManager.RefreshSignInAsync(user);

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
                var userRoles = await this._context.UserRoles.ToListAsync();
                var myUserRoles = await this._userManager.GetRolesAsync(user);

                var _dummy = new EditMyAccountViewModel(roles, myUserRoles, user);
                //editUser.Roles = _dummy.Roles;
                editUser.IsAdmin = myUserRoles.Contains(Constants.AdminRole);
            }

            return View(editUser);
        }
    }
}
