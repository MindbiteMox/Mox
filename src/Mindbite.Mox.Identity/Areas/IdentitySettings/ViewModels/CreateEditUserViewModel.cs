using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Mindbite.Mox.Attributes;
using Mindbite.Mox.Extensions;

namespace Mindbite.Mox.Identity.ViewModels
{
    public class RoleViewModel
    {
        [Required]
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Checked { get; set; }
        public int Depth { get; set; }
        public bool IsParent { get; set; }
        public bool Disabled { get; set; }
    }

    public class CreateUserViewModel
    {
        [MoxRequired]
        [MaxLength(255)]
        [Display(Name = "Namn")]
        public string Name { get; set; }

        [MoxRequired]
        [EmailAddress]
        [MaxLength(255)]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "E-post")]
        public string Email { get; set; }

        public RoleViewModel[] Roles { get; set; }

        public CreateUserViewModel() { }

        public CreateUserViewModel(IEnumerable<IdentityExtensions.RoleTreeNode> roles, string[] preselectedRoles = null)
        {
            this.Roles = roles.Select(x => new RoleViewModel
            {
                Id = x.RoleName,
                Name = x.DisplayName,
                Checked = x.IsLeaf ? preselectedRoles?.Contains(x.RoleName) ?? false : roles.Where(y => y.RoleName.StartsWith(x.RoleName) && y.IsLeaf).All(y => preselectedRoles?.Contains(y.RoleName) ?? false),
                Depth = x.Depth,
                IsParent = !x.IsLeaf
            }).ToArray();
        }
    }

    public class EditUserViewModel
    {
        [MoxRequired]
        public string Id { get; set; }

        [MoxRequired]
        [MaxLength(255)]
        [Display(Name = "Namn")]
        public string Name { get; set; }

        [MoxRequired]
        [EmailAddress]
        [MaxLength(255)]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "E-post")]
        public string Email { get; set; }

        public bool HasPassword { get; set; }
        public bool WantsPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Lösenord (fyll i för att ändra)")]
        [MoxRequiredIf("WantsPassword", AndNot = "HasPassword")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [MoxCompare("Password", If = "WantsPassword")]
        [Display(Name = "Upprepa lösenord")]
        [MoxRequiredIf("WantsPassword", AndNot = "HasPassword")]
        public string RepeatPassword { get; set; }

        public bool RolesDisabled { get; set; }
        public string RolesDisabledLink { get; set; }
        public RoleViewModel[] Roles { get; set; }

        public EditUserViewModel() { }

        public EditUserViewModel(IEnumerable<IdentityExtensions.RoleTreeNode> roles, IEnumerable<string> preselectedRoles, Data.Models.MoxUser user, bool hasPassword, bool disableRoles, string rolesDisabledLink)
        {
            this.Roles = roles.Select(x => new RoleViewModel
            {
                Id = x.RoleName,
                Name = x.DisplayName,
                Checked = x.IsLeaf ? preselectedRoles?.Contains(x.RoleName) ?? false : roles.Where(y => y.RoleName.StartsWith(x.RoleName) && y.IsLeaf).All(y => preselectedRoles?.Contains(y.RoleName) ?? false),
                Depth = x.Depth,
                IsParent = !x.IsLeaf,
                Disabled = disableRoles
            }).ToArray();

            this.Id = user.Id;
            this.Name = user.Name;
            this.Email = user.Email;
            this.HasPassword = hasPassword;
            this.WantsPassword = hasPassword;
            this.RolesDisabled = disableRoles;
            this.RolesDisabledLink = rolesDisabledLink;
        }
    }
}
