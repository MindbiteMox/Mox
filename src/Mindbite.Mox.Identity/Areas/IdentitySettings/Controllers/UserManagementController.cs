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
using System.Security.Claims;

namespace Mindbite.Mox.Identity.Controllers
{

    [Area(Constants.SettingsArea)]
    [Authorize(Roles = Constants.AdminRole)]
    public class UserManagementController : Controller
    {
        private readonly Data.MoxIdentityDbContext _context;
        private readonly IUserValidator<MoxUser> _userValidator;
        private readonly UserManager<MoxUser> _userManager;
        private readonly SignInManager<MoxUser> _signinManager;
        private readonly IStringLocalizer _localizer;
        private readonly SettingsOptions _settingsExtension;
        private readonly MoxIdentityOptions _identityOptions;
        private readonly ViewMessaging _viewMessaging;
        private readonly IServiceProvider _serviceProvider;
        private readonly Services.RoleGroupManager _roleGroupManager;

        public UserManagementController(IDbContextFetcher dbContextFetcher, IUserValidator<MoxUser> userValidator, UserManager<MoxUser> userManager, SignInManager<MoxUser> signInManager, IStringLocalizer localizer, IOptions<SettingsOptions> settingsExtension, IOptions<MoxIdentityOptions> identityOptions, ViewMessaging viewMessaging, IServiceProvider serviceProvider, Services.RoleGroupManager roleGroupManager)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.MoxIdentityDbContext>();
            this._userValidator = userValidator;
            this._userManager = userManager;
            this._signinManager = signInManager;
            this._localizer = localizer;
            this._settingsExtension = settingsExtension.Value;
            this._identityOptions = identityOptions.Value;
            this._viewMessaging = viewMessaging;
            this._serviceProvider = serviceProvider;
            this._roleGroupManager = roleGroupManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Table(DataTableSort sort, string filter)
        {
            if (this._identityOptions.UsersTable != null)
            {
                return PartialView("Mox/UI/DataTable", await this._identityOptions.UsersTable(this.ControllerContext, sort, filter));
            }

            var dataSource = this._context.Users.Where(x => !x.IsHidden);

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var normalizedFilter = filter.ToLower();
                dataSource = dataSource.Where(x => x.Email.ToLower().Contains(normalizedFilter) || x.Name.ToLower().Contains(normalizedFilter));
            }

            var dataTable = DataTableBuilder
                .Create(dataSource.Select(x => new
                {
                    x.Id,
                    x.Email,
                    x.Name,
                    x.RoleGroup.GroupName
                }))
                .Sort(x => x.Email, SortDirection.Ascending, sort.DataTableSortColumn, sort.DataTableSortDirection)
                .Page(sort.DataTablePage)
                .RowLink(x => Url.Action("Edit", new { id = x.Id }))
                .Columns(columns =>
                {
                    columns.Add(x => x.Email).Title(this._localizer["E-post"]).Width(250);
                    columns.Add(x => x.Name).Title(this._localizer["Namn"]);
                    columns.Add(x => x.GroupName).Title(this._localizer["Behörighetsgrupp"]);
                })
                .Buttons(buttons =>
                {
                    buttons.Add(x => Url.Action("edit", new { x.Id })).CssClass("edit").Title("Redigera");
                    buttons.Add(x => Url.Action("delete", new { x.Id })).CssClass("delete").Title("Radera");
                });

            return PartialView("Mox/UI/DataTable", dataTable);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(new CreateUserViewModel());
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
                    userToCreate.RoleGroupId = newUser.RoleGroupId.Value;

                    var createUserResult = await this._userManager.CreateAsync(userToCreate);
                    if (!createUserResult.Succeeded)
                    {
                        foreach (var error in createUserResult.Errors)
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

            var hasPassword = await this._userManager.HasPasswordAsync(user);

            return View(new EditUserViewModel(user, hasPassword));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel editUser)
        {
            if (ModelState.IsValid)
            {
                var user = await this._context.Users.SingleOrDefaultAsync(x => x.Id == id);
                var userRoles = await this._context.UserRoles.ToListAsync();

                user.UserName = editUser.Email;
                user.Email = editUser.Email;
                user.Name = editUser.Name;
                user.RoleGroupId = editUser.RoleGroupId.Value;

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

                var signedOnUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(signedOnUserId == user.Id)
                {
                    var signedOnUser = await this._userManager.FindByIdAsync(signedOnUserId);
                    await this._signinManager.RefreshSignInAsync(signedOnUser);
                }

                _viewMessaging.DisplayMessage("Ändringarna sparades!");
                return RedirectToAction("Index");
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
