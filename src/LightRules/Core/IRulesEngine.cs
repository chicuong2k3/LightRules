namespace LightRules.Core
{
    /// <summary>
    /// A rules engine that can evaluate and/or fire rules against a facts set.
    /// </summary>
    public interface IRulesEngine
    {
        /// <summary>
        /// Get the engine parameters controlling execution behavior.
        /// </summary>
        RulesEngineParameters GetParameters();

        /// <summary>
        /// Registered rule listeners. Default implementations may return an empty list.
        /// </summary>
        IReadOnlyList<IRuleListener> GetRuleListeners() => Array.Empty<IRuleListener>();

        /// <summary>
        /// Registered rules engine listeners. Default implementations may return an empty list.
        /// </summary>
        IReadOnlyList<IRulesEngineListener> GetRulesEngineListeners() => Array.Empty<IRulesEngineListener>();

        /// <summary>
        /// Fire all registered rules on the given facts according to engine parameters.
        /// Returns the final Facts instance after execution.
        /// </summary>
        Facts Fire(Rules rules, Facts facts);

        /// <summary>
        /// Evaluate rules without firing them. Returns a map from rule to evaluation result.
        /// Default implementation returns an empty map.
        /// </summary>
        IDictionary<IRule, bool> Check(Rules rules, Facts facts) => new Dictionary<IRule, bool>();
    }
}
