namespace LightRules.Core
{
    /// <summary>
    /// Condition combinators (AND / OR / NOT) for composing simple ICondition instances.
    /// </summary>
    public static class ConditionCombinators
    {
        public static ICondition And(ICondition a, ICondition b) => new AndCondition(a, b);
        public static ICondition Or(ICondition a, ICondition b) => new OrCondition(a, b);
        public static ICondition Not(ICondition inner) => new NotCondition(inner);

        private sealed class AndCondition(ICondition a, ICondition b) : ICondition
        {
            public bool Evaluate(Facts facts) => a.Evaluate(facts) && b.Evaluate(facts);
        }

        private sealed class OrCondition(ICondition a, ICondition b) : ICondition
        {
            public bool Evaluate(Facts facts) => a.Evaluate(facts) || b.Evaluate(facts);
        }

        private sealed class NotCondition(ICondition inner) : ICondition
        {
            public bool Evaluate(Facts facts) => !inner.Evaluate(facts);
        }
    }
}
