using Mindbite.Mox.Verification.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Mindbite.Mox.Identity.Verification
{
    internal class BackDoorVerificator : IVerificator
    {
        public string Name => "Mox.Identity.Backdoor";

        public async Task<VerificationResult> VerifyAsync(IServiceProvider serviceProvider)
        {
            var errors = new List<VerificationError>();

            try
            {
                var backdoor = serviceProvider.GetRequiredService<Services.IBackDoor>();
                var identityOptions = serviceProvider.GetRequiredService<IOptions<MoxIdentityOptions>>();

                if(identityOptions?.Value?.Backdoor == null)
                {
                    return new VerificationResult { Success = true, Verificator = this };
                }

                if (identityOptions.Value.Backdoor.UseBackdoor)
                {
                    var backdoorOptions = identityOptions.Value.Backdoor;

                    if(!string.IsNullOrWhiteSpace(backdoorOptions.RemotePasswordAuthUrl))
                    {
                        if (!string.IsNullOrWhiteSpace(backdoorOptions.Password))
                        {
                            throw new Exception($"{nameof(MoxIdentityOptions.BackdoorOptions.Password)} cannot be set when using {nameof(MoxIdentityOptions.BackdoorOptions.RemotePasswordAuthUrl)}.");
                        }
                        else if (string.IsNullOrWhiteSpace(backdoorOptions.RemotePasswordAuthDataFormatString))
                        {
                            throw new Exception($"{nameof(MoxIdentityOptions.BackdoorOptions.RemotePasswordAuthDataFormatString)} must be set when using {nameof(MoxIdentityOptions.BackdoorOptions.RemotePasswordAuthUrl)}.");
                        }
                    }

                    await backdoor.Build(identityOptions.Value.Backdoor.Email, identityOptions.Value.Backdoor.Password);
                }
            }
            catch(InvalidOperationException e)
            {
                errors.Add(new VerificationError()
                {
                    Code = "IBackDoor service",
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

    public class AdminRoleGroupCreatedVerificator : IVerificator
    {
        public string Name => "Mox.Identity.AdminRoleGroup";

        private readonly string _adminRoleGroupName;

        public AdminRoleGroupCreatedVerificator(string adminRoleGroupName = Services.RoleGroupManager.DefaultAdministratorGroupName)
        {
            this._adminRoleGroupName = adminRoleGroupName;
        }

        public async Task<VerificationResult> VerifyAsync(IServiceProvider serviceProvider)
        {
            var roleGroupManager = serviceProvider.GetRequiredService<Services.RoleGroupManager>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var adminRoleGroup = await roleGroupManager.RoleGroups.Include(x => x.Roles).FirstOrDefaultAsync();
            var allRoles = await roleManager.Roles.ToListAsync();

            if (adminRoleGroup == null)
            {
                await roleGroupManager.CreateAsync(new Data.Models.RoleGroup { GroupName = _adminRoleGroupName }, allRoles.Select(x => x.Name));
            }
            else if(adminRoleGroup.Roles.Select(x => x.Role).Intersect(allRoles.Select(x => x.Name)).Count() != allRoles.Count())
            {
                await roleGroupManager.UpdateAsync(adminRoleGroup, allRoles.Select(x => x.Name));
            }

            return new VerificationResult()
            {
                Success = true,
                Verificator = this,
                Errors = Array.Empty<VerificationError>()
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
            var backdoor = serviceProvider.GetRequiredService<Services.IBackDoor>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if(await roleManager.FindByNameAsync(this._role) == null)
            {
                var result = await roleManager.CreateAsync(new IdentityRole { Name = this._role });

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
            }

            return new VerificationResult()
            {
                Success = true,
                Verificator = this,
                Errors = Array.Empty<VerificationError>()
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
