﻿using Microsoft.AspNetCore.Authentication.Cookies;
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
            public string RemotePasswordAuthUrl { get; set; }
            public string RemotePasswordAuthDataFormatString { get; set; }
        }

        public class Hooks
        {
            internal List<Type> HookTypes { get; set; } = new List<Type>();

            public void Add<T>() where T: Services.UserChanges
            {
                HookTypes.Add(typeof(T));
            }
        }

        public class MagicLinkOptions
        {
            // In minutes
            public int ValidForMinutes { get; set; } = 5;
        }

        public BackdoorOptions Backdoor { get; set; }
        public Type DefaultUserType { get; set; }
        public Func<ActionContext, UI.DataTableSort, string, Task<UI.IDataTable>> UsersTable { get; set; }
        public List<Configuration.StaticIncludes.StaticFile> LoginStaticFiles { get; set; } = new List<Configuration.StaticIncludes.StaticFile>();
        public Hooks HookTypes { get; set; } = new Hooks();
        public MagicLinkOptions MagicLink { get; set; } = new MagicLinkOptions();
        public List<string> AdditionalAllowedStaticFileLocations { get; set; } = new List<string>();
        public string AdministratorGroupName { get; set; } = "Administratör";
    }

    public class SettingsOptions
    {
        internal interface ISettingsExtension
        {
            Task Save(string userId, object viewModel);
            Task<object> GetViewModel(string userId);
            Task<object> TryUpdateModel(Func<object, Type, Task<bool>> tryUpdateModel);
        }

        public abstract class SettingsExtension<T> : ISettingsExtension where T : class, new()
        {
            public abstract Task Save(string userId, object viewModel);
            public abstract Task<object> GetViewModel(string userId);
            public async Task<object> TryUpdateModel(Func<object, Type, Task<bool>> tryUpdateModel)
            {
                var model = new T();
                await tryUpdateModel(model, typeof(T));
                return model;
            }
        }

        public class View
        {
            public string TabTitle { get; set; }
            public string ViewName { get; set; }
            public Type ExtensionType { get; set; }
        }

        public List<View> AdditionalEditUserViews { get; set; } = new List<View>();
    }
}
