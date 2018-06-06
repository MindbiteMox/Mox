using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

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

        [Required]
        [MaxLength(255)]
        [Display(Name = "Namn")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "E-post")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Lösenord")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        [Display(Name = "Upprepa lösenord")]
        public string RepeatPassword { get; set; }

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
            [Required]
            public string Id { get; set; }
            public string Name { get; set; }
            public bool Checked { get; set; }
        }

        [Required]
        public string Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Display(Name = "Namn")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "E-post")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Lösenord (fyll i för att ändra)")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password")]
        [Display(Name = "Upprepa lösenord")]
        public string RepeatPassword { get; set; }

        public RoleViewModel[] Roles { get; set; }

        public EditUserViewModel() { }

        public EditUserViewModel(IEnumerable<IdentityRole> roles, IEnumerable<string> rolesForUser, Data.Models.MoxUser user)
        {
            this.Roles = roles.Select(x => new RoleViewModel() { Id = x.Id, Name = x.Name, Checked = rolesForUser.Contains(x.Name) }).ToArray();

            this.Id = user.Id;
            this.Name = user.Name;
            this.Email = user.Email;
        }
    }
}
