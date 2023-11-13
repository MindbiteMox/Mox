using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Mindbite.Mox.Identity.Data.Models
{
    public abstract class MoxUser : IdentityUser
    {
        [Display(Name = "E-post")]
        [PersonalData]
        public override string Email { get; set; }

        [Display(Name = "Användarnamn")]
        [PersonalData]
        public override string UserName { get; set; }

        [Display(Name = "Namn")]
        [PersonalData]
        public string Name { get; set; }

        public int RoleGroupId { get; set; }

        public bool IsDeleted { get; set; }
        public bool IsHidden { get; set; }

        [NotMapped]
        public string Initials => string.Join("", this.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x.FirstOrDefault() + "").Take(3));

        public RoleGroup RoleGroup { get; set; }
    }

    public class MoxUserBaseImpl : MoxUser
    {
    }
}
