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
        public override string Email { get; set; }

        [Display(Name = "Användarnamn")]
        public override string UserName { get; set; }

        [Display(Name = "Namn")]
        public string Name { get; set; }

        [NotMapped]
        public string Initials => string.Join("", this.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x.FirstOrDefault() + "").Take(3));
    }

    public class MoxUserBaseImpl : MoxUser
    {
    }
}
