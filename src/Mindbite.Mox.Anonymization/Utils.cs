using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Reflection;

namespace Mindbite.Mox.Anonymization.Utils
{
    public static class AnonymizationUtils
    {
        public static void AnonymizePersonalDataProperties(object o, Func<PropertyInfo, object?>? getAnonymizedValue)
        {
            var properties = o.GetType()
                .GetProperties(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CustomAttributes.Any(x => x.AttributeType == typeof(PersonalDataAttribute)))
                .ToList();

            foreach (var property in properties)
            {
                if (getAnonymizedValue != null)
                {
                    property.SetValue(o, getAnonymizedValue(property));
                }
                else
                {
                    if (property.PropertyType == typeof(string))
                    {
                        property.SetValue(o, "ANONYMIZED");
                    }
                    else
                    {
                        throw new NotImplementedException($"Anonymizing values of type {property.PropertyType.Name} isn't implemented. Implement it yourself by setting AnonymizationOptions.GetAnonymizedValue");
                    }
                }
            }
        }
    }
}
