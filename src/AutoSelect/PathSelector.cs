using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoSelect;

public static class PathSelector
{
    /// <summary>
    /// Creates an expression that accesses the path provided, starting at the source expression.
    /// </summary>
    /// <param name="source">The expression to build off of.</param>
    /// <param name="path">The path to select.</param>
    /// <returns>An expression that returns the path provided.</returns>
    public static Expression Select(
        Expression source,
        string path)
    {
        var type = source.Type;
        var properties = type.GetProperties();
        // Find the property that matches the path
        var property = properties
            .SingleOrDefault(p => p.Name.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (property is null)
        {
            // No property precisely matches the path. This likely means we're looking to traverse into a subobject
            // In order to determine which property, find properties where the name appears at the start of the path
            var possibleProperties = properties
                .Where(p => path.StartsWith(p.Name, StringComparison.OrdinalIgnoreCase));
            property = possibleProperties.Count() switch
            {
                // One match = we found the property
                1 => possibleProperties.Single(),
                // No match = there is no possible property to follow
                0 => throw new ArgumentException(
                    $"The provided path '{path}' did not match any properties by exact name or prefix for the type '{type.Name}'.",
                    nameof(path)),
                // More than 1 match = ambiguous match, unable to proceed
                _ => throw new ArgumentException(
                    $"The provided path '{path}' resulted in an ambiguous match for the type '{type.Name}'",
                    nameof(path)),
            };
        }
        // We have a property to step into; now decide how to step into it
        var nextPath = path[property.Name.Length..];
        if (property.PropertyType.IsAssignableTo(typeof(IEnumerable))
            && property.PropertyType != typeof(string))
        {
            // Collections need to be stepped into; we have to specifically exclude strings from this check to avoid
            // returning char arrays
            return SelectCollection(source, property, nextPath);
        }
        // All other properties are easy; simply write a member access expression
        var propertyExpression = Expression.Property(source, property);
        // If the next path is not empty, we still have more work to do
        if (nextPath.Length > 0)
        {
            return Select(propertyExpression, nextPath);
        }
        return propertyExpression;
    }

    private static Expression SelectCollection(
        Expression source,
        PropertyInfo collectionProperty,
        string path)
    {
        // First, check if we're in the simple case - directly selecting a collection with no sub-expression
        var collectionPropertyExpression = Expression.Property(source, collectionProperty);
        if (string.IsNullOrWhiteSpace(path))
        {
            return collectionPropertyExpression;
        }
        // If the path contains additional values, we can assume that this is a sub-expression - that is, we want to
        // use the path to select properties on the items of the collection. In order to do that, we need to extract
        // the type of the collection's items, and run the path selector against that
        var collectionType = collectionProperty.PropertyType;
        // NOTE: This method of selecting the collection's inner type may not work for all collection types; a better
        // implementation would be nice, but this should serve the vast majority of cases.
        var collectionInnerType = collectionType.GetCollectionElementType();
        var nestedSource = Expression.Parameter(collectionInnerType);
        // Get the inner path we want to select
        var nestedPath = Select(nestedSource, path);
        // Create a lambda we can pass to the Select method
        var selectBody = Expression.Lambda(nestedPath, nestedSource);
        // Now create a call to Enumerable.Select(), passing in our source (the collection property) and select body
        var selectMethod = ReflectionCache.Select.MakeGenericMethod(collectionInnerType, nestedPath.Type);
        return Expression.Call(null, selectMethod, [ collectionPropertyExpression, selectBody ]);
    }
}
