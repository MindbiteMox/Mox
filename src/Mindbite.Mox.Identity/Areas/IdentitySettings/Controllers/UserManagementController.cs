using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.Identity.ViewModels;
using Mindbite.Mox.Services;
using Mindbite.Mox.UI;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;
using Mindbite.Mox.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace Mindbite.Mox.Identity.Controllers
{

    [Area(Constants.SettingsArea)]
    [Authorize(Roles = Constants.AdminRole)]
    public class UserManagementController : Controller
    {
        private readonly Data.MoxIdentityDbContext _context;
        private readonly IUserValidator<MoxUser> _userValidator;
        private readonly UserManager<MoxUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Services.UserRolesFetcher _rolesFetcher;
        private readonly SignInManager<MoxUser> _signinManager;
        private readonly IStringLocalizer _localizer;
        private readonly SettingsOptions _settingsExtension;
        private readonly MoxIdentityOptions _identityOptions;
        private readonly ViewMessaging _viewMessaging;

        public UserManagementController(IDbContextFetcher dbContextFetcher, IUserValidator<MoxUser> userValidator, UserManager<MoxUser> userManager, RoleManager<IdentityRole> roleManager, IUserRolesFetcher rolesFetcher, SignInManager<MoxUser> signInManager, IStringLocalizer localizer, IOptions<SettingsOptions> settingsExtension, IOptions<MoxIdentityOptions> identityOptions, ViewMessaging viewMessaging)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.MoxIdentityDbContext>();
            this._userValidator = userValidator;
            this._userManager = userManager;
            this._roleManager = roleManager;
            this._rolesFetcher = rolesFetcher as Services.UserRolesFetcher;
            this._signinManager = signInManager;
            this._localizer = localizer;
            this._settingsExtension = settingsExtension.Value;
            this._identityOptions = identityOptions.Value;
            this._viewMessaging = viewMessaging;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Table(DataTableSort sort, string filter)
        {
            var dataSource = this._context.Users.Where(x => x.Email != "backdoor@mindbite.se").AsQueryable(); // TODO: Make a better way to hide users from this list!
            var userRoles = await this._context.UserRoles.ToListAsync();
            var roles = await this._context.Roles.ToListAsync();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var normalizedFilter = filter.ToLower();
                dataSource = dataSource.Where(x => x.Email.ToLower().Contains(normalizedFilter) || x.Name.ToLower().Contains(normalizedFilter));
            }

            var dataTable = DataTableBuilder
                .Create(dataSource.Select(x => new
                {
                    Id = x.Id,
                    Email = x.Email,
                    Name = x.Name,
                    Roles = string.Join(", ", roles.Where(role => userRoles.Where(ur => ur.UserId == x.Id).Select(ur => ur.RoleId).Contains(role.Id)).Select(role => this._localizer[$"role_{role.Name}"]))
                }))
                .Sort(sort.DataTableSortColumn ?? "Email", sort.DataTableSortDirection ?? "Ascending")
                .Page(sort.DataTablePage)
                .RowLink(x => Url.Action("Edit", new { id = x.Id }))
                .Columns(columns =>
                {
                    columns.Add(x => x.Email).Title(this._localizer["E-post"]).Width(250);
                    columns.Add(x => x.Name).Title(this._localizer["Namn"]);
                    columns.Add(x => x.Roles).Title(this._localizer["Behörigheter"]);
                })
                .Buttons(buttons =>
                {
                    buttons.Add(x => Url.Action("edit", new { x.Id })).CssClass("edit");
                    buttons.Add(x => Url.Action("delete", new { x.Id })).CssClass("delete");
                });

            return PartialView("Mox/UI/DataTable", dataTable);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var roles = await this._roleManager.Roles.ToListAsync();
            return View(new CreateUserViewModel(roles, preselectedRoles: new string[] { Constants.MoxRole }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel newUser)
        {
            if (ModelState.IsValid)
            {
                var user = await this._userManager.FindByEmailAsync(newUser.Email);
                if (user == null)
                {
                    var userToCreate = default(MoxUser);

                    if(this._identityOptions?.DefaultUserType != null)
                    {
                        userToCreate = (MoxUser)Activator.CreateInstance(this._identityOptions.DefaultUserType);
                    }
                    else
                    {
                        userToCreate = new MoxUserBaseImpl();
                    }

                    userToCreate.UserName = newUser.Email;
                    userToCreate.Email = newUser.Email;
                    userToCreate.Name = newUser.Name;

                    var createUserResult = await this._userManager.CreateAsync(userToCreate);
                    if(!createUserResult.Succeeded)
                    {
                        foreach (var error in createUserResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(newUser);
                    }

                    var addRoleResult = await this._userManager.AddToRolesAsync(userToCreate, newUser.Roles.Where(x => x.Checked).Select(x => x.Name));

                    if (!addRoleResult.Succeeded)
                    {
                        foreach (var error in addRoleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(newUser);
                    }

                    _viewMessaging.DisplayMessage("Kontot skapades!");
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, this._localizer["E-post addressen är upptagen!"]);
                }
            }

            return View(newUser);
        }

        
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await this._context.Users.SingleOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await this._roleManager.Roles.ToListAsync();
            var userRoles = await this._userManager.GetRolesAsync(user);
            var hasPassword = await this._userManager.HasPasswordAsync(user);

            return View(new EditUserViewModel(roles, userRoles, user, hasPassword));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel editUser)
        {
            if (id != editUser.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await this._context.Users.SingleOrDefaultAsync(x => x.Id == editUser.Id);
                var userRoles = await this._context.UserRoles.ToListAsync();

                user.UserName = editUser.Email;
                user.Email = editUser.Email;
                user.Name = editUser.Name;

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
                    await this._userManager.AddPasswordAsync(user, editUser.Password);
                }
                else if(!editUser.WantsPassword)
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

                var removeRolesResult = await this._userManager.RemoveFromRolesAsync(user, editUser.Roles.Where(x => !x.Checked && userRoles.Any(y => y.UserId == user.Id && y.RoleId == x.Id)).Select(x => x.Name));
                var addRolesResult = await this._userManager.AddToRolesAsync(user, editUser.Roles.Where(x => x.Checked && !userRoles.Any(y => y.UserId == user.Id && y.RoleId == x.Id)).Select(x => x.Name));

                if (removeRolesResult.Succeeded && addRolesResult.Succeeded)
                {
                    if (this._rolesFetcher != null)
                    {
                        this._rolesFetcher.ClearCache(user.Id);
                    }

                    string signedOnUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var signedOnUser = await this._userManager.FindByIdAsync(signedOnUserId);
                    await this._signinManager.RefreshSignInAsync(signedOnUser);

                    _viewMessaging.DisplayMessage("Ändringarna sparades!");
                    return RedirectToAction("Index");
                }
                else
                {
                    var errors = removeRolesResult.Errors.Concat(addRolesResult.Errors);
                    foreach (var error in errors.Select(x => x.Description).Distinct())
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                }
            }

            return View(editUser);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id, bool? saveChangesError = false)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await this._context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            if (saveChangesError.GetValueOrDefault())
            {
                ViewData["ErrorMessage"] = this._localizer["Kunde inte ta bort. Försök igen"];
            }

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var userToDelete = await this._context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);

            if (userToDelete == null)
            {
                return NotFound();
            }

            try
            {
                string signedOnUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if(userToDelete.Id == signedOnUserId)
                {
                    await this._signinManager.SignOutAsync();
                }

                await this._userManager.DeleteAsync(userToDelete);

                _viewMessaging.DisplayMessage("Kontot togs bort!");
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                return RedirectToAction("Delete", new { id, saveChangesError = true });
            }
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> EditOther(string id, [FromQuery] string view)
        {
            var extendedView = this._settingsExtension.AdditionalEditUserViews.FirstOrDefault(x => x.ViewName == view);

            if (extendedView == null)
                return RedirectToAction("Edit", new { id });

            var extension = (SettingsOptions.ISettingsExtension)HttpContext.RequestServices.GetRequiredService(extendedView.ExtensionType);
            var model = default(object);

            if (HttpContext.Request.Method == "POST")
            {
                model = await extension.TryUpdateModel((o, t) => this.TryUpdateModelAsync(o, t, ""));

                if(this.ModelState.IsValid)
                {
                    await extension.Save(id, model);
                    return RedirectToAction("EditOther", new { id, view });
                }
            }
            else
            {
                model = await extension.GetViewModel(id);
            }

            return View(extendedView.ViewName, model);
        }
    }
}
