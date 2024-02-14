using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;

namespace Mindbite.Mox.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class SelectMenuAttribute : Attribute
    {
        public string MenuId { get; set; }

        public SelectMenuAttribute(string menuId)
        {
            this.MenuId = menuId;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
    public class MoxRequiredAttribute : ValidationAttribute, IClientModelValidator
    {
        public string DataAttributeKey = "data-val-required";
        public bool AllowEmptyStrings { get; set; }

        private readonly string _errorMessage = "{0} is required";

        public MoxRequiredAttribute() { }

        public MoxRequiredAttribute(string errorMessage)
        {
            this.ErrorMessage = errorMessage;
            this._errorMessage = ErrorMessage;
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(this._errorMessage, name);
        }

        public override bool RequiresValidationContext => true;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var localizer = (IStringLocalizer)validationContext.GetService(typeof(IStringLocalizer));

            if (value == null || (string.IsNullOrEmpty(value.ToString()) && !this.AllowEmptyStrings))
            {
                return new ValidationResult(localizer[this._errorMessage, validationContext.DisplayName]);
            }

            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            var localizer = (IStringLocalizer)context.ActionContext.HttpContext.RequestServices.GetService(typeof(IStringLocalizer));
            var errorMessageField = typeof(MoxRequiredAttribute).GetField("_errorMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var errorMessage = this.ErrorMessage ?? (string)errorMessageField.GetValue(this);

            AddAttribute(context.Attributes, "data-val", "true");
            var formattedErrorMessage = localizer[errorMessage, context.ModelMetadata.GetDisplayName()];
            AddAttribute(context.Attributes, this.DataAttributeKey, formattedErrorMessage);
        }

        protected void AddAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return;
            }
            attributes.Add(key, value);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class MoxNoCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            filterContext.HttpContext.Response.Headers["Pragma"] = "no-cache";
            filterContext.HttpContext.Response.Headers["Expires"] = "0";

            base.OnResultExecuting(filterContext);
        }
    }

    public class MoxRequiredIfEqualsAttribute : MoxRequiredAttribute, IClientModelValidator
    {
        private readonly string _propertyName;
        private readonly object _value;

        public MoxRequiredIfEqualsAttribute(string propertyName, object value) : base()
        {
            this._propertyName = propertyName;
            this._value = value;
            this.DataAttributeKey = "data-val-requiredifequals";
        }

        public new void AddValidation(ClientModelValidationContext context)
        {
            base.AddValidation(context);
            AddAttribute(context.Attributes, "data-val-requiredifequals-propertyname", this._propertyName);
            AddAttribute(context.Attributes, "data-val-requiredifequals-value", this._value?.ToString() ?? "");
        }

        private object GetPropertyValue(Type type, object instance, string propertyName)
        {
            var property = type.GetProperty(propertyName);
            return property.GetValue(instance);
        }

        protected override ValidationResult IsValid(object valueToValidate, ValidationContext validationContext)
        {
            var instance = validationContext.ObjectInstance;
            var type = instance.GetType();
            var value = GetPropertyValue(type, instance, this._propertyName);

            if (value == null || (value != null && this._value != null && !value.Equals(this._value)))
            {
                return ValidationResult.Success;
            }

            return base.IsValid(valueToValidate, validationContext);
        }
    }

    public class MoxRequiredIfAttribute : MoxRequiredAttribute, IClientModelValidator
    {
        private readonly string _propertyName;

        public string And { get; set; }
        public string AndNot { get; set; }

        public MoxRequiredIfAttribute(string propertyName) : base()
        {
            this._propertyName = propertyName;
            this.DataAttributeKey = "data-val-requiredif";
        }

        public MoxRequiredIfAttribute(string propertyName, string errorMessage) : base(errorMessage)
        {
            this._propertyName = propertyName;
            this.DataAttributeKey = "data-val-requiredif";
        }

        public new void AddValidation(ClientModelValidationContext context)
        {
            base.AddValidation(context);
            AddAttribute(context.Attributes, "data-val-requiredif-propertyname", this._propertyName);
            AddAttribute(context.Attributes, "data-val-requiredif-and", this.And);
            AddAttribute(context.Attributes, "data-val-requiredif-andnot", this.AndNot);
        }

        private bool EvaluateProperty(Type type, object instance, string propertyName)
        {
            var property = type.GetProperty(propertyName);
            var propertyValue = property.GetValue(instance);

            if (propertyValue is bool && (bool)propertyValue)
            {
                return true;
            }
            else if (!(propertyValue is bool) && propertyValue != GetDefault(property.PropertyType))
            {
                return true;
            }

            return false;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var instance = validationContext.ObjectInstance;
            var type = instance.GetType();
            var result = EvaluateProperty(type, instance, this._propertyName);

            if (!string.IsNullOrWhiteSpace(this.And))
            {
                result = result && EvaluateProperty(type, instance, this.And);
            }
            else if (!string.IsNullOrWhiteSpace(this.AndNot))
            {
                result = result && !EvaluateProperty(type, instance, this.AndNot);
            }

            if (!result)
            {
                return ValidationResult.Success;
            }

            return base.IsValid(value, validationContext);
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }

    public class MoxRequiredIfNotAttribute : MoxRequiredAttribute, IClientModelValidator
    {
        private readonly string _propertyName;

        public MoxRequiredIfNotAttribute(string propertyName) : base()
        {
            this._propertyName = propertyName;
            this.DataAttributeKey = "data-val-requiredifnot";
        }

        public MoxRequiredIfNotAttribute(string propertyName, string errorMessage) : base(errorMessage)
        {
            this._propertyName = propertyName;
            this.DataAttributeKey = "data-val-requiredifnot";
        }

        public new void AddValidation(ClientModelValidationContext context)
        {
            base.AddValidation(context);
            AddAttribute(context.Attributes, "data-val-requiredifnot-propertyname", this._propertyName);
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var instance = validationContext.ObjectInstance;
            var type = instance.GetType();
            var property = type.GetProperty(this._propertyName);
            var propertyValue = property.GetValue(instance);

            if (propertyValue is bool && (bool)propertyValue)
            {
                return ValidationResult.Success;
            }
            else if (!(propertyValue is bool) && propertyValue != GetDefault(property.PropertyType))
            {
                return ValidationResult.Success;
            }

            return base.IsValid(value, validationContext);
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }

    public class MoxCompareAttribute : CompareAttribute
    {
        public string If { get; set; }
        public string IfNot { get; set; }

        public override bool RequiresValidationContext => true;

        public MoxCompareAttribute(string compareProperty) : base(compareProperty)
        {
        }

        private bool EvaluateProperty(Type type, object instance, string propertyName)
        {
            var property = type.GetProperty(propertyName);
            var propertyValue = property.GetValue(instance);

            if (propertyValue is bool && (bool)propertyValue)
            {
                return true;
            }
            else if (!(propertyValue is bool) && propertyValue != GetDefault(property.PropertyType))
            {
                return true;
            }

            return false;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var instance = validationContext.ObjectInstance;
            var type = instance.GetType();
            var result = true;

            if (!string.IsNullOrWhiteSpace(this.If))
            {
                result = EvaluateProperty(type, instance, this.If);
            }
            else if (!string.IsNullOrWhiteSpace(this.IfNot))
            {
                result = !EvaluateProperty(type, instance, this.IfNot);
            }

            if (!result)
            {
                return ValidationResult.Success;
            }

            return base.IsValid(value, validationContext);
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }

    public enum Render
    {
        DropDown,
        CheckBoxList,
        Radio,
        EditorOnly,
        DivContainer,
        NoOutput
    }

    public class MoxFormFieldTypeAttribute : Attribute
    {
        public Render Render { get; set; }
        public string EmptyLabel { get; set; }
        public string DataSourcePropertyName { get; set; }

        public MoxFormFieldTypeAttribute(Render render)
        {
            this.Render = render;
        }
    }

    public class MoxFormFieldSetAttribute : Attribute
    {
        public string Name { get; set; }
        public int Order { get; set; }

        public MoxFormFieldSetAttribute(string name, [CallerLineNumber] int order = 0)
        {
            this.Name = name;
            this.Order = order;
        }
    }

#nullable enable

    [AttributeUsage(AttributeTargets.Property)]
    public class MoxFormDataSourceAttribute : Attribute
    {
        public string? EmptyMessage { get; set; }

        /// <summary>
        /// A static function on this class with the following signature: Task<IEnumerable<SelectListItem>> YourAsyncMethod(HttpContext context)
        /// </summary>
        public string SelectListFunctionName { get; set; }
        public string? EmptyMessageFunctionName { get; set; }

        public MoxFormDataSourceAttribute(string selectListFunctionName, string emptyMessage)
        {
            this.SelectListFunctionName = selectListFunctionName;
            this.EmptyMessage = emptyMessage;
        }

        public MoxFormDataSourceAttribute(string selectListFunctionName)
        {
            this.SelectListFunctionName = selectListFunctionName;
        }

        public static Type GetViewModelType(Type controllerType)
        {
            static Type? getFormClass(Type? t)
            {
                if (t == null || t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(Core.Controllers.FormController<>) || t.GetGenericTypeDefinition() == typeof(Core.Controllers.FormController<,,>)))
                {
                    return t;
                }

                return getFormClass(t.BaseType);
            }

            var formClass = getFormClass(controllerType);

            if (formClass == null)
            {
                throw new Exception($"No viewmodel could be found on controllertype {controllerType}");
            }

            return formClass!.GenericTypeArguments.First();
        }

        public async Task<IEnumerable<SelectListItem>> GetSelectListAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Type controllerType, Type? viewModelType = null)
        {
            viewModelType ??= GetViewModelType(controllerType);

            var method = viewModelType.GetMethod(this.SelectListFunctionName, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (method == null)
            {
                throw new Exception($"public static Task<IEnumerable<SelectListItem>> {this.SelectListFunctionName}(HttpContext) could not be found!");
            }

            if (method.GetParameters().FirstOrDefault()?.ParameterType != typeof(Microsoft.AspNetCore.Http.HttpContext))
            {
                throw new Exception($"{this.SelectListFunctionName} must be defined with exactly 1 parameter of type 'HttpContext'");
            }

            if (method.ReturnType != typeof(Task<IEnumerable<SelectListItem>>))
            {
                throw new Exception($"{this.SelectListFunctionName} must be defined with return type 'Task<IEnumerable<SelectListItem>>'");
            }

            var result = (Task<IEnumerable<SelectListItem>>)method.Invoke(null, new[] { httpContext })!;
            return await result;
        }

        public async Task<string?> GetEmptyMessageAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Type controllerType)
        {
            if (!string.IsNullOrWhiteSpace(this.EmptyMessageFunctionName))
            {
                var viewModelType = GetViewModelType(controllerType);
                var method = viewModelType.GetMethod(this.EmptyMessageFunctionName, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if (method == null)
                {
                    throw new Exception($"public static Task<string?> {this.EmptyMessageFunctionName}(HttpContext) could not be found!");
                }

                if (method.GetParameters().FirstOrDefault()?.ParameterType != typeof(Microsoft.AspNetCore.Http.HttpContext))
                {
                    throw new Exception($"{this.EmptyMessageFunctionName} must be defined with exactly 1 parameter of type 'HttpContext'");
                }

                if (method.ReturnType != typeof(Task<string?>))
                {
                    throw new Exception($"{this.EmptyMessageFunctionName} must be defined with return type 'Task<string?>'");
                }

                var result = (Task<string?>)method.Invoke(null, new[] { httpContext })!;
                return await result;
            }

            return this.EmptyMessage;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MoxFormBulkActionsAttribute : Attribute
    {
        public Type ActionsEnumType { get; set; }
        public string ActionName { get; set; }
        public string? EmptyString { get; set; } = "- Välj en massåtgärd -";
        public string? WarningMessageFormat { get; set; } = "{0} kommer köras på alla markerade rader ({1}).";

        public MoxFormBulkActionsAttribute(Type actionsEnumType, string actionName)
        {
            this.ActionsEnumType = actionsEnumType;
            this.ActionName = actionName;
        }

        public static MoxFormBulkActionsAttribute? GetViewModelAttribute(Type controllerType)
        {
            return MoxFormFilterAttribute.GetViewModelType(controllerType).GetCustomAttribute<MoxFormBulkActionsAttribute>();
        }
    }

    public enum MoxFormFilterType
    {
        Text,
        Dropdown,
        CustomEditor
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MoxFormFilterAttribute : Attribute
    {
        public MoxFormFilterType Type { get; set; }
        public string Name { get; set; }
        public string? Placeholder { get; set; }
        public int Order { get; set; } = 0;
        public bool SpacingAfter { get; set; } = false;
        public string? EditorTemplate { get; set; }

        /// <summary>
        /// A static function on this class with the following signature: Task<IEnumerable<SelectListItem>> YourAsyncMethod(HttpContext context)
        /// </summary>
        public string? GetSelectListFunction { get; set; }

        /// <summary>
        /// A static function on this class with the following signature: Task<bool> YourAsyncMethod(HttpContext context)
        /// </summary>
        public string? IsVisibleFunction { get; set; }

        public MoxFormFilterAttribute(MoxFormFilterType type, string name)
        {
            this.Type = type;
            this.Name = name;
        }

        public static Type GetViewModelType(Type controllerType)
        {
            static Type? getFormClass(Type? t)
            {
                if (t == null || t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(Core.Controllers.FormController<>) || t.GetGenericTypeDefinition() == typeof(Core.Controllers.FormController<,,>)))
                {
                    return t;
                }

                return getFormClass(t.BaseType);
            }

            var formClass = getFormClass(controllerType);

            return formClass!.GenericTypeArguments.First();
        }

        public async Task<IEnumerable<SelectListItem>> GetSelectListAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Type controllerType)
        {
            if (!string.IsNullOrWhiteSpace(this.GetSelectListFunction))
            {
                var viewModelType = GetViewModelType(controllerType);
                var method = viewModelType.GetMethod(this.GetSelectListFunction, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if(method == null)
                {
                    throw new Exception($"public static Task<IEnumerable<SelectListItem>> {nameof(this.GetSelectListFunction)}(HttpContext) could not be found!");
                }

                if(method.GetParameters().FirstOrDefault()?.ParameterType != typeof(Microsoft.AspNetCore.Http.HttpContext))
                {
                    throw new Exception($"{nameof(this.GetSelectListFunction)} must be defined with exactly 1 parameter of type 'HttpContext'");
                }

                if(method.ReturnType != typeof(Task<IEnumerable<SelectListItem>>))
                {
                    throw new Exception($"{nameof(this.GetSelectListFunction)} must be defined with return type 'Task<IEnumerable<SelectListItem>>'");
                }

                var result = (Task<IEnumerable<SelectListItem>>)method.Invoke(null, new[] { httpContext })!;
                return await result;
            }
            else if (this.Type == MoxFormFilterType.Dropdown)
            {
                throw new Exception($"{nameof(this.GetSelectListFunction)} must be defined when the filter type is '{MoxFormFilterType.Dropdown}'");
            }

            return Enumerable.Empty<SelectListItem>();
        }

        public static IEnumerable<MoxFormFilterAttribute> GetFilters(Type controllerType)
        {
            return GetViewModelType(controllerType).GetCustomAttributes<MoxFormFilterAttribute>().ToList();
        }

        public static async Task<IEnumerable<MoxFormFilterAttribute>> GetVisibleFiltersAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Type controllerType)
        {
            var allFilters = GetFilters(controllerType);
            var visibleFilters = new List<MoxFormFilterAttribute>();
            foreach (var filter in allFilters)
            {
                if (await filter.IsVisible(httpContext, controllerType))
                {
                    visibleFilters.Add(filter);
                }
            }

            return visibleFilters;
        }

        public async Task<bool> IsVisible(Microsoft.AspNetCore.Http.HttpContext httpContext, Type controllerType)
        {
            if (string.IsNullOrWhiteSpace(this.IsVisibleFunction))
            {
                return true;
            }

            var viewModelType = GetViewModelType(controllerType);
            var method = viewModelType.GetMethod(this.IsVisibleFunction, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (method == null)
            {
                throw new Exception($"public static Task<bool> {nameof(this.IsVisibleFunction)}(HttpContext) could not be found!");
            }

            if (method.GetParameters().FirstOrDefault()?.ParameterType != typeof(Microsoft.AspNetCore.Http.HttpContext))
            {
                throw new Exception($"{nameof(this.IsVisibleFunction)} must be defined with exactly 1 parameter of type 'HttpContext'");
            }

            if (method.ReturnType != typeof(Task<bool>))
            {
                throw new Exception($"{nameof(this.IsVisibleFunction)} must be defined with return type 'Task<bool>'");
            }

            var result = (Task<bool>)method.Invoke(null, new[] { httpContext })!;
            return await result;
        }
    }

    /// <summary>
    /// When applied to a controller or action method, this attribute checks if a POST request with a matching
    /// AntiForgeryToken has already been submitted recently (in the last minute), and redirects the request if so.
    /// If no AntiForgeryToken was included in the request, this filter does nothing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PreventDuplicateRequestsAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// The number of minutes that the results of POST requests will be kept in cache.
        /// </summary>
        private const int MinutesInCache = 1;
        private const int MinutesWaitTimeout = 10;

        public class Waiting
        {
            public DateTime Timeout { get; set; } = DateTime.Now.AddMinutes(MinutesWaitTimeout);
        }

        /// <summary>
        /// Checks the cache for an existing __RequestVerificationToken, and updates the result object for duplicate requests.
        /// Executes for every request.
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // Check if this request has already been performed recently
            var token = filterContext?.HttpContext?.Request?.Form["__RequestVerificationToken"];

            if (!string.IsNullOrEmpty(token))
            {
                var cache = filterContext!.HttpContext!.RequestServices.GetRequiredService<IMemoryCache>();

                var cacheEntry = cache.Get(token);

                while (cacheEntry is Waiting waiting && waiting.Timeout > DateTime.Now)
                {
                    System.Threading.Thread.Sleep(1000);
                    cacheEntry = cache.Get(token);
                }

                if (cacheEntry != null)
                {
                    // Optionally, assign an error message to discourage users from clicking submit multiple times (retrieve in the view using TempData["ErrorMessage"])
                    if (cacheEntry is RedirectToActionResult actionResult)
                    {
                        filterContext.Result = new RedirectToActionResult(actionResult.ActionName, actionResult.ControllerName, actionResult.RouteValues, actionResult.Permanent, actionResult.PreserveMethod, actionResult.Fragment);
                    }
                    else if (cacheEntry is ViewResult viewResult)
                    {
                        filterContext.Result = new ViewResult
                        {
                            ContentType = viewResult.ContentType,
                            StatusCode = viewResult.StatusCode,
                            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(viewResult.ViewData),
                            ViewEngine = viewResult.ViewEngine,
                            ViewName = viewResult.ViewName,
                            TempData = viewResult.TempData
                        };
                    }
                    else if (cacheEntry is PartialViewResult partialViewResult)
                    {
                        filterContext.Result = new PartialViewResult
                        {
                            ContentType = partialViewResult.ContentType,
                            StatusCode = partialViewResult.StatusCode,
                            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(partialViewResult.ViewData),
                            ViewEngine = partialViewResult.ViewEngine,
                            ViewName = partialViewResult.ViewName,
                            TempData = partialViewResult.TempData
                        };
                    }
                    else if (cacheEntry is JsonResult jsonResult)
                    {
                        filterContext.Result = new JsonResult(jsonResult.Value)
                        {
                            ContentType = jsonResult.ContentType,
                            StatusCode = jsonResult.StatusCode,
                            SerializerSettings = jsonResult.SerializerSettings,
                        };
                    }
                    else
                    {
                        // Provide a fallback in case the actual result is unavailable (redirects to controller index, assuming default routing behaviour)
                        filterContext.Result = new RedirectToActionResult(filterContext.RouteData.Values["Action"]?.ToString(), filterContext.RouteData.Values["Controller"]?.ToString(), filterContext.RouteData.Values);
                    }
                }
                else
                {
                    // Put the token in the cache, along with an arbitrary value (here, a timestamp)
                    cache.Set(token, new Waiting(), absoluteExpirationRelativeToNow: new TimeSpan(0, MinutesInCache, 0));
                }
            }
        }

        /// <summary>
        /// Adds the result of a completed request to the cache.
        /// Executes only for the first completed request.
        /// </summary>
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);

            var token = filterContext?.HttpContext?.Request?.Form["__RequestVerificationToken"];
            if (!string.IsNullOrEmpty(token))
            {
                var cache = filterContext!.HttpContext!.RequestServices.GetRequiredService<IMemoryCache>();
                cache.Set(token, filterContext.Result, absoluteExpirationRelativeToNow: new TimeSpan(0, MinutesInCache, 0));
            }
        }
    }
}
