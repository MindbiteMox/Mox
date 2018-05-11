using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Mindbite.Core.Mox.Data
{
    public static class SqlFormatter
    {
        /// <summary>
        /// Usage:
        ///     dalConn.Fill(FormatSQL($"SELECT * FROM Table WHERE Field='{Value}'"), dt);
        ///     Instead of 
        ///     dalConn.Fill("SELECT * FROM Table WHERE Field='" + TextHelper.SecureInput(Value) + "'", dt);
        /// </summary>
        /// <param name="sql">SQL query with string.Format tokens</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>Cleaned sql string, uses TextHelper.SecureInput for cleaning</returns>
        public static string FormatSQL(FormattableString sql)
        {
            return sql.ToString(new SqlFormatProvider());
        }

        public static string FormatSQL(string sql, params object[] parameters)
        {
            return string.Format(sql, parameters.Select(x => TextHelper.SecureInput(x.ToString())));
        }
    }

    class SqlFormatProvider : IFormatProvider
    {
        private readonly SqlFormatter _formatter = new SqlFormatter();

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return _formatter;
            return null;
        }

        class SqlFormatter : ICustomFormatter
        {
            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                if (arg is IFormattable)
                    return TextHelper.SecureInput(((IFormattable)arg).ToString(format, CultureInfo.InvariantCulture));
                return TextHelper.SecureInput(arg.ToString());
            }
        }
    }

    internal class TextHelper
    {
        internal static string SecureInput(string input)
        {
            return (input + "").Replace("'", "''");
        }
    }
}
