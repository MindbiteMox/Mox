using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Mindbite.Mox.Extensions;

namespace Mindbite.Mox.Identity.ViewModels
{
    public class EditMyAccountViewModel : EditUserViewModel
    {
        public bool IsAdmin { get; set; }

        public EditMyAccountViewModel() : base() { }

        public EditMyAccountViewModel(IEnumerable<IdentityExtensions.RoleTreeNode> roles, IEnumerable<string> preselectedRoles, Data.Models.MoxUser user, bool hasPassword, bool disableRoles, string rolesDisabledLink) : base(roles, preselectedRoles, user, hasPassword, disableRoles, rolesDisabledLink)
        {
            this.IsAdmin = preselectedRoles.Contains(Constants.AdminRole);
        }
    }
}
