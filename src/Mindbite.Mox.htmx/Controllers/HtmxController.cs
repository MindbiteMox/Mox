using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Htmx.Controllers
{
    [AllowAnonymous]
    public class HtmxController : Controller
    {
        private readonly Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine _viewEngine;

        public HtmxController(Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine viewEngine)
        {
            this._viewEngine = viewEngine;
        }

        public async Task<IActionResult> UpdateEditorTemplate(string prefix, string template)
        {
            static Type? GetGenericRazorPageType(Type t)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Microsoft.AspNetCore.Mvc.Razor.RazorPage<>))
                {
                    return t;
                }

                if (t.BaseType == null)
                {
                    return null;
                }

                return GetGenericRazorPageType(t.BaseType);
            }

            prefix ??= "";
            ModelState.Clear();
            ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

            var theView = this._viewEngine.GetView(template, template, false);
            var razorView = (Microsoft.AspNetCore.Mvc.Razor.RazorView)theView.View!;
            var modelType = GetGenericRazorPageType(razorView.RazorPage.GetType())?.GetGenericArguments().First();
            if (modelType == null)
            {
                throw new Exception($"View must have a model type");
            }

            var viewModel = Activator.CreateInstance(modelType);

            await TryUpdateModelAsync(viewModel, modelType, prefix);

            foreach (var key in ModelState.Keys)
            {
                ModelState.Remove(key);
            }

            return View(template, viewModel);
        }
    }
}
