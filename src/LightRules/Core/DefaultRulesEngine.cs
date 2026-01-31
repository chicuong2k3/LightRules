namespace LightRules.Core
{
    /// <summary>
    /// Default sequential rules engine that evaluates rules in order and executes actions when conditions match.
    /// Honors engine parameters and invokes listeners at lifecycle points.
    /// </summary>
    public sealed class DefaultRulesEngine : AbstractRulesEngine
    {
        public DefaultRulesEngine() { }
        public DefaultRulesEngine(RulesEngineParameters parameters) : base(parameters) { }

        public override Facts Fire(Rules rules, Facts facts)
        {
            ArgumentNullException.ThrowIfNull(rules);
            ArgumentNullException.ThrowIfNull(facts);
            TriggerListenersBeforeRules(rules, facts);
            var final = DoFire(rules, facts);
            TriggerListenersAfterRules(rules, final);
            return final;
        }

        private Facts DoFire(Rules rules, Facts facts)
        {
            if (rules.IsEmpty)
            {
                return facts;
            }

            var currentFacts = facts;

            foreach (var rule in rules)
            {
                var priority = rule.Priority;

                if (priority > Parameters.PriorityThreshold)
                {
                    break;
                }

                if (!ShouldBeEvaluated(rule, currentFacts))
                {
                    continue;
                }

                var evaluationResult = false;
                try
                {
                    // Evaluate against a snapshot to prevent conditions from mutating the shared Facts instance
                    evaluationResult = rule.Evaluate(currentFacts.Clone());
                }
                catch (Exception ex)
                {
                    TriggerListenersOnEvaluationError(rule, currentFacts, ex);
                    if (Parameters.SkipOnFirstNonTriggeredRule)
                    {
                        break;
                    }
                }

                if (evaluationResult)
                {
                    TriggerListenersAfterEvaluate(rule, currentFacts, true);
                    try
                    {
                        TriggerListenersBeforeExecute(rule, currentFacts);
                        // Execute returns the (possibly) new Facts instance
                        currentFacts = rule.Execute(currentFacts);
                        TriggerListenersOnSuccess(rule, currentFacts);
                        if (Parameters.SkipOnFirstAppliedRule)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        TriggerListenersOnFailure(rule, currentFacts, ex);
                        if (Parameters.SkipOnFirstFailedRule)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    TriggerListenersAfterEvaluate(rule, currentFacts, false);
                    if (Parameters.SkipOnFirstNonTriggeredRule)
                    {
                        break;
                    }
                }
            }

            return currentFacts;
        }

        public override IDictionary<IRule, bool> Check(Rules rules, Facts facts)
        {
            ArgumentNullException.ThrowIfNull(rules);
            ArgumentNullException.ThrowIfNull(facts);
            TriggerListenersBeforeRules(rules, facts);
            var result = DoCheck(rules, facts);
            TriggerListenersAfterRules(rules, facts);
            return result;
        }

        private IDictionary<IRule, bool> DoCheck(Rules rules, Facts facts)
        {
            var results = new Dictionary<IRule, bool>();
            var currentFacts = facts;
            foreach (var rule in rules)
            {
                if (ShouldBeEvaluated(rule, currentFacts))
                {
                    try
                    {
                        results[rule] = rule.Evaluate(currentFacts.Clone());
                    }
                    catch
                    {
                        results[rule] = false;
                    }
                }
            }
            return results;
        }

        private void TriggerListenersOnFailure(IRule rule, Facts facts, Exception exception)
        {
            foreach (var l in RuleListeners) l.OnFailure(rule, facts, exception);
        }

        private void TriggerListenersOnSuccess(IRule rule, Facts facts)
        {
            foreach (var l in RuleListeners) l.OnSuccess(rule, facts);
        }

        private void TriggerListenersBeforeExecute(IRule rule, Facts facts)
        {
            foreach (var l in RuleListeners) l.BeforeExecute(rule, facts);
        }

        private bool TriggerListenersBeforeEvaluate(IRule rule, Facts facts)
        {
            foreach (var l in RuleListeners)
            {
                if (!l.BeforeEvaluate(rule, facts)) return false;
            }
            return true;
        }

        private void TriggerListenersAfterEvaluate(IRule rule, Facts facts, bool evaluationResult)
        {
            foreach (var l in RuleListeners) l.AfterEvaluate(rule, facts, evaluationResult);
        }

        private void TriggerListenersOnEvaluationError(IRule rule, Facts facts, Exception exception)
        {
            foreach (var l in RuleListeners) l.OnEvaluationError(rule, facts, exception);
        }

        private void TriggerListenersBeforeRules(Rules rules, Facts facts)
        {
            foreach (var l in RulesEngineListeners) l.BeforeEvaluate(rules, facts);
        }

        private void TriggerListenersAfterRules(Rules rules, Facts facts)
        {
            foreach (var l in RulesEngineListeners) l.AfterExecute(rules, facts);
        }

        private bool ShouldBeEvaluated(IRule rule, Facts facts)
        {
            return TriggerListenersBeforeEvaluate(rule, facts);
        }
    }
}
