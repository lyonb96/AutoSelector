using System.Reflection;

namespace AutoSelect;

/// <summary>
/// Internal helper class for caching common reflection lookups, both to reduce code complexity at call sites and to
/// optimize a bit.
/// </summary>
internal static class ReflectionCache
{
    private static MethodInfo? select;
    private static MethodInfo? toList;
    private static MethodInfo? toArray;
    private static MethodInfo? map;

    internal static MethodInfo Select
        => select ??= FindSelect();

    internal static MethodInfo ToList
        => toList ??= FindToList();

    internal static MethodInfo ToArray
        => toArray ??= FindToArray();

    internal static MethodInfo Map
        => map ??= FindMap();

    private static MethodInfo FindSelect()
    {
        var methods = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static);
        return methods.Single(m => m.Name == nameof(Enumerable.Select)
            && m.GetParameters()
                .Last()
                .ParameterType
                .GetGenericTypeDefinition() == typeof(Func<,>));
    }

    private static MethodInfo FindToList()
    {
        return typeof(Enumerable)
            .GetMethod(nameof(Enumerable.ToList), BindingFlags.Public | BindingFlags.Static)!;
    }

    private static MethodInfo FindToArray()
    {
        return typeof(Enumerable)
            .GetMethod(nameof(Enumerable.ToArray), BindingFlags.Public | BindingFlags.Static)!;
    }

    private static MethodInfo FindMap()
    {
        return typeof(Extensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(x => x.Name == nameof(Extensions.Map) && x.GetGenericArguments().Length == 2);
    }
}
