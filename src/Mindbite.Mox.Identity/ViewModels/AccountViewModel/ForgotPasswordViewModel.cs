using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Mindbite.Mox.Identity.ViewModels.AccountViewModel
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Din e-post")]
        public string Email { get; set; }
    }

    public class ResetViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Nytt lösenord")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        [Display(Name = "Upprepa nytt lösenord")]
        public string RepeatPassword { get; set; }

        [Required]
        public Guid ResetId { get; set; }
    }
}
