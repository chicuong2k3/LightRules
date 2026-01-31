using System.Collections.Concurrent;

namespace LightRules.Core
{
    /// <summary>
    /// Thread-safe abstract base implementation of <see cref="IRulesEngine"/> providing
    /// basic storage and registration of engine parameters and listeners.
    /// Concrete engines should extend this class and implement the <see cref="IRulesEngine.Fire"/>
    /// and <see cref="IRulesEngine.Check"/> behaviors.
    /// </summary>
    public abstract class AbstractRulesEngine : IRulesEngine
    {
        private readonly RulesEngineParameters _parameters;
        private readonly ConcurrentBag<IRuleListener> _ruleListeners;
        private readonly ConcurrentBag<IRulesEngineListener> _rulesEngineListeners;

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
            _ruleListeners = new ConcurrentBag<IRuleListener>();
            _rulesEngineListeners = new ConcurrentBag<IRulesEngineListener>();
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
        /// Return a snapshot of registered rule listeners.
        /// </summary>
        public IReadOnlyList<IRuleListener> GetRuleListeners()
        {
            return _ruleListeners.ToArray();
        }

        /// <summary>
        /// Return a snapshot of registered rules-engine listeners.
        /// </summary>
        public IReadOnlyList<IRulesEngineListener> GetRulesEngineListeners()
        {
            return _rulesEngineListeners.ToArray();
        }

        /// <summary>
        /// Register a single rule listener. Thread-safe.
        /// </summary>
        public void RegisterRuleListener(IRuleListener ruleListener)
        {
            _ruleListeners.Add(ruleListener);
        }

        /// <summary>
        /// Register multiple rule listeners. Thread-safe.
        /// </summary>
        public void RegisterRuleListeners(IEnumerable<IRuleListener> ruleListeners)
        {
            foreach (var listener in ruleListeners)
            {
                _ruleListeners.Add(listener);
            }
        }

        /// <summary>
        /// Register a single rules-engine listener. Thread-safe.
        /// </summary>
        public void RegisterRulesEngineListener(IRulesEngineListener listener)
        {
            _rulesEngineListeners.Add(listener);
        }

        /// <summary>
        /// Register multiple rules-engine listeners. Thread-safe.
        /// </summary>
        public void RegisterRulesEngineListeners(IEnumerable<IRulesEngineListener> listeners)
        {
            foreach (var listener in listeners)
            {
                _rulesEngineListeners.Add(listener);
            }
        }

        // IRulesEngine.Fire and Check must be implemented by subclasses
        public abstract Facts Fire(Rules rules, Facts facts);

        /// <summary>
        /// Fire all registered rules asynchronously. Subclasses should override for true async behavior.
        /// Default implementation calls the synchronous Fire method.
        /// </summary>
        public virtual Task<Facts> FireAsync(Rules rules, Facts facts, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Fire(rules, facts));
        }

        /// <summary>
        /// Check rules asynchronously. Subclasses should override for true async behavior.
        /// Default implementation calls the synchronous Check method.
        /// </summary>
        public virtual Task<IDictionary<IRule, bool>> CheckAsync(Rules rules, Facts facts, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Check(rules, facts));
        }

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
        /// Protected access to rule listeners for subclasses. Returns a snapshot for thread-safe iteration.
        /// </summary>
        protected IEnumerable<IRuleListener> RuleListeners => _ruleListeners.ToArray();

        /// <summary>
        /// Protected access to rules-engine listeners for subclasses. Returns a snapshot for thread-safe iteration.
        /// </summary>
        protected IEnumerable<IRulesEngineListener> RulesEngineListeners => _rulesEngineListeners.ToArray();
    }
}
