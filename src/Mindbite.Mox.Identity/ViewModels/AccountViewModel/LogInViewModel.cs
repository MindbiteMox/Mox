using Mindbite.Mox.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.ViewModels.AccountViewModel
{
    public class LogInViewModel
    {
        [MoxRequired]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "E-post")]
        public string Email { get; set; }

        [MoxRequired]
        [DataType(DataType.Password)]
        [Display(Name = "Lösenord")]
        public string Password { get; set; }

        [Display(Name = "Kom ihåg mig?")]
        public bool RememberMe { get; set; }
    }
}
