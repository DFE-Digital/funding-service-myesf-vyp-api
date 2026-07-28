using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using static System.Linq.Expressions.Expression;

namespace ViewYourPayments.Infrastructure.Extensions
{
    public static class EfExtensions
    {
        /// <summary>
        /// Extension method to handle null and return list object.
        /// </summary>
        /// <typeparam name="TSource">Expected type of return object.</typeparam>
        /// <param name="source">Query object.</param>
        /// <returns>List object.</returns>
        public async static Task<List<TSource>> ToListAsyncSafe<TSource>(
          this IQueryable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (!(source is IAsyncEnumerable<TSource>))
                return source.ToList();
            return await source.ToListAsync();
        }

        /// <summary>
        /// Appends existing query with like search including wild card.
        /// </summary>
        /// <typeparam name="T">Type of expected object.</typeparam>
        /// <param name="query">Query object where you want to append.</param>
        /// <param name="propertyName">Column/property name on which you want to add new condition.</param>
        /// <param name="searchTerms">Search terms as array of string.</param>
        /// <returns>Returns updated query.</returns>
        public static IQueryable<T> GetWildcardQueryWithMulitpleSearchTerms<T>(this IQueryable<T> query, string propertyName, params string[] searchTerms)
        {
            if (!searchTerms.Any() || string.IsNullOrEmpty(propertyName)) { return query; }

            // T is a compile-time placeholder for the element type of the query.
            Type elementType = typeof(T);

            // Get the string property for the given property name.
            PropertyInfo property = elementType.GetProperty(propertyName);
            if (property == null || property.PropertyType != typeof(string)) { return query; }

            // Get the right overload of String.Contains
            MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;

            // Create a parameter for the expression tree:
            // the 'x' in 'x => x.PropertyName.Contains("term")'
            // The type of this parameter is the query's element type
            ParameterExpression prm = Parameter(elementType);

            // Map each property to an expression tree node
            IEnumerable<Expression> expressions = searchTerms
                .Select(term =>
                    // For each search term, we have to construct an expression tree node like x.PropertyName.Contains("term")
                    Call(                  // .Contains(...) 
                        Property(          // .PropertyName
                            prm,           // x 
                            property
                        ),
                        containsMethod,
                        Constant(term)     // "term" 
                    )
                );

            // Combine all the resultant expression nodes using ||
            Expression body = expressions
                .Aggregate(
                    (prev, current) => Or(prev, current)
                );

            // Wrap the expression body in a compile-time-typed lambda expression
            Expression<Func<T, bool>> lambda = Lambda<Func<T, bool>>(body, prm);

            // Because the lambda is compile-time-typed (albeit with a generic parameter), we can use it with the Where method
            return query.Where(lambda);
        }

        /// <summary>
        /// Add order to existing query. 
        /// </summary>
        /// <typeparam name="T">Type of expected object. </typeparam>
        /// <param name="source">Query object where you want to add order by.</param>
        /// <param name="property">Column/property name on which you want to apply order by.</param>
        /// <param name="isAscending">Boolean value for IsAscending.</param>
        /// <param name="isPrimaryOrder">Boolean value to flag is this first order by call in this query.</param>
        /// <returns>Returns updated query.</returns>
        public static IOrderedQueryable<T> ApplyOrder<T>(this IQueryable<T> source, string property, bool isAscending, bool isPrimaryOrder)
        {
            string[] props = property.Split('.');
            Type type = typeof(T);
            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;
            foreach (string prop in props)
            {
                PropertyInfo pi = type.GetProperty(prop);
                if (pi == null)
                    pi = type.GetProperties().FirstOrDefault(x => x.Name.ToLower() == prop.ToLower());
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }
            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            LambdaExpression lambda = Expression.Lambda(delegateType, expr, arg);
            string methodName = null;

            if (!isPrimaryOrder && source is IOrderedQueryable<T>)
            {
                if (isAscending)
                    methodName = "ThenBy";
                else
                    methodName = "ThenByDescending";
            }
            else
            {
                if (isAscending)
                    methodName = "OrderBy";
                else
                    methodName = "OrderByDescending";
            }

            object result = typeof(Queryable).GetMethods().Single(
                method => method.Name == methodName
                && method.IsGenericMethodDefinition
                && method.GetGenericArguments().Length == 2
                && method.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), type)
                .Invoke(null, new object[] { source, lambda });
            return (IOrderedQueryable<T>)result;
        }
    }
}
