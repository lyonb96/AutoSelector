namespace AutoSelect;

/// <summary>
/// An attribute that allows specifying a path to map a property from instead of using the property's name.
/// </summary>
/// <param name="path">The path to map the value from.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MapFromAttribute(string path) : Attribute
{
    /// <summary>Gets or sets the path to map the property from.</summary>
    /// <value>The path to map the value from.</value>
    public string Path { get; set; } = path;
}
