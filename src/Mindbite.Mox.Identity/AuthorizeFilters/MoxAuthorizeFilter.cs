using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.AuthorizeFilters
{
    public class MoxAuthorizeFilter : AuthorizeFilter
    {
        private string MoxPath;
        public MoxAuthorizeFilter(AuthorizationPolicy policy, string moxPath) : base(policy)
        {
            this.MoxPath = moxPath;
        }

        public override Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if(context.HttpContext.Request.Path.StartsWithSegments($"/{MoxPath.TrimStart('/')}"))
            {
                return base.OnAuthorizationAsync(context);
            }
            //var route = context.RouteData.Routers.FirstOrDefault(x => x is Route) as Route;
            //if (route?.RouteTemplate.ToLower().TrimStart('/').StartsWith(MoxPath.ToLower()) ?? false)
            //{
            //}
            return Task.CompletedTask;
        }
    }
}
