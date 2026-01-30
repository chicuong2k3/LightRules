namespace LightRules.Core
{
    /// <summary>
    /// Listener for high-level rules engine lifecycle events.
    /// </summary>
    public interface IRulesEngineListener
    {
        /// <summary>
        /// Triggered before evaluating the rule set. When used with an inference engine,
        /// this may be called before each candidate rule set evaluation in each iteration.
        /// </summary>
        void BeforeEvaluate(Rules rules, Facts facts);

        /// <summary>
        /// Triggered after executing the rule set. When used with an inference engine,
        /// this may be called after executing each candidate rule set in each iteration.
        /// </summary>
        void AfterExecute(Rules rules, Facts facts);
    }
}
