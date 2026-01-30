namespace LightRules.Core
{
    /// <summary>
    /// Parameters that control rules engine execution behavior.
    /// </summary>
    public sealed class RulesEngineParameters
    {
        /// <summary>
        /// Default rule priority threshold (no threshold).
        /// </summary>
        public const int DefaultRulePriorityThreshold = int.MaxValue;

        /// <summary>
        /// Whether to stop evaluating further applicable rules when the first rule is applied.
        /// </summary>
        public bool SkipOnFirstAppliedRule { get; set; }

        /// <summary>
        /// Whether to stop evaluating further applicable rules when the first rule is not triggered.
        /// </summary>
        public bool SkipOnFirstNonTriggeredRule { get; set; }

        /// <summary>
        /// Whether to stop evaluating further applicable rules when the first rule fails.
        /// </summary>
        public bool SkipOnFirstFailedRule { get; set; }

        /// <summary>
        /// Priority threshold above which rules should be skipped.
        /// </summary>
        public int PriorityThreshold { get; set; }

        /// <summary>
        /// Create a new <see cref="RulesEngineParameters"/> with default values.
        /// </summary>
        public RulesEngineParameters()
        {
            PriorityThreshold = DefaultRulePriorityThreshold;
        }

        /// <summary>
        /// Create a new <see cref="RulesEngineParameters"/> with specified values.
        /// </summary>
        public RulesEngineParameters(bool skipOnFirstAppliedRule, bool skipOnFirstFailedRule, bool skipOnFirstNonTriggeredRule, int priorityThreshold)
        {
            SkipOnFirstAppliedRule = skipOnFirstAppliedRule;
            SkipOnFirstFailedRule = skipOnFirstFailedRule;
            SkipOnFirstNonTriggeredRule = skipOnFirstNonTriggeredRule;
            PriorityThreshold = priorityThreshold;
        }

        /// <summary>
        /// Fluent setter for PriorityThreshold.
        /// </summary>
        public RulesEngineParameters WithPriorityThreshold(int priorityThreshold)
        {
            PriorityThreshold = priorityThreshold;
            return this;
        }

        /// <summary>
        /// Fluent setter for SkipOnFirstAppliedRule.
        /// </summary>
        public RulesEngineParameters WithSkipOnFirstAppliedRule(bool skip)
        {
            SkipOnFirstAppliedRule = skip;
            return this;
        }

        /// <summary>
        /// Fluent setter for SkipOnFirstNonTriggeredRule.
        /// </summary>
        public RulesEngineParameters WithSkipOnFirstNonTriggeredRule(bool skip)
        {
            SkipOnFirstNonTriggeredRule = skip;
            return this;
        }

        /// <summary>
        /// Fluent setter for SkipOnFirstFailedRule.
        /// </summary>
        public RulesEngineParameters WithSkipOnFirstFailedRule(bool skip)
        {
            SkipOnFirstFailedRule = skip;
            return this;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Engine parameters {{ skipOnFirstAppliedRule = {SkipOnFirstAppliedRule}, skipOnFirstNonTriggeredRule = {SkipOnFirstNonTriggeredRule}, skipOnFirstFailedRule = {SkipOnFirstFailedRule}, priorityThreshold = {PriorityThreshold} }}";
        }
    }
}
