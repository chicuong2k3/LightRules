namespace LightRules.Attributes;

/// <summary>
/// Attribute to mark a method as an action within a rule.
/// </summary>
/// <remarks>
/// Apply to instance methods on types that implement the IRule interface. When multiple
/// action methods are present, the <see cref="Order"/> property can be used to
/// control execution order (lower values run first).
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class ActionAttribute : Attribute
{
    /// <summary>
    /// The order of the action when multiple action methods exist on the same rule.
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Initializes a new instance of <see cref="ActionAttribute"/> with default order (0).
    /// </summary>
    public ActionAttribute()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of <see cref="ActionAttribute"/> with the specified order.
    /// </summary>
    /// <param name="order">The execution order for the action. Lower numbers run earlier.</param>
    public ActionAttribute(int order)
    {
        Order = order;
    }
}