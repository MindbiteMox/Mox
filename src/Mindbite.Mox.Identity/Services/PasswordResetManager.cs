using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Identity.Data.Models;
using Mindbite.Mox.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Mindbite.Mox.Identity.Services
{
    public class ResetResult
    {
        public enum ErrorType
        {
            None, NoValidResetFound, IdentityError
        }

        public bool Success { get; set; }
        public ErrorType Error { get; set; }
        public IEnumerable<IdentityError> IdentityErrors { get; set; }
    }

    public interface IPasswordResetManager
    {
        Task<PasswordReset> RequestResetAsync(MoxUser user);
        Task<ResetResult> CompleteResetAsync(Guid resetId, string newPassword);
        Task<bool> CheckResetIsValidAsync(Guid resetId);
        Task<MoxUser> GetUser(Guid resetId);
    }

    public class PasswordResetManager : IPasswordResetManager
    {
        private readonly Data.MoxIdentityDbContext _context;
        private readonly UserManager<MoxUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Mox.Communication.EmailSender _emailSender;

        public PasswordResetManager(IDbContextFetcher dbContextFetcher, UserManager<MoxUser> userManager, IHttpContextAccessor httpContextAccessor, Mox.Communication.EmailSender emailSender)
        {
            this._context = dbContextFetcher.FetchDbContext<Data.MoxIdentityDbContext>();
            this._userManager = userManager;
            this._httpContextAccessor = httpContextAccessor;
            this._emailSender = emailSender;
        }

        public async Task<ResetResult> CompleteResetAsync(Guid resetId, string newPassword)
        {
            if (resetId == null)
                throw new ArgumentNullException($"Parameter {nameof(resetId)} cannot be null!");

            if (resetId == Guid.Empty)
                throw new ArgumentException($"Parameter {nameof(resetId)} cannot be empty!");

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException($"Parameter {nameof(newPassword)} cannot be null or empty!");

            var reset = await this._context.PasswordReset.Include(x => x.User).FirstOrDefaultAsync(x => !x.Completed && x.ValidUntil >= DateTime.Now && resetId == x.Id);

            if (reset == null)
                return new ResetResult() { Success = false, Error = ResetResult.ErrorType.NoValidResetFound };

            var identityResetResult = await this._userManager.ResetPasswordAsync(reset.User, reset.ResetToken, newPassword);

            if (!identityResetResult.Succeeded)
                return new ResetResult() { Success = false, Error = ResetResult.ErrorType.IdentityError, IdentityErrors = identityResetResult.Errors };

            // If one reset succedes, they are all invalidated
            var pendingResets = await this._context.PasswordReset.Where(x => !x.Completed && x.ValidUntil >= DateTime.Now && x.UserId == reset.UserId).ToListAsync();
            foreach(var pendingReset in pendingResets)
            {
                pendingReset.Completed = true;
                pendingReset.CompletedOn = DateTime.Now;
                this._context.Update(pendingReset);
            }
            await this._context.SaveChangesAsync();

            return new ResetResult() { Success = true, Error = ResetResult.ErrorType.None };
        }

        public async Task<PasswordReset> RequestResetAsync(MoxUser user)
        {
            if (user == null)
                throw new ArgumentNullException($"Parameter {nameof(user)} cannot be null!");

            var httpContext = this._httpContextAccessor.HttpContext;
            var resetToken = await this._userManager.GeneratePasswordResetTokenAsync(user);

            var reset = new PasswordReset()
            {
                UserId = user.Id,
                ResetToken = resetToken,
                Completed = false,
                CreatedOn = DateTime.Now,
                RequestedByClientInfo = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Okänt",
                CompletedOn = null,
                ValidUntil = DateTime.Today.AddDays(1)
            };

            this._context.Add(reset);
            await this._context.SaveChangesAsync();

            return reset;
        }

        public async Task<bool> CheckResetIsValidAsync(Guid resetId)
        {
            return await this._context.PasswordReset.AnyAsync(x => !x.Completed && x.ValidUntil >= DateTime.Now && x.Id == resetId);
        }

        public async Task<MoxUser> GetUser(Guid resetId)
        {
            var reset = await this._context.PasswordReset.SingleOrDefaultAsync(x => x.Id == resetId);
            if (reset == null)
                return null;

            return await this._userManager.FindByIdAsync(reset.UserId);
        }
    }
}
