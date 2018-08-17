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
        public bool AllowEmptyStrings { get; set; }

        public override bool RequiresValidationContext => true;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var localizer = (IStringLocalizer)validationContext.GetService(typeof(IStringLocalizer));

            if (value == null || (string.IsNullOrEmpty(value.ToString()) && !this.AllowEmptyStrings))
            {
                // TODO: allow custom error message via constructor
                return new ValidationResult(localizer["{0} is required", validationContext.DisplayName]);
            }

            return ValidationResult.Success;
        }
    }
}
