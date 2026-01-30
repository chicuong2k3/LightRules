namespace LightRules.Attributes;

/// <summary>
/// Attribute to mark a parameter as a named fact to be injected into a rule method.
/// </summary>
/// <remarks>
/// Apply to parameters of condition or action methods to indicate which fact from the
/// facts context should be bound to that parameter. If <see cref="Value"/> is null or
/// empty the parameter name may be used as the fact key by the discovery/injection logic.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
public class FactAttribute : Attribute
{
    /// <summary>
    /// The name of the fact to bind to the parameter. If null or empty, the parameter
    /// name is typically used as the fact key.
    /// </summary>
    public string? Value { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FactAttribute"/> class.
    /// </summary>
    public FactAttribute()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FactAttribute"/> class with the
    /// specified fact name.
    /// </summary>
    /// <param name="value">The fact name to bind to the parameter.</param>
    public FactAttribute(string value)
    {
        Value = value;
    }
}