namespace LightRules.Core
{
    /// <summary>
    /// Abstract base implementation of <see cref="IRulesEngine"/> providing
    /// basic storage and registration of engine parameters and listeners.
    /// Concrete engines should extend this class and implement the <see cref="IRulesEngine.Fire"/>
    /// and <see cref="IRulesEngine.Check"/> behaviors.
    /// </summary>
    public abstract class AbstractRulesEngine : IRulesEngine
    {
        private readonly RulesEngineParameters _parameters;
        private readonly List<IRuleListener> _ruleListeners;
        private readonly List<IRulesEngineListener> _rulesEngineListeners;

        /// <summary>
        /// Create a new instance with default parameters.
        /// </summary>
        protected AbstractRulesEngine() : this(new RulesEngineParameters()) { }

        /// <summary>
        /// Create a new instance with the provided parameters.
        /// </summary>
        protected AbstractRulesEngine(RulesEngineParameters parameters)
        {
            _parameters = parameters;
            _ruleListeners = new List<IRuleListener>();
            _rulesEngineListeners = new List<IRulesEngineListener>();
        }

        /// <summary>
        /// Return a copy of the current engine parameters.
        /// </summary>
        public RulesEngineParameters GetParameters()
        {
            return new RulesEngineParameters(
                _parameters.SkipOnFirstAppliedRule,
                _parameters.SkipOnFirstFailedRule,
                _parameters.SkipOnFirstNonTriggeredRule,
                _parameters.PriorityThreshold
            );
        }

        /// <summary>
        /// Return an immutable view of registered rule listeners.
        /// </summary>
        public IReadOnlyList<IRuleListener> GetRuleListeners()
        {
            return _ruleListeners.AsReadOnly();
        }

        /// <summary>
        /// Return an immutable view of registered rules-engine listeners.
        /// </summary>
        public IReadOnlyList<IRulesEngineListener> GetRulesEngineListeners()
        {
            return _rulesEngineListeners.AsReadOnly();
        }

        /// <summary>
        /// Register a single rule listener.
        /// </summary>
        public void RegisterRuleListener(IRuleListener ruleListener)
        {
            _ruleListeners.Add(ruleListener);
        }

        /// <summary>
        /// Register multiple rule listeners.
        /// </summary>
        public void RegisterRuleListeners(IEnumerable<IRuleListener> ruleListeners)
        {
            _ruleListeners.AddRange(ruleListeners);
        }

        /// <summary>
        /// Register a single rules-engine listener.
        /// </summary>
        public void RegisterRulesEngineListener(IRulesEngineListener listener)
        {
            _rulesEngineListeners.Add(listener);
        }

        /// <summary>
        /// Register multiple rules-engine listeners.
        /// </summary>
        public void RegisterRulesEngineListeners(IEnumerable<IRulesEngineListener> listeners)
        {
            _rulesEngineListeners.AddRange(listeners);
        }

        // IRulesEngine.Fire and Check must be implemented by subclasses
        public abstract Facts Fire(Rules rules, Facts facts);

        public virtual IDictionary<IRule, bool> Check(Rules rules, Facts facts)
        {
            var results = new Dictionary<IRule, bool>();
            foreach (var rule in rules)
            {
                try
                {
                    var eval = rule.Evaluate(facts);
                    results[rule] = eval;
                }
                catch
                {
                    results[rule] = false;
                }
            }
            return results;
        }

        /// <summary>
        /// Protected access to current parameters for subclasses.
        /// </summary>
        protected RulesEngineParameters Parameters => _parameters;

        /// <summary>
        /// Protected access to the modifiable list of rule listeners for subclasses.
        /// </summary>
        protected IList<IRuleListener> RuleListeners => _ruleListeners;

        /// <summary>
        /// Protected access to the modifiable list of rules-engine listeners for subclasses.
        /// </summary>
        protected IList<IRulesEngineListener> RulesEngineListeners => _rulesEngineListeners;
    }
}
