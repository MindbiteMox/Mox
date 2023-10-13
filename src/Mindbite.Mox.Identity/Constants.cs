using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity
{
    public static class Constants
    {
        public const string AdminRole = "MoxIdentity/Admin";
        //public const string EditMyOwnAccountRole = "MoxIdentity/MyAccount";

        public const string SettingsArea = "IdentitySettings";
        public const string SettingsAppId = "MoxSettings";
        public const string SettingsAppName = "Inställningar";

        public const string MoxUserNameClaimType = "MoxUserName";
        public const string MoxUserRoleGroupNameClaimType = "MoxUserRoleGroupName";
    }
}
