namespace LightRules.Core
{
    /// <summary>
    /// Inference rules engine: repeatedly selects candidate rules based on facts and
    /// fires them until no more candidates are available.
    /// </summary>
    public sealed class InferenceRulesEngine : AbstractRulesEngine
    {
        private readonly DefaultRulesEngine _delegate;

        /// <summary>
        /// Create a new inference rules engine with default parameters.
        /// </summary>
        public InferenceRulesEngine() : this(new RulesEngineParameters()) { }

        /// <summary>
        /// Create a new inference rules engine with the given parameters.
        /// </summary>
        public InferenceRulesEngine(RulesEngineParameters parameters) : base(parameters)
        {
            _delegate = new DefaultRulesEngine(parameters);
        }

        public override Facts Fire(Rules rules, Facts facts)
        {
            ArgumentNullException.ThrowIfNull(rules);
            ArgumentNullException.ThrowIfNull(facts);

            var currentFacts = facts;
            IEnumerable<IRule> selectedRules;
            do
            {
                selectedRules = SelectCandidates(rules, currentFacts).ToList();
                if (selectedRules.Any())
                {
                    currentFacts = _delegate.Fire(new Rules(selectedRules), currentFacts);
                }
            } while (selectedRules.Any());

            return currentFacts;
        }

        private IEnumerable<IRule> SelectCandidates(Rules rules, Facts facts)
        {
            return rules.Where(rule => rule.Evaluate(facts)).ToList();
        }

        public override IDictionary<IRule, bool> Check(Rules rules, Facts facts)
        {
            ArgumentNullException.ThrowIfNull(rules);
            ArgumentNullException.ThrowIfNull(facts);
            return _delegate.Check(rules, facts);
        }

        /// <summary>
        /// Fire rules asynchronously with iterative inference.
        /// </summary>
        public override async Task<Facts> FireAsync(Rules rules, Facts facts, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rules);
            ArgumentNullException.ThrowIfNull(facts);

            var currentFacts = facts;
            IEnumerable<IRule> selectedRules;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                selectedRules = await SelectCandidatesAsync(rules, currentFacts, cancellationToken).ConfigureAwait(false);
                if (selectedRules.Any())
                {
                    currentFacts = await _delegate.FireAsync(new Rules(selectedRules), currentFacts, cancellationToken).ConfigureAwait(false);
                }
            } while (selectedRules.Any());

            return currentFacts;
        }

        private async Task<IEnumerable<IRule>> SelectCandidatesAsync(Rules rules, Facts facts, CancellationToken cancellationToken)
        {
            var candidates = new List<IRule>();
            foreach (var rule in rules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool result;
                if (rule is IAsyncRule asyncRule)
                {
                    result = await asyncRule.EvaluateAsync(facts, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    result = rule.Evaluate(facts);
                }
                if (result)
                {
                    candidates.Add(rule);
                }
            }
            return candidates;
        }

        public override Task<IDictionary<IRule, bool>> CheckAsync(Rules rules, Facts facts, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rules);
            ArgumentNullException.ThrowIfNull(facts);
            return _delegate.CheckAsync(rules, facts, cancellationToken);
        }

        public new void RegisterRuleListener(IRuleListener ruleListener)
        {
            base.RegisterRuleListener(ruleListener);
            _delegate.RegisterRuleListener(ruleListener);
        }

        public new void RegisterRuleListeners(IEnumerable<IRuleListener> ruleListeners)
        {
            var list = new List<IRuleListener>(ruleListeners);
            base.RegisterRuleListeners(list);
            _delegate.RegisterRuleListeners(list);
        }

        public new void RegisterRulesEngineListener(IRulesEngineListener rulesEngineListener)
        {
            base.RegisterRulesEngineListener(rulesEngineListener);
            _delegate.RegisterRulesEngineListener(rulesEngineListener);
        }

        public new void RegisterRulesEngineListeners(IEnumerable<IRulesEngineListener> rulesEngineListeners)
        {
            var list = new List<IRulesEngineListener>(rulesEngineListeners);
            base.RegisterRulesEngineListeners(list);
            _delegate.RegisterRulesEngineListeners(list);
        }
    }
}
