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
        public EditMyAccountViewModel() : base() { }

        public EditMyAccountViewModel(Data.Models.MoxUser user, bool hasPassword) : base(user, hasPassword)
        {
        }
    }
}
