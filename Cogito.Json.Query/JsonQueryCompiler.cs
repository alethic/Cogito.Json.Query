using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

using Cogito.Json.Query.Internal;
using Cogito.Reflection;

using Newtonsoft.Json.Linq;

namespace Cogito.Json.Query
{

    /// <summary>
    /// Builds expressions for a simple JSON query language.
    /// 
    /// https://github.com/clue/json-query-language
    /// </summary>
    public class JsonQueryCompiler
    {

        /// <summary>
        /// Describes a function that will return an expression that will navigate into a specified name of a source expression.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="name"></param>
        /// <param name="nav"></param>
        /// <returns></returns>
        public delegate Expression NavigateFunc(Expression expression, string name, NavigateFunc nav);

        /// <summary>
        /// Describes the combination of a target object and a query.
        /// </summary>
        struct TargetQuery :
            IEquatable<TargetQuery>
        {

            static readonly JTokenEqualityComparer JTokenEqualityComparer = new JTokenEqualityComparer();

            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="type"></param>
            /// <param name="filter"></param>
            public TargetQuery(Type type, JObject filter)
            {
                Type = type;
                Filter = filter;
            }

            /// <summary>
            /// Type to be filtered.
            /// </summary>
            public Type Type { get; set; }

            /// <summary>
            /// Filter applied to items of the type.
            /// </summary>
            public JObject Filter { get; set; }

            #region Comparable

            public override bool Equals(object obj)
            {
                return obj is TargetQuery && Equals((TargetQuery)obj);
            }

            public bool Equals(TargetQuery other)
            {
                return EqualityComparer<Type>.Default.Equals(Type, other.Type) && JTokenEqualityComparer.Equals(Filter, other.Filter);
            }

            public override int GetHashCode()
            {
                var hashCode = 1858949900;
                hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(Type);
                hashCode = hashCode * -1521134295 + JTokenEqualityComparer.GetHashCode(Filter);
                return hashCode;
            }

            #endregion

        }

        /// <summary>
        /// Generates an expression that invokes the indexer on the target with the given key.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetType"></param>
        /// <param name="key"></param>
        /// <param name="keyType"></param>
        /// <returns></returns>
        public static Expression InvokeIndexer(Expression target, Type targetType, Expression key, Type keyType)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (keyType == null)
                throw new ArgumentNullException(nameof(keyType));

            var method = targetType.GetMethod("get_Item", new[] { keyType });
            if (method == null)
                throw new InvalidOperationException($"Cannot find indexer method on {targetType}.");

            return Expression.Call(target, method, key);
        }

        /// <summary>
        /// Navigates to the parent <see cref="JObject"/> as expressed by the '&lt;' path syntax.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="name"></param>
        /// <param name="navigate"></param>
        /// <returns></returns>
        public static Expression JTokenParentNavigateFunc(Expression expression, string name, NavigateFunc navigate)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name != "<")
                return null;

            if (typeof(JToken).IsAssignableFrom(expression.Type) == false)
                throw new NotSupportedException("Unable to use root navigation on non-JToken.");

            // parent of incoming token
            var parent = Expression.Condition(
                Expression.ReferenceEqual(expression, Expression.Constant(null, expression.Type)),
                Expression.Constant(null, typeof(JContainer)),
                Expression.Property(expression, nameof(JToken.Parent)));

            // step up to object if on property
            return Expression.Condition(
                Expression.TypeIs(parent, typeof(JProperty)),
                Expression.Property(parent, nameof(JProperty.Parent)),
                parent);
        }

        /// <summary>
        /// Navigates to the root <see cref="JToken"/> of the object as expressed by the '^' path syntax.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="name"></param>
        /// <param name="navigate"></param>
        /// <returns></returns>
        public static Expression JTokenRootNavigateFunc(Expression expression, string name, NavigateFunc navigate)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name != "^")
                return null;

            if (typeof(JToken).IsAssignableFrom(expression.Type) == false)
                throw new NotSupportedException("Unable to use root navigation on non-JToken.");

            return Expression.Condition(
                Expression.ReferenceEqual(expression, Expression.Constant(null, expression.Type)),
                Expression.Constant(null, typeof(JToken)),
                Expression.Property(expression, nameof(JToken.Root)));
        }

        /// <summary>
        /// Default NavigateFunc. Handles standard .NET properties and <see cref="JToken"/> traversal.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="name"></param>
        /// <param name="navigate"></param>
        /// <returns></returns>
        public static Expression DefaultNavigateFunc(Expression expression, string name, NavigateFunc navigate)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // returns the nullable version of the type
            Type NullableOfType(Type type) =>
                type.IsValueType ? typeof(Nullable<>).MakeGenericType(type) : type;

            // returns a nullable version of the given type
            Expression NullableExpressionOfType(Type type) =>
                Expression.Default(NullableOfType(type));

            // ensure the result is nullable
            Expression NullableExpression(Expression expr) =>
                Expression.Convert(expr, NullableOfType(expr.Type));

            // resolve member
            var member = expression.Type.GetPropertyOrField(name);

            // member is a property
            if (member is PropertyInfo property)
                return Expression.Condition(
                    Expression.Equal(expression, NullableExpressionOfType(expression.Type)),
                    NullableExpressionOfType(property.PropertyType),
                    NullableExpression(Expression.Property(expression, property)));

            // member is a field
            if (member is FieldInfo field)
                return Expression.Condition(
                    Expression.Equal(expression, NullableExpressionOfType(expression.Type)),
                    NullableExpressionOfType(field.FieldType),
                    NullableExpression(Expression.Field(expression, field)));

            // source expression implements a generic dictionary
            if (typeof(IEnumerable).IsAssignableFrom(expression.Type) && // early breakout
                expression.Type.GetAssignableTypes().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)) is Type iface)
            {
                // invoke get_Item on dictionary, with key type as described by interface
                // conditional is used to prevent index missing exceptions
                var keyType = iface.GetGenericArguments()[0];
                var valType = iface.GetGenericArguments()[1];

                // if key type is supported, use it, else try to change type
                var keyExpr = Expression.Convert(
                    keyType.IsInstanceOfType(name) ?
                        (Expression)Expression.Constant(name, keyType) :
                        Expression.Call(typeof(Convert).GetMethod(nameof(Convert.ChangeType)), Expression.Constant(name), Expression.Constant(keyType)),
                    keyType);

                // if null, return nullable; else invoke ContainsKey before trying get_Item; convert result to nullable
                return Expression.Condition(
                    Expression.Equal(expression, Expression.Constant(null, expression.Type)),
                    NullableExpressionOfType(valType),
                    Expression.Condition(
                        Expression.Call(expression, iface.GetMethod("ContainsKey", new[] { keyType }), keyExpr),
                        NullableExpression(InvokeIndexer(expression, iface, keyExpr, keyType)),
                        NullableExpressionOfType(valType)));
            }

            // no way to navigate found, use untyped null
            return NullableExpressionOfType(typeof(object));
        }

        /// <summary>
        /// Supports navigation into <see cref="JObject"/> return types.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="name"></param>
        /// <param name="navigate"></param>
        /// <returns></returns>
        public static Expression JObjectNavigateFunc(Expression expression, string name, NavigateFunc navigate)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // source expression is a JObject
            if (typeof(JObject).IsAssignableFrom(expression.Type))
                return Expression.Condition(
                    Expression.ReferenceEqual(expression, Expression.Constant(null, expression.Type)),
                    Expression.Constant(JValue.CreateUndefined(), typeof(JToken)),
                    InvokeIndexer(expression, typeof(JObject), Expression.Constant(name), typeof(string)));

            return null;
        }

        /// <summary>
        /// Supports navigation into <see cref="JValue"/> return types.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="name"></param>
        /// <param name="navigate"></param>
        /// <returns></returns>
        public static Expression JValueNavigateFunc(Expression expression, string name, NavigateFunc navigate)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // cannot navigate into any JValue
            if (typeof(JValue).IsAssignableFrom(expression.Type))
                return Expression.Constant(JValue.CreateUndefined());

            return null;
        }

        /// <summary>
        /// Supports navigation into <see cref="JArray"/> return types.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="name"></param>
        /// <param name="navigate"></param>
        /// <returns></returns>
        public static Expression JArrayNavigateFunc(Expression expression, string name, NavigateFunc navigate)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // cannot navigate into any JArray
            if (typeof(JArray).IsAssignableFrom(expression.Type))
                return Expression.Constant(JValue.CreateUndefined());

            return null;
        }

        /// <summary>
        /// Supports navigation into <see cref="JToken"/> return types.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="name"></param>
        /// <param name="navigate"></param>
        /// <returns></returns>
        public static Expression JTokenNavigateFunc(Expression expression, string name, NavigateFunc navigate)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (navigate == null)
                throw new ArgumentNullException(nameof(navigate));

            // source expression is a JToken; it might really be a JObject
            if (typeof(JToken).IsAssignableFrom(expression.Type))
                return Expression.Condition(
                    Expression.TypeEqual(expression, typeof(JObject)),
                    navigate(Expression.Convert(expression, typeof(JObject)), name, navigate),
                    Expression.Constant(JValue.CreateUndefined(), typeof(JToken)));

            return null;
        }

        /// <summary>
        /// Cache of compiled JQL filters against specific types.
        /// </summary>
        static ConcurrentWeakDictionary<TargetQuery, Delegate> delegateCache =
            new ConcurrentWeakDictionary<TargetQuery, Delegate>();

        /// <summary>
        /// Gets or creates a new cached delegate function.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        static Delegate GetOrCreateDelegate<TSource>(JObject filter)
        {
            // null filter always returns true
            if (filter == null)
                return (Func<TSource, bool>)(o => true);

            // otherwise find in cache or create and add to cache
            return delegateCache.GetOrAdd(new TargetQuery(typeof(TSource), filter), f =>
            {
                var d = new JsonQueryCompiler();
                var p = Expression.Parameter(f.Type);
                var a = d.Predicate(p, f.Filter);
                var m = a.Compile();
                return m;
            });
        }

        /// <summary>
        /// Creates a function that executes the given filter against the specified source type.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static Func<TSource, bool> PredicateFunc<TSource>(JObject filter)
        {
            if (filter == null)
                return o => true;

            // close over delegate
            var d = GetOrCreateDelegate<TSource>(filter);
            return o => (bool)d.DynamicInvoke(o);
        }

        /// <summary>
        /// Returns <c>true</c> if the given target matches the specified filter.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static bool Matches<TSource>(TSource target, JObject filter)
        {
            return filter != null ? PredicateFunc<TSource>(filter)(target) : true;
        }

        /// <summary>
        /// Functions that handle property navigation for explicite types.
        /// </summary>
        public List<NavigateFunc> NavigateFuncs { get; } = new List<NavigateFunc>()
        {
            JTokenParentNavigateFunc,
            JTokenRootNavigateFunc,
            JValueNavigateFunc,
            JArrayNavigateFunc,
            JObjectNavigateFunc,
            JTokenNavigateFunc,

            DefaultNavigateFunc,
        };

        /// <summary>
        /// Builds a predicate lambda expression for the given <paramref name="target"/>.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public LambdaExpression Predicate(ParameterExpression target, JObject filter)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            return Expression.Lambda(
                typeof(Func<,>).MakeGenericType(target.Type, typeof(bool)),
                Build(target, filter),
                target);
        }

        /// <summary>
        /// Generates a LINQ <see cref="Expression"/> rooted at the specified <paramref name="target"/>.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public Expression Build(Expression target, JObject filter)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            var e = (Expression)null;

            foreach (var p in filter.Properties())
            {
                var e2 = Build(target, p.Name, p.Value);
                e = e != null ? Expression.AndAlso(e, e2) : e2;
            }

            if (e == null)
                e = Expression.Constant(true);

            return e;
        }

        Expression Build(Expression target, string key, JToken value)
        {
            // negation operation
            if (key.StartsWith("!"))
                return Expression.Not(Build(target, key.Substring(1), value));

            // comparator operation
            if (key.StartsWith("$"))
                return BuildComparator(target, key.Substring(1), value);

            // standard property specification
            return BuildProperty(target, key, value);
        }

        Expression BuildProperty(Expression target, string key, JToken filter)
        {
            return BuildProperty(target, Regex.Split(key, @"(?<!\\)\."), filter);
        }

        Expression BuildProperty(Expression target, string[] path, JToken filter)
        {
            // navigate into the specified path
            target = Navigate(target, path);

            switch (filter.Type)
            {
                case JTokenType.Boolean:
                case JTokenType.Float:
                case JTokenType.Integer:
                case JTokenType.Null:
                case JTokenType.String:
                    return BuildIs(target, filter);
                case JTokenType.Array:
                    return BuildIn(target, filter);
                case JTokenType.Object:
                    return Build(target, (JObject)filter);
                default:
                    throw new InvalidOperationException();
            }
        }

        Expression BuildComparator(Expression target, string comparator, JToken filter)
        {
            switch (comparator)
            {
                case "is":
                    return BuildIs(target, filter);
                case "in":
                    return BuildIn(target, filter);
                case "contains":
                    return BuildContains(target, filter);
                case "lt":
                    return BuildLessThan(target, filter);
                case "gt":
                    return BuildGreaterThan(target, filter);
                case "lte":
                    return BuildLessThanOrEqual(target, filter);
                case "gte":
                    return BuildGreaterThanOrEqual(target, filter);
                case "not":
                    return BuildNot(target, filter);
                case "and":
                    return BuildAnd(target, filter);
                case "or":
                    return BuildOr(target, filter);
                default:
                    throw new NotSupportedException($"Unsupported comparator: {comparator}.");
            }
        }

        /// <summary>
        /// Builds an 'is' comparator.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Expression BuildIs(Expression target, JToken filter)
        {
            var t = JTokenToType(filter);
            if (t == null)
                t = target.Type;

            return Expression.Equal(
                Expression.Convert(
                    target,
                    t),
                Expression.Convert(
                    Expression.Constant(JTokenToValue(filter)),
                    t));
        }

        /// <summary>
        /// Builds an 'in' comparator.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Expression BuildIn(Expression target, JToken filter)
        {
            var t = JTokenToType(filter);
            if (t == null || t != typeof(Array))
                throw new ArgumentException("Unsupported JToken type for 'in' expression.");

            return Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Contains),
                new[] { typeof(object) },
                Expression.Constant(JTokenToValue(filter), typeof(IEnumerable<object>)),
                Expression.Convert(target, typeof(object)));
        }

        /// <summary>
        /// Builds an 'contains' comparator.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Expression BuildContains(Expression target, JToken filter)
        {
            var t = JTokenToType(filter);
            if (t == null)
                t = target.Type;

            if (t != typeof(string))
                throw new NotImplementedException("Unsuported filter operator 'contains' on non-string type.");

            return Expression.Call(
                Expression.Convert(
                    target,
                    t),
                nameof(string.Contains),
                new Type[0],
                Expression.Convert(
                    Expression.Constant(JTokenToValue(filter)),
                    t));
        }

        /// <summary>
        /// Builds an 'lt' comparator.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Expression BuildLessThan(Expression target, JToken filter)
        {
            var t = JTokenToType(filter);
            if (t == null)
                t = target.Type;

            return Expression.LessThan(
                Expression.Convert(
                    target,
                    t),
                Expression.Convert(
                    Expression.Constant(JTokenToValue(filter)),
                    t));
        }

        /// <summary>
        /// Builds an 'gt' comparator.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Expression BuildGreaterThan(Expression target, JToken filter)
        {
            var t = JTokenToType(filter);
            if (t == null)
                t = target.Type;

            return Expression.GreaterThan(
                Expression.Convert(
                    target,
                    t),
                Expression.Convert(
                    Expression.Constant(JTokenToValue(filter)),
                    t));
        }

        /// <summary>
        /// Builds an 'lte' comparator.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Expression BuildLessThanOrEqual(Expression target, JToken filter)
        {
            var t = JTokenToType(filter);
            if (t == null)
                t = target.Type;

            return Expression.LessThanOrEqual(
                Expression.Convert(
                    target,
                    t),
                Expression.Convert(
                    Expression.Constant(JTokenToValue(filter)),
                    t));
        }

        /// <summary>
        /// Builds an 'gte' comparator.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Expression BuildGreaterThanOrEqual(Expression target, JToken filter)
        {
            var t = JTokenToType(filter);
            if (t == null)
                t = target.Type;

            return Expression.GreaterThanOrEqual(
                Expression.Convert(
                    target,
                    t),
                Expression.Convert(
                    Expression.Constant(JTokenToValue(filter)),
                    t));
        }

        /// <summary>
        /// Builds an 'not' comparator.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Expression BuildNot(Expression target, JToken filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Builds an 'and' comparator.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Expression BuildAnd(Expression target, JToken filter)
        {
            if (filter.Type != JTokenType.Array)
                throw new NotImplementedException("'and' operator must be a JSON array.");

            var e = (Expression)Expression.Constant(true);

            foreach (var o in filter.OfType<JObject>())
                e = Expression.AndAlso(e, Build(target, o));

            return e;
        }

        /// <summary>
        /// Builds an 'or' comparator.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Expression BuildOr(Expression target, JToken filter)
        {
            if (filter.Type != JTokenType.Array)
                throw new NotImplementedException("'and' operator must be a JSON array.");

            var e = (Expression)Expression.Constant(false);

            foreach (var o in filter.OfType<JObject>())
                e = Expression.OrElse(e, Build(target, o));

            return e;
        }

        /// <summary>
        /// Navigates into the given property path.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        Expression Navigate(Expression target, string[] path)
        {
            foreach (var p in path)
                target = Navigate(target, p);

            return target;
        }

        /// <summary>
        /// Navigates into the specified property name by invoking each navigate func in order until one returns an
        /// expression.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Expression Navigate(Expression target, string name)
        {
            return NavigateFuncs
                .Select(i => i(target, name, (a, b, c) => Navigate(a, b)))
                .FirstOrDefault(i => i != null);
        }

        /// <summary>
        /// Returns the native value for the given <see cref="JToken"/>.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        object JTokenToValue(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return (bool)token;
                case JTokenType.Float:
                    return (float)token;
                case JTokenType.Integer:
                    return (long)token;
                case JTokenType.Null:
                    return null;
                case JTokenType.String:
                    return (string)token;
                case JTokenType.Array:
                    return ((JArray)token).Select(i => JTokenToValue(i)).ToArray();
                case JTokenType.Object:
                    return token;
                default:
                    throw new NotSupportedException("Unsupported JToken type.");
            }
        }

        /// <summary>
        /// Extracts the .NET type for a given <see cref="JToken"/>.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Type JTokenToType(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return typeof(bool?);
                case JTokenType.Float:
                    return typeof(float?);
                case JTokenType.Integer:
                    return typeof(long?);
                case JTokenType.String:
                    return typeof(string);
                case JTokenType.Array:
                    return typeof(Array);
                case JTokenType.Null:
                    return null;
                default:
                    throw new NotSupportedException("Unsupported JToken type.");
            }
        }

    }
}
