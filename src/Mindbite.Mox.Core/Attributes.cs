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

            if (!value.Equals(this._value))
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
        EditorOnly
    }

    public class MoxFormFieldTypeAttribute : Attribute
    {
        public Render Render { get; set; }
        public string EmptyLabel { get; set; }

        public MoxFormFieldTypeAttribute(Render render)
        {
            this.Render = render;
        }
    }
}
