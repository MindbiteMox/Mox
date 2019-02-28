using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Mindbite.Mox.Identity.ViewModels
{
    public class EditMyAccountViewModel : EditUserViewModel
    {
        public bool IsAdmin { get; set; }

        public EditMyAccountViewModel() : base() { }

        public EditMyAccountViewModel(IEnumerable<IdentityRole> roles, IEnumerable<string> rolesForUser, Data.Models.MoxUser user, bool hasPassword) : base(roles, rolesForUser, user, hasPassword)
        {
            this.IsAdmin = rolesForUser.Contains(Constants.AdminRole);
        }
    }
}
