using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.Services
{
    public interface IMagicLinkManager
    {
        Task InvalidateAllTokensAsync(MoxUser user);
        string GenerateNormalizedShortCode();
        string FormatShortCode(string normalizedShortCode);
        Task<(Guid? magicToken, string shortCode, GenerateMagicLinkError? error)> GenerateMagicTokenAsync(MoxUser user);
        Task<(bool success, SendMagicLinkError? error)> GenerateAndSendMagicLinkAsync(ActionContext actionContext, MoxUser user, string returnUrl);
        Task<bool> ValidateShortCodeAsync(MoxUser user, string shortCode);
        Task<(bool isValid, MoxUser user)> ValidateMagicTokenAsync(Guid magicToken);
    }

    public enum GenerateMagicLinkError
    {
        [Display(Name = "Inloggningskontot är låst")]
        UserLockedOut,
        [Display(Name = "För många försök")]
        TooManyLinksGenerated
    }

    public enum SendMagicLinkError
    {
        [Display(Name = "Inloggningskontot är låst")]
        UserLockedOut,
        [Display(Name = "För många försök")]
        TooManyLinksGenerated,
        [Display(Name = "Mailet kunde inte skickas")]
        EmailError,
    }

    public class MagicLinkManager : IMagicLinkManager
    {
        private readonly Data.MoxIdentityDbContext _context;
        private readonly UserManager<MoxUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Communication.EmailSender _emailSender;
        private readonly MoxIdentityOptions _identityOptions;
        private readonly Configuration.Config _moxConfig;
        private readonly IStringLocalizer _localizer;
        private readonly IViewRenderService _viewRenderer;

        public MagicLinkManager(IDbContextFetcher dbContextFetcher, UserManager<MoxUser> userManager, IHttpContextAccessor httpContextAccessor, Communication.EmailSender emailSender, IOptions<MoxIdentityOptions> identityOptions, IOptions<Configuration.Config> moxConfig, IStringLocalizer localizer, IViewRenderService viewRenderer)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.MoxIdentityDbContext>();
            this._userManager = userManager;
            this._httpContextAccessor = httpContextAccessor;
            this._emailSender = emailSender;
            this._identityOptions = identityOptions.Value;
            this._moxConfig = moxConfig.Value;
            this._localizer = localizer;
            this._viewRenderer = viewRenderer;
        }

        public async Task InvalidateAllTokensAsync(MoxUser user)
        {
            var tokens = await this._context.MagicLinkTokens.Where(x => x.UserId == user.Id && !x.Used && !x.Invalidated).ToListAsync();

            foreach (var token in tokens)
            {
                token.Invalidated = true;
                this._context.Update(token);
            }

            await this._context.SaveChangesAsync();
        }

        public string GenerateNormalizedShortCode()
        {
            const int size = 6;
            var chars = "ABCDEFGHJKMNPQRTWXYZ2346789".ToCharArray();

            var data = new byte[size];
            using (var generator = new RNGCryptoServiceProvider())
            {
                generator.GetNonZeroBytes(data);
            }

            var result = new System.Text.StringBuilder(size);
            foreach (var b in data)
            {
                result.Append(chars[b % chars.Length]);
            }

            return result.ToString();
        }

        public string FormatShortCode(string normalizedShortCode)
        {
            var parts = new List<string>();
            var shortCodeView = normalizedShortCode;
            while(shortCodeView.Length > 0)
            {
                var take = Math.Min(3, shortCodeView.Length);
                var part = shortCodeView.Substring(0, take);
                parts.Add(part);

                shortCodeView = shortCodeView.Substring(take);
            }
            return string.Join(" ", parts);
        }

        public async Task<(Guid? magicToken, string shortCode, GenerateMagicLinkError? error)> GenerateMagicTokenAsync(MoxUser user)
        {
            if(await this._userManager.IsLockedOutAsync(user))
            {
                return (null, null, GenerateMagicLinkError.UserLockedOut);
            }

            var httpContext = this._httpContextAccessor.HttpContext;

            await InvalidateAllTokensAsync(user);

            var magicLink = new MagicLinkToken
            {
                RequestedByClientInfo = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown",
                UserId = user.Id,
                ValidUntil = DateTime.Now.AddMinutes(this._identityOptions.MagicLink.ValidForMinutes),
                Used = false,
                NormalizedShortCode = GenerateNormalizedShortCode()
            };
            this._context.Add(magicLink);

            await this._context.SaveChangesAsync();

            return (magicLink.Id, magicLink.NormalizedShortCode, null);
        }

        public async Task<(bool success, SendMagicLinkError? error)> GenerateAndSendMagicLinkAsync(ActionContext actionContext, MoxUser user, string returnUrl)
        {
            var (token, shortCode, error) = await GenerateMagicTokenAsync(user);
            if(token == null)
            {
                return (false, (SendMagicLinkError)error);
            }

            var emailMessage = new System.Net.Mail.MailMessage
            {
                Subject = this._localizer["Direktinloggning"],
                Body = await this._viewRenderer.RenderToStringAsync(actionContext, "Mox/Identity/Email/MagicLink", new ViewModels.AccountViewModel.MagicLinkEmailViewModel
                {
                    MagicToken = token.Value,
                    ShortCode = this.FormatShortCode(shortCode),
                    RememberMe = false,
                    ReturnUrl = returnUrl
                }),
                IsBodyHtml = true
            };
            emailMessage.To.Add(user.Email);

            try
            {
                await this._emailSender.SendAsync(emailMessage);
            }
            catch
            {
                await InvalidateAllTokensAsync(user);
                return (false, SendMagicLinkError.EmailError);
            }

            return (true, null);
        }

        public async Task<bool> ValidateShortCodeAsync(MoxUser user, string shortCode)
        {
            var tokens = await this._context.MagicLinkTokens.Where(x => x.UserId == user.Id && !x.Used && !x.Invalidated && x.ValidUntil > DateTime.Now && x.User != null).ToListAsync();
            var shortCodeDidMatch = tokens.Any(x => x.NormalizedShortCode == Regex.Replace(shortCode, @"\s+", "").ToUpper());

            if (shortCodeDidMatch)
            {
                await this.InvalidateAllTokensAsync(user);
            }

            return shortCodeDidMatch;
        }

        public async Task<(bool isValid, MoxUser user)> ValidateMagicTokenAsync(Guid magicToken)
        {
            var token = await this._context.MagicLinkTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == magicToken && !x.Used && !x.Invalidated && x.ValidUntil > DateTime.Now && x.User != null);
            if(token == null)
            {
                return (false, null);
            }

            await this.InvalidateAllTokensAsync(token.User);

            return (true, token.User);
        }
    }
}
