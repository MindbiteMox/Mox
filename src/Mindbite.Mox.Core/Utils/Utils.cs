using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq.Expressions;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mindbite.Mox.Utils
{
    public static class Dynamics
    {
        public static dynamic Merge(object item1, object item2)
        {
            if (item1 == null || item2 == null)
                return item1 ?? item2 ?? new ExpandoObject();

            dynamic expando = new ExpandoObject();
            var result = expando as IDictionary<string, object>;
            foreach (System.Reflection.PropertyInfo fi in item1.GetType().GetProperties())
            {
                result[fi.Name] = fi.GetValue(item1, null);
            }

            foreach (System.Reflection.PropertyInfo fi in item2.GetType().GetProperties())
            {
                result[fi.Name] = fi.GetValue(item2, null);
            }
            return result;
        }

        /*
         * FROM:
         * https://stackoverflow.com/questions/18895977/how-to-dynamically-create-a-lambda-expression-that-contains-dot-notation
         */
        public static LambdaExpression GetLambdaExpression(Type type, IEnumerable<string> properties)
        {
            Type t = type;
            ParameterExpression parameter = Expression.Parameter(t);
            Expression expression = parameter;

            for (int i = 0; i < properties.Count(); i++)
            {
                expression = Expression.Property(expression, t, properties.ElementAt(i));
                t = expression.Type;
            }

            var lambdaExpression = Expression.Lambda(expression, parameter);
            return lambdaExpression;
        }

        /*
         * FROM:
         * https://stackoverflow.com/questions/3269518/get-the-property-name-used-in-a-lambda-expression-in-net-3-5
         */
        public static string GetFullPropertyName<T, TProperty>(Expression<Func<T, TProperty>> exp)
        {
            if (!TryFindMemberExpression(exp.Body, out MemberExpression memberExp))
                return string.Empty;

            var memberNames = new Stack<string>();
            do
            {
                memberNames.Push(memberExp.Member.Name);
            }
            while (TryFindMemberExpression(memberExp.Expression, out memberExp));

            return string.Join(".", memberNames.ToArray());
        }

        private static bool TryFindMemberExpression(Expression exp, out MemberExpression memberExp)
        {
            memberExp = exp as MemberExpression;
            if (memberExp != null)
            {
                // heyo! that was easy enough
                return true;
            }

            // if the compiler created an automatic conversion,
            // it'll look something like...
            // obj => Convert(obj.Property) [e.g., int -> object]
            // OR:
            // obj => ConvertChecked(obj.Property) [e.g., int -> long]
            // ...which are the cases checked in IsConversion
            if (IsConversion(exp) && exp is UnaryExpression)
            {
                memberExp = ((UnaryExpression)exp).Operand as MemberExpression;
                if (memberExp != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsConversion(Expression exp)
        {
            return exp.NodeType == ExpressionType.Convert || exp.NodeType == ExpressionType.ConvertChecked;
        }
    }

    public static class Utils
    {
        public static int Clamp(int value, int min, int max)
        {
            return System.Math.Min(max, System.Math.Max(min, value));
        }
    }

    public static class Strings
    {
        public static string Slugify(string s)
        {
            string str = RemoveAccent(s).ToLower();
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }

        private static string RemoveAccent(string s)
        {
            byte[] bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(s);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }
    }
}