using System.Linq.Expressions;
using System.Reflection;

namespace PgsqlDataFlow
{
    public static class PropertyAccessors<T>
    {
        public static readonly Dictionary<string, Func<T, object>> Getters;
        static PropertyAccessors()
        {
            Getters = [];
            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T));
                MemberExpression accessor = Expression.Property(parameter, property);
                UnaryExpression convert = Expression.Convert(accessor, typeof(object));
                Func<T,object> getter = Expression.Lambda<Func<T,object>>(convert, parameter).Compile();

                Getters[property.Name] = getter;
            }
        }
    }
}
