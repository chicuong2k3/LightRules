namespace LightRules.Attributes;

/// <summary>
/// Attribute to mark a method as the condition evaluator of a rule.
/// </summary>
/// <remarks>
/// The attributed method should be an instance method on a type implementing
/// <c>IRule</c> and return a <c>bool</c> indicating whether the rule should fire.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class ConditionAttribute : Attribute
{
}