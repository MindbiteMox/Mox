using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Mindbite.Core.Mox.Data
{
    public static class Extensions
    {
        public static string FormatGuid(this Guid guid)
        {
            return guid.ToString().Replace("-", "");
        }

        public static DataTableBuilder<T> MakeDataTable<T>(this IEnumerable<T> data, params string[] fields)
        {
            return new DataTableBuilder<T>(data, fields);
        }

        public static string GetDescription(this Enum e)
        {
            FieldInfo fi = e.GetType().GetField(e.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return e.ToString();
            }
        }
    }
}
