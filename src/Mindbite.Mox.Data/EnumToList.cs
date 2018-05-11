using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mindbite.Mox.Data
{
    public class EnumToList<T> : List<EnumKeyValuePair>
    {

        public EnumToList()
        {

            var enumerations = Enum.GetValues(typeof(T)).Cast<Enum>().ToList();


            foreach (var enumItem in enumerations)
            {
                this.Add(new EnumKeyValuePair() { Key = GetEnumDescription(enumItem), Value = Convert.ToInt32(enumItem) });
            }
        }

        private static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return value.ToString();
            }
        }
    }

    public class EnumKeyValuePair
    {
        public string Key { get; set; }
        public int Value { get; set; }
    }
}
