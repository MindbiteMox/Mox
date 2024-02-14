using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Mindbite.Mox.Services
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string viewName, object model, IDictionary<string, object> viewData = null);
        Task<string> RenderToStringAsync(ActionContext actionContext, string viewName, object model, IDictionary<string, object> viewData = null);
        Task<string> RenderToStringAsync(ActionContext actionContext, string viewName, object model, ViewDataDictionary viewDataDictionary);
    }

    public class ViewRenderService : IViewRenderService
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public ViewRenderService(IRazorViewEngine razorViewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
        {
            this._razorViewEngine = razorViewEngine;
            this._tempDataProvider = tempDataProvider;
            this._serviceProvider = serviceProvider;
        }

        public Task<string> RenderToStringAsync(string viewName, object model, IDictionary<string, object> viewData = null)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return this.RenderToStringAsync(actionContext, viewName, model, viewData);
        }

        public async Task<string> RenderToStringAsync(ActionContext actionContext, string viewName, object model, IDictionary<string, object> viewData = null)
        {
            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), actionContext.ModelState ?? new ModelStateDictionary())
            {
                Model = model,
            };

            if (viewData != null)
            {
                foreach (var pair in viewData)
                {
                    viewDictionary[pair.Key] = pair.Value;
                }
            }

            return await RenderToStringAsync(actionContext, viewName, model, viewDataDictionary: viewDictionary);
        }

        public async Task<string> RenderToStringAsync(ActionContext actionContext, string viewName, object model, ViewDataDictionary viewDataDictionary)
        {
            using (var sw = new StringWriter())
            {
                var viewResult = _razorViewEngine.FindView(actionContext, viewName ?? actionContext.RouteData.Values["action"] as string, false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"{viewName} does not match any available view. \n Searched locations: {string.Join(", \n", viewResult.SearchedLocations)}");
                }

                viewDataDictionary.Model = model;

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDataDictionary,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }
    }
}