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
        /// Fire all registered rules asynchronously on the given facts according to engine parameters.
        /// Returns the final Facts instance after execution.
        /// </summary>
        /// <param name="rules">The rules to fire.</param>
        /// <param name="facts">The initial facts.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        Task<Facts> FireAsync(Rules rules, Facts facts, CancellationToken cancellationToken = default);

        /// <summary>
        /// Evaluate rules without firing them. Returns a map from rule to evaluation result.
        /// Default implementation returns an empty map.
        /// </summary>
        IDictionary<IRule, bool> Check(Rules rules, Facts facts) => new Dictionary<IRule, bool>();

        /// <summary>
        /// Evaluate rules asynchronously without firing them. Returns a map from rule to evaluation result.
        /// </summary>
        /// <param name="rules">The rules to check.</param>
        /// <param name="facts">The facts to evaluate against.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        Task<IDictionary<IRule, bool>> CheckAsync(Rules rules, Facts facts, CancellationToken cancellationToken = default);
    }
}
