using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Mindbite.Mox.Attributes;

namespace Mindbite.Mox.Identity.ViewModels
{
    public class CreateUserViewModel
    {
        public class RoleViewModel
        {
            [Required]
            public string Id { get; set; }
            public string Name { get; set; }
            public bool Checked { get; set; }
        }

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

        public CreateUserViewModel(IEnumerable<IdentityRole> roles, string[] preselectedRoles = null)
        {
            this.Roles = roles.Select(x => new RoleViewModel() { Id = x.Id, Name = x.Name, Checked = preselectedRoles?.Contains(x.Name) ?? false }).ToArray();
        }
    }

    public class EditUserViewModel
    {
        public class RoleViewModel
        {
            [MoxRequired]
            public string Id { get; set; }
            public string Name { get; set; }
            public bool Checked { get; set; }
        }

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

        public RoleViewModel[] Roles { get; set; }

        public EditUserViewModel() { }

        public EditUserViewModel(IEnumerable<IdentityRole> roles, IEnumerable<string> rolesForUser, Data.Models.MoxUser user, bool hasPassword)
        {
            this.Roles = roles.Select(x => new RoleViewModel() { Id = x.Id, Name = x.Name, Checked = rolesForUser.Contains(x.Name) }).ToArray();

            this.Id = user.Id;
            this.Name = user.Name;
            this.Email = user.Email;
            this.HasPassword = hasPassword;
            this.WantsPassword = hasPassword;
        }
    }
}
