namespace LightRules.Attributes;

/// <summary>
/// Attribute to mark a method that returns the priority for a rule or a specific action.
/// </summary>
/// <remarks>
/// When applied to a method, the method should return an <c>int</c> that represents
/// the computed priority. This allows dynamic priority calculation at runtime.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class PriorityAttribute : Attribute
{
}