using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Mindbite.Mox.Identity.Data;
using Mindbite.Mox.Identity.Data.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Mindbite.Mox.Utils.FileProviders;
using Microsoft.AspNetCore.Identity;
using Mindbite.Mox.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace Mindbite.Mox.Identity
{
    public class Localization { }

    public class MoxIdentityOptions
    {
        public class BackdoorOptions
        {
            public bool UseBackdoor { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
        }

        public BackdoorOptions Backdoor { get; set; }
    }

    public class MoxUserManager : UserManager<MoxUser>
    {
        public MoxUserManager(IUserStore<MoxUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<MoxUser> passwordHasher, IEnumerable<IUserValidator<MoxUser>> userValidators, IEnumerable<IPasswordValidator<MoxUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<MoxUser>> logger) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
        }
    }
}
