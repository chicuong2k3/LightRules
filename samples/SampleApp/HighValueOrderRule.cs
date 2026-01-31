using System;
using LightRules.Attributes;
using LightRules.Core;

namespace LightRules.Samples
{
    [Rule(Name = "HighValueOrder", Description = "Apply discount to high value orders", Priority = 10)]
    public class HighValueOrderRule
    {
        [Condition]
        public bool IsHighValue([Fact("orderTotal")] decimal total)
        {
            return total >= 1000m;
        }

        [Action(Order = 1)]
        public Facts ApplyDiscount([Fact("orderId")] string id, Facts facts)
        {
            Console.WriteLine($"ApplyDiscount called for order {id}");
            return facts.AddOrReplaceFact("discountApplied", true);
        }
    }
}
