using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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
            var errorMessage = (string)errorMessageField.GetValue(this);

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
    public sealed class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            filterContext.HttpContext.Response.Headers["Pragma"] = "no-cache";
            filterContext.HttpContext.Response.Headers["Expires"] = "0";

            base.OnResultExecuting(filterContext);
        }
    }

    public class MoxRequiredIfAttribute : MoxRequiredAttribute, IClientModelValidator
    {
        private readonly string _propertyName;

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
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var instance = validationContext.ObjectInstance;
            var type = instance.GetType();
            var property = type.GetProperty(this._propertyName);
            var propertyValue = property.GetValue(instance);

            if (propertyValue is bool && !(bool)propertyValue)
            {
                return ValidationResult.Success;
            }
            else if (propertyValue == GetDefault(property.PropertyType))
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
            else if (propertyValue != GetDefault(property.PropertyType))
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
}
