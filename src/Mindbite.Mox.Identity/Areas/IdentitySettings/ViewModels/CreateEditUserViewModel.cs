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

        [MoxRequired]
        [Display(Name = "Behörighetsgrupp")]
        public int? RoleGroupId { get; set; }
    }

    public class EditUserViewModel
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

        [MoxRequired]
        [Display(Name = "Behörighetsgrupp")]
        public int? RoleGroupId { get; set; }

        public EditUserViewModel() { }

        public EditUserViewModel(Data.Models.MoxUser user, bool hasPassword)
        {
            this.Name = user.Name;
            this.Email = user.Email;
            this.RoleGroupId = user.RoleGroupId;
            this.HasPassword = hasPassword;
            this.WantsPassword = hasPassword;
        }
    }
}
