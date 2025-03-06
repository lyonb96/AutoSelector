namespace AutoSelect;

public static class Extensions
{
    /// <summary>
    /// Maps the elements of this query to the destination type via AutoSelector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the query.</typeparam>
    /// <typeparam name="TDest">The type to map the query elements to.</typeparam>
    /// <param name="query">The query to apply the map to.</param>
    /// <returns>A query with the elements mapped to the specified destination type.</returns>
    public static IQueryable<TDest> Map<TSource, TDest>(this IQueryable<TSource> query)
        where TDest : class, new()
    {
        return query.Select(AutoSelector<TSource, TDest>.Map());
    }

    /// <summary>
    /// Maps the elements of this query to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to map the query elements to.</typeparam>
    /// <param name="query">The query to apply the map to.</param>
    /// <returns>A query with the elements mapped to the specified destination type.</returns>
    public static IQueryable<T> Map<T>(this IQueryable query)
    {
        var map = ReflectionCache.Map.MakeGenericMethod(
            query.ElementType,
            typeof(T));
        return (IQueryable<T>)map.Invoke(null, [query])!;
    }

    /// <summary>
    /// Helper that returns the type of elements in a collection. Not the most robust implementation, but works for
    /// most cases.
    /// </summary>
    /// <param name="t">The type to find the element type from.</param>
    /// <returns>The type of the elements of the collection.</returns>
    internal static Type GetCollectionElementType(this Type t)
        => t.GetElementType() ?? t.GetGenericArguments().First();

    /// <summary>
    /// Helper that returns the outer type of a collection, for example List{} if the type is List{int}.</int>
    /// </summary>
    /// <param name="t">The type to determine the outer collection type from.</param>
    /// <returns>
    /// The open generic type of the collection if generic, or the type itself if it represents an array.
    /// </returns>
    internal static Type GetCollectionType(this Type t)
        => t.IsArray ? t : t.GetGenericTypeDefinition();

    /// <summary>
    /// Quick and dirty way to check if a type is a built-in data type. Again, not the most robust thing in the world,
    /// but it works for these purposes.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    internal static bool IsClrType(this Type t)
        => t.Assembly == typeof(int).Assembly;
}
