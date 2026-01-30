namespace LightRules.Core
{
    /// <summary>
    /// Listener for per-rule events during evaluation and execution.
    /// </summary>
    public interface IRuleListener
    {
        /// <summary>
        /// Triggered before the evaluation of a rule.
        /// Implementations may return <c>false</c> to skip evaluating this rule.
        /// </summary>
        /// <param name="rule">Rule being evaluated.</param>
        /// <param name="facts">Facts known before evaluating the rule.</param>
        /// <returns>True if the rule should be evaluated; false to skip it.</returns>
        bool BeforeEvaluate(IRule rule, Facts facts);

        /// <summary>
        /// Triggered after the evaluation of a rule.
        /// </summary>
        /// <param name="rule">Rule that has been evaluated.</param>
        /// <param name="facts">Facts known after evaluation.</param>
        /// <param name="evaluationResult">True if the rule evaluated to true; otherwise false.</param>
        void AfterEvaluate(IRule rule, Facts facts, bool evaluationResult);

        /// <summary>
        /// Triggered on condition evaluation error due to any runtime exception.
        /// </summary>
        /// <param name="rule">Rule that was evaluated.</param>
        /// <param name="facts">Facts available when the error occurred.</param>
        /// <param name="exception">The exception thrown during evaluation.</param>
        void OnEvaluationError(IRule rule, Facts facts, Exception exception);

        /// <summary>
        /// Triggered before the execution of a rule (when the condition evaluated to true).
        /// </summary>
        /// <param name="rule">The current rule.</param>
        /// <param name="facts">Facts known before executing the rule.</param>
        void BeforeExecute(IRule rule, Facts facts);

        /// <summary>
        /// Triggered after a rule has been executed successfully.
        /// </summary>
        /// <param name="rule">The current rule.</param>
        /// <param name="facts">Facts known after executing the rule.</param>
        void OnSuccess(IRule rule, Facts facts);

        /// <summary>
        /// Triggered after a rule has failed during execution.
        /// </summary>
        /// <param name="rule">The current rule.</param>
        /// <param name="facts">Facts known after attempting execution.</param>
        /// <param name="exception">The exception thrown during execution.</param>
        void OnFailure(IRule rule, Facts facts, Exception exception);
    }
}
