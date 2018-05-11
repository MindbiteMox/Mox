using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace Mindbite.Core.Mox.Data
{
    public class DataTableBuilder<T>
    {
        public IEnumerable<T> Data { get; private set; }
        public List<KeyValuePair<string, Func<T, object>>> AdditionalFields { get; private set; }
        public string[] Fields { get; private set; }
        public DataTableBuilder(IEnumerable<T> data, params string[] fields)
        {
            this.Data = data;
            this.Fields = fields ?? new string[0];
            this.AdditionalFields = new List<KeyValuePair<string, Func<T, object>>>();
        }

        public DataTableBuilder<T> AddCalculatedField(string name, Func<T, object> func)
        {
            this.AdditionalFields.Add(new KeyValuePair<string, Func<T, object>>(name, func));
            return this;
        }

        public DataTable ToDataTable()
        {
            var properties = TypeDescriptor.GetProperties(typeof(T)).Cast<PropertyDescriptor>().Where(x => this.Fields.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
            DataTable table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
            {
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (var field in this.AdditionalFields)
            {
                table.Columns.Add(field.Key, typeof(object));
            }

            foreach (T item in this.Data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;

                foreach (var field in this.AdditionalFields)
                    row[field.Key] = field.Value(item);

                table.Rows.Add(row);
            }

            return table;
        }
    }

}
