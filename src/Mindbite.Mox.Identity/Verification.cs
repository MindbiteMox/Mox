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
        int IVerificator.Order => 10;

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

#nullable enable
    public class AdminRoleGroupCreatedVerificator : IVerificator
    {
        public string Name => "Mox.Identity.AdminRoleGroup";
        int IVerificator.Order => 5;

        private readonly string _adminRoleGroupName;

        public AdminRoleGroupCreatedVerificator(string adminRoleGroupName)
        {
            this._adminRoleGroupName = adminRoleGroupName ?? throw new ArgumentNullException(nameof(adminRoleGroupName));
        }

        public async Task<VerificationResult> VerifyAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var allRoles = await roleManager.Roles.ToListAsync();

            var roleGroupCreatedVerificator = new RoleGroupCreatedVerificator(this._adminRoleGroupName, allRoles.Select(x => x.Name).ToList(), true);
            return await roleGroupCreatedVerificator.VerifyAsync(serviceProvider);
        }
    }

    public class RoleGroupCreatedVerificator : IVerificator
    {
        public string Name => "Mox.Identity.RoleGroup";

        private readonly string _roleGroupName;
        private readonly IEnumerable<string> _defaultRoles;
        private readonly bool _forceOnlyDefaultRoles;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roleGroupName"></param>
        /// <param name="defaultRoles"></param>
        /// <param name="forceOnlyDefaultRoles">When true the role group will have only the roles defined in <paramref name="defaultRoles"/></param>
        public RoleGroupCreatedVerificator(string roleGroupName, IEnumerable<string> defaultRoles, bool forceOnlyDefaultRoles = false)
        {  
            this._roleGroupName = roleGroupName ?? throw new ArgumentNullException(nameof(roleGroupName));
            this._defaultRoles = defaultRoles ?? throw new ArgumentNullException(nameof(defaultRoles));
            this._forceOnlyDefaultRoles = forceOnlyDefaultRoles;
        }

        public async Task<VerificationResult> VerifyAsync(IServiceProvider serviceProvider)
        {
            var roleGroupManager = serviceProvider.GetRequiredService<Services.IRoleGroupManager>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var existingRoleGroup = await roleGroupManager.RoleGroups.Include(x => x.Roles).FirstOrDefaultAsync(x => x.GroupName == this._roleGroupName);

            if (existingRoleGroup == null)
            {
                var allRoles = await roleManager.Roles.Where(x => this._defaultRoles.Contains(x.Name)).ToListAsync();
                await roleGroupManager.CreateAsync(new Data.Models.RoleGroup { GroupName = _roleGroupName }, allRoles.Select(x => x.Name));
            }
            else if (this._forceOnlyDefaultRoles)
            {
                var allRoles = await roleManager.Roles.Where(x => this._defaultRoles.Contains(x.Name)).ToListAsync();
                if (existingRoleGroup.Roles.Select(x => x.Role).Intersect(allRoles.Select(x => x.Name)).Count() != allRoles.Count())
                {
                    await roleGroupManager.UpdateAsync(existingRoleGroup, allRoles.Select(x => x.Name));
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
#nullable disable

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
