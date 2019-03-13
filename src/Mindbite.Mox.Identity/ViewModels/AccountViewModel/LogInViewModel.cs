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

        [Display(Name = "Håll mig inloggad")]
        public bool RememberMe { get; set; } = true;
    }

    public class PasswordOrMagicLinkViewModel
    {
        public string Email { get; set; }
        public bool RememberMe { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class ShortCodeViewModel
    {
        [MoxRequired]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "E-post")]
        public string Email { get; set; }
        [MoxRequired]
        [Display(Name = "Autentiseringskod")]
        public string ShortCode { get; set; }
        public bool RememberMe { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class PasswordViewModel
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

        public bool RememberMe { get; set; }
    }

    public class MagicLinkEmailViewModel
    {
        public Guid MagicToken { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
        public string ShortCode { get; set; }
    }
}
