using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq.Expressions;
using System.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

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

            void assignValues(object theObject)
            {
                switch(theObject)
                {
                    case IDictionary<string, object> dictionary:
                        foreach (var pair in dictionary)
                        {
                            result.Add(pair);
                        }
                        break;
                    default:
                        foreach (var fi in theObject.GetType().GetProperties())
                        {
                            result[fi.Name] = fi.GetValue(theObject, null);
                        }
                        break;
                }
            }

            assignValues(item1);
            assignValues(item2);

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

        /*
         * FROM:
         * https://stackoverflow.com/a/3269649/8331979
         */
        private static bool TryFindMemberExpression(Expression exp, out MemberExpression memberExp)
        {
            memberExp = exp as MemberExpression;
            if (memberExp != null)
            {
                return true;
            }
            
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

        public static void SetPropertyValue<T, TValue>(this T target, Expression<Func<T, TValue>> memberLamda, TValue value)
        {
            if (memberLamda.Body is MemberExpression memberSelectorExpression)
            {
                var property = memberSelectorExpression.Member as PropertyInfo;
                if (property != null)
                {
                    property.SetValue(target, value, null);
                }
            }
        }

        public static Type GetDeclaringTypeFor<T, TValue>(Expression<Func<T, TValue>> expression)
        {
            var memberSelectorExpression = (MemberExpression)expression.Body;
            if (memberSelectorExpression != null)
            {
                var property = (PropertyInfo)memberSelectorExpression.Member;
                return property.DeclaringType!;
            }

            throw new Exception("Not reachable");
        }
    }

    public static class Utils
    {
        public static int Clamp(int value, int min, int max)
        {
            return System.Math.Min(max, System.Math.Max(min, value));
        }

        public static string DisplayName(Enum enumValue)
        {
            var member = enumValue.GetType().GetMember(enumValue.ToString()).First();
            return member.GetCustomAttribute<DisplayAttribute>().Name;
        }

        public static string DisplayName<T>(Expression<Func<T, object>> expression)
        {
            var memberExpression = expression as MemberExpression;
            if (memberExpression == null)
            {
                var unaryExpr = expression as UnaryExpression;
                if (unaryExpr != null && unaryExpr.NodeType == ExpressionType.Convert)
                {
                    memberExpression = unaryExpr.Operand as MemberExpression;
                }
            }

            if (memberExpression != null && memberExpression.Member.MemberType == MemberTypes.Property)
            {
                return memberExpression.Member.GetCustomAttribute<DisplayAttribute>().Name;
            }

            throw new ArgumentException(nameof(expression));
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