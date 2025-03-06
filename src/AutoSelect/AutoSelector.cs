using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoSelect;

public static class AutoSelector<TSource, TDest>
    where TDest : class, new()
{
    private static Expression<Func<TSource, TDest>>? map;

    /// <summary>
    /// Creates an expression that maps from the source type to the destination type.
    /// </summary>
    /// <returns>
    /// An expression that assigns values from the source to a new instance of the destination type.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the mapper unexpectedly returns null.</exception>
    public static Expression<Func<TSource, TDest>> Map()
    {
        if (map is not null)
        {
            return map;
        }
        var source = Expression.Parameter(typeof(TSource));
        var init = BuildMap(source, typeof(TDest))
            ?? throw new InvalidOperationException("The map builder unexpectedly returned null.");
        return map = Expression.Lambda<Func<TSource, TDest>>(init, source);
    }

    private static MemberInitExpression? BuildMap(
        Expression source,
        Type destination,
        int depth = 0)
    {
        // To prevent infinite recursion for self-referencing classes, we include a depth stop
        if (depth > 8)
        {
            return null;
        }
        var properties = destination.GetProperties();
        var bindings = new List<MemberBinding>();
        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<NotMappedAttribute>() is not null)
            {
                // Ignore properties decorated with [NotMapped]
                continue;
            }
            // Determine the path we need to select
            var path = property.GetCustomAttribute<MapFromAttribute>()?.Path
                ?? property.Name;
            // Build the selector for that path
            var selector = PathSelector.Select(source, path);
            // Now determine how we assign the selected value back to the object
            // Collections (the selector returned an IEnumerable)
            MemberAssignment? binding;
            if (selector.Type.IsAssignableTo(typeof(IEnumerable)) && selector.Type != typeof(string))
            {
                binding = BindCollection(property, selector, depth);
            }
            // Objects (the selector returned a class that isn't a CLR type)
            else if (selector.Type.IsClass && !selector.Type.IsClrType())
            {
                binding = BindObject(property, selector, depth);
            }
            // Otherwise it is a simple property
            else
            {
                binding = BindSimple(property, selector);
            }
            // If we found a successful assignment operation, add it to the bindings
            if (binding is not null)
            {
                bindings.Add(binding);
            }
        }
        var init = Expression.MemberInit(
            Expression.New(destination),
            bindings);
        return init;
    }

    private static MemberAssignment? BindCollection(
        PropertyInfo property,
        Expression selector,
        int depth)
    {
        // First, extract the collection types on either side (source and destination)
        var sourceElementType = selector.Type.GetCollectionElementType();
        var destElementType = property.PropertyType.GetCollectionElementType();
        // If the two types are not equal, we need to handle conversion
        if (sourceElementType != destElementType)
        {
            // Loop back into the mapper to handle mapping the source type to the destination type
            var param = Expression.Parameter(sourceElementType);
            var map = BuildMap(param, destElementType, depth + 1);
            if (map is null)
            {
                // If the map returns null, we traversed too deep to support mapping this property
                return null;
            }
            var lambda = Expression.Lambda(map, param);
            // Then wrap our selector in a call to Select, projecting the source element type to the destination type
            var select = ReflectionCache.Select.MakeGenericMethod(sourceElementType, destElementType);
            selector = Expression.Call(null, select, [selector, lambda]);
        }
        // Now, handle assignability of the outer collection types
        // For example, the above is likely to return an IEnumerable, but if the destination property is a List, it
        // will fail to assign, so we need a ToList call
        if (selector.Type.IsAssignableTo(property.PropertyType))
        {
            return Expression.Bind(property, selector);
        }
        var destinationCollectionType = property.PropertyType.GetCollectionType();
        if (destinationCollectionType == typeof(List<>) || destinationCollectionType == typeof(ICollection<>))
        {
            var toList = ReflectionCache.ToList.MakeGenericMethod(destElementType);
            selector = Expression.Call(null, toList, selector);
        }
        else if (destinationCollectionType.IsArray)
        {
            var toArray = ReflectionCache.ToArray.MakeGenericMethod(destElementType);
            selector = Expression.Call(null, toArray, selector);
        }
        // TODO: Add more collection types
        return Expression.Bind(property, selector);
    }

    private static MemberAssignment? BindObject(
        PropertyInfo property,
        Expression selector,
        int depth)
    {
        // Check if the objects are directly assignable
        if (selector.Type.IsAssignableTo(property.PropertyType))
        {
            return Expression.Bind(property, selector);
        }
        // If the types are not assignable, feed it through the mapper
        var map = BuildMap(selector, property.PropertyType, depth + 1);
        if (map is null)
        {
            // If the map returns null, we traversed too deep to support mapping this property
            return null;
        }
        // Wrap the map in a null check
        var compare = Expression.Equal(selector, Expression.Constant(null, selector.Type));
        var condition = Expression.Condition(
            compare,
            Expression.Constant(null, map.Type),
            map);
        return Expression.Bind(property, condition);
    }

    private static MemberAssignment BindSimple(
        PropertyInfo property,
        Expression selector)
    {
        return Expression.Bind(
            property,
            property.PropertyType == selector.Type
                ? selector
                : Expression.Convert(selector, property.PropertyType));
    }
}
