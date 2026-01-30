namespace LightRules.Core
{
    /// <summary>
    /// A default rule implementation that evaluates a single <see cref="ICondition"/>
    /// and executes a sequence of <see cref="IAction"/> when the condition is satisfied.
    /// </summary>
    public sealed class DefaultRule : BasicRule
    {
        private readonly ICondition _condition;
        private readonly IList<IAction> _actions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRule"/> class.
        /// </summary>
        /// <param name="name">Rule name.</param>
        /// <param name="description">Rule description.</param>
        /// <param name="priority">Rule priority.</param>
        /// <param name="condition">Condition to evaluate; must not be null.</param>
        /// <param name="actions">Sequence of actions to execute when the condition is true; must not be null.</param>
        public DefaultRule(string name, string? description, int priority, ICondition condition, IEnumerable<IAction> actions)
            : base(name, description, priority)
        {
            _condition = condition ?? throw new System.ArgumentNullException(nameof(condition));
            System.ArgumentNullException.ThrowIfNull(actions);
            _actions = new List<IAction>(actions);
        }

        /// <summary>
        /// Evaluate the wrapped condition against the provided facts.
        /// </summary>
        /// <param name="facts">Evaluation facts.</param>
        /// <returns>True if the condition is satisfied; otherwise false.</returns>
        public override bool Evaluate(Facts facts)
        {
            return _condition.Evaluate(facts);
        }

        /// <summary>
        /// Execute the configured actions in order using the provided facts.
        /// </summary>
        /// <param name="facts">Execution facts.</param>
        public override void Execute(Facts facts)
        {
            foreach (var action in _actions)
            {
                action.Execute(facts);
            }
        }
    }
}
