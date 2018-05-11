using Mindbite.Mox.Verification.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Mindbite.Mox.Identity.Verification
{
    class BackDoorVerificator : IVerificator
    {
        public string Name => "Mox.Identity.Backdoor";

        public async Task<VerificationResult> VerifyAsync(IServiceProvider serviceProvider)
        {
            List<VerificationError> errors = new List<VerificationError>();
            try
            {
                var backdoor = serviceProvider.GetRequiredService<Identity.Services.IBackDoor>();
                var identityOptions = serviceProvider.GetRequiredService<IOptions<MoxIdentityOptions>>();

                if(identityOptions?.Value?.Backdoor == null)
                {
                    throw new Exception($"{nameof(Identity.MoxIdentityOptions)}.Backdoor must be configured.");
                }

                if (identityOptions.Value.Backdoor.UseBackdoor)
                {
                    await backdoor.Build(identityOptions.Value.Backdoor.Email, identityOptions.Value.Backdoor.Password);
                }
            }
            catch(InvalidOperationException e)
            {
                errors.Add(new VerificationError()
                {
                    Code = "IBackdoor service",
                    Description = $"{e.Message} \n\nInner exception: {e.InnerException?.Message}"
                });
            }
            catch(Exception e)
            {
                errors.Add(new VerificationError()
                {
                    Code = "Backdoor",
                    Description = $"{e.Message} \n\nInner exception: {e.InnerException?.Message}"
                });
            }

            return new VerificationResult()
            {
                Success = !errors.Any(),
                Verificator = this,
                Errors = errors
            };
        }
    }

    public class RolesCreatedVerificator : IVerificator
    {
        public string Name => "Mox.Identity.Role";

        private readonly string _role;

        public RolesCreatedVerificator(string role)
        {
            this._role = role;
        }

        public async Task<VerificationResult> VerifyAsync(IServiceProvider serviceProvider)
        {
            var backdoor = serviceProvider.GetRequiredService<Identity.Services.IBackDoor>();
            var result = await backdoor.CreateRole(this._role);

            if (!result.Succeeded)
            {
                return new VerificationResult
                {
                    Success = false,
                    Verificator = this,
                    Errors = result.Errors.Select(x => new VerificationError()
                    {
                        Code = x.Code,
                        Description = x.Description
                    })
                };
            }

            return new VerificationResult()
            {
                Success = true,
                Verificator = this,
                Errors = new VerificationError[] { }
            };
        }
    }

    public class EmailConfigSetVerificator : IVerificator
    {
        public string Name => "Mox.Identity.EmailConfig";

        public Task<VerificationResult> VerifyAsync(IServiceProvider serviceProvider)
        {
            List<VerificationError> errors = new List<VerificationError>();
            try
            {
                serviceProvider.GetRequiredService<Communication.EmailSender>();
            }
            catch (InvalidOperationException e)
            {
                errors.Add(new VerificationError()
                {
                    Code = "Identity EmailConfig",
                    Description = $"{e.Message} \n\nInner exception: {e.InnerException?.Message}"
                });
            }
            catch (Exception e)
            {
                errors.Add(new VerificationError()
                {
                    Code = "Identity EmailConfig",
                    Description = $"{e.Message} \n\nInner exception: {e.InnerException?.Message}"
                });
            }

            return Task.FromResult(new VerificationResult
            {
                Success = !errors.Any(),
                Verificator = this,
                Errors = errors
            });
        }
    }
}
