using System;
using System.Linq;
using LightRules.Core;
using LightRules.Discovery;

class Program
{
    static void Main()
    {
        Console.WriteLine("LightRules sample: attribute-based discovery + engine run\n");

        // Discover rules using the global registry (auto-registered via ModuleInitializer)
        var metas = RuleDiscovery.Discover().ToList();
        Console.WriteLine($"Discovered {metas.Count} rule metadata entries.");

        if (metas.Count == 0)
        {
            Console.Error.WriteLine("No generated rule adapters found. Ensure the source generator (LightRules.Generator) is enabled for this project and that rules are annotated.");
            return;
        }

        // Create rule instances using the factory - no reflection required!
        var rules = new Rules();
        foreach (var meta in metas)
        {
            Console.WriteLine($"\nRule: {meta.Name} (Priority: {meta.Priority})");
            Console.WriteLine($"  Description: {meta.Description}");
            Console.WriteLine($"  Adapter type: {meta.RuleType.FullName}");

            // Use the factory to create instance - no Activator.CreateInstance needed
            var ruleInstance = meta.CreateInstance();
            rules.Register(ruleInstance);
            Console.WriteLine($"  Created instance via factory (no reflection)");
        }

        Console.WriteLine($"\nInstantiated {rules.Size} IRule instances.\n");

        // Create facts
        var facts = new Facts();
        facts = facts.AddOrReplaceFact("orderTotal", 1500m);
        facts = facts.AddOrReplaceFact("orderId", "ORD-123");

        Console.WriteLine("Facts before running rules: " + facts);

        // Run the engine
        var engine = new DefaultRulesEngine();
        var finalFacts = engine.Fire(rules, facts);

        Console.WriteLine("\nFacts after running rules: " + finalFacts);
        Console.WriteLine("\nFinished running sample rules.");
    }
}
