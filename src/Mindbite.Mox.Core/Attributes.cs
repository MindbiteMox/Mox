using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SelectMenuAttribute : Attribute
    {
        public string MenuId { get; set; }

        public SelectMenuAttribute(string menuId)
        {
            this.MenuId = menuId;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
    public class MoxRequiredAttribute : ValidationAttribute
    {
        private readonly string _errorMessage = "{0} is required";
        public bool AllowEmptyStrings { get; set; }

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
    }
}
