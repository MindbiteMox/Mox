using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.Controllers
{
    [AllowAnonymous]
    public class ForgotPassword : Controller
    {
        private readonly Services.IPasswordResetManager _passwordReset;
        private readonly UserManager<MoxUser> _userManager;
        private readonly Communication.EmailSender _emailSender;
        private readonly IViewRenderService _viewRenderer;
        private readonly IStringLocalizer _localizer;

        public ForgotPassword(Services.IPasswordResetManager passwordReset, UserManager<MoxUser> userManager, Communication.EmailSender emailSender, IViewRenderService viewRenderer, IStringLocalizer localizer)
        {
            this._passwordReset = passwordReset;
            this._userManager = userManager;
            this._emailSender = emailSender;
            this._viewRenderer = viewRenderer;
            this._localizer = localizer;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ViewModels.AccountViewModel.ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await this._userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, this._localizer["There is no user with this e-mail address!"]);
                    return View(model);
                }

                var resetRequest = await this._passwordReset.RequestResetAsync(user);

                var emailMessage = new System.Net.Mail.MailMessage
                {
                    Subject = this._localizer["Reset password"],
                    Body = await this._viewRenderer.RenderToStringAsync(this.ControllerContext, "Email/Reset", resetRequest),
                    IsBodyHtml = true
                };
                emailMessage.To.Add(user.Email);

                await this._emailSender.SendAsync(emailMessage);

                return RedirectToAction("WaitForEmail");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult WaitForEmail()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetSuccess()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Reset(Guid resetId)
        {
            bool isValid = await this._passwordReset.CheckResetIsValidAsync(resetId);

            if(!isValid)
            {
                return RedirectToAction("Index");
            }

            return View(new ViewModels.AccountViewModel.ResetViewModel() { ResetId = resetId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset(ViewModels.AccountViewModel.ResetViewModel model)
        {
            bool resetIsValid = await this._passwordReset.CheckResetIsValidAsync(model.ResetId);

            if (ModelState.IsValid && resetIsValid)
            {
                var user = await this._passwordReset.GetUser(model.ResetId);
                if (user == null)
                    return RedirectToAction("Index");

                var validationResult = await Task.WhenAll(this._userManager.PasswordValidators.AsEnumerable().Select(x => x.ValidateAsync(this._userManager, user, model.Password)));
                if(validationResult.Any(x => !x.Succeeded))
                {
                    foreach (var validationError in validationResult.Where(x => !x.Succeeded).SelectMany(x => x.Errors))
                    {
                        ModelState.AddModelError(string.Empty, validationError.Description);
                    }
                    return View(model);
                }

                var result = await this._passwordReset.CompleteResetAsync(model.ResetId, model.Password);
                if(!result.Success)
                {
                    if (result.Error == Services.ResetResult.ErrorType.IdentityError)
                    {
                        foreach (var error in result.IdentityErrors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("Index");
                    }
                }

                return RedirectToAction("ResetSuccess");
            }

            return View(model);
        }
    }
}
