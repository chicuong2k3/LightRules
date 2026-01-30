using System;
using System.Linq;
using LightRules.Core;
using LightRules.Discovery;

class Program
{
    static void Main()
    {
        // Discover rules using the generated registry
        var metas = RuleDiscovery.Discover().ToList();
        Console.WriteLine($"Discovered {metas.Count} rule metadata entries.");

        // If no metadata was produced by the generator, fail fast — adapters must be generated at compile time.
        if (metas.Count == 0)
        {
            Console.Error.WriteLine("No generated rule adapters found. Ensure the source generator (LightRules.Generator) is enabled for this project and that rules are annotated.");
            return;
        }

        // Instantiate adapters by creating the original POCO and passing it to the adapter's ctor
        var rules = new System.Collections.Generic.List<IRule>();
        foreach (var meta in metas)
        {
            var adapterType = meta.RuleType;
            Console.WriteLine($"Adapter type: {adapterType.FullName}");

            // derive the POCO type name by removing the suffix
            string adapterName = adapterType.FullName!;
            string pocoTypeName = adapterName.Replace("_RuleAdapter", "");
            Console.WriteLine($"Expecting POCO type: {pocoTypeName}");

            Type? pocoType = Type.GetType(pocoTypeName) ?? AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => (t.FullName ?? string.Empty) == pocoTypeName);

            if (pocoType == null)
            {
                Console.WriteLine($"POCO type not found in loaded assemblies: {pocoTypeName}");
            }
            else
            {
                Console.WriteLine($"Found POCO type: {pocoType.FullName}");
            }

            object? pocoInstance = null;
            if (pocoType != null)
            {
                try
                {
                    pocoInstance = Activator.CreateInstance(pocoType);
                    Console.WriteLine($"Created POCO instance of type {pocoType.FullName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create POCO instance: {ex.Message}");
                }
            }

            object? adapterInstance = null;
            try
            {
                if (pocoInstance != null)
                {
                    adapterInstance = Activator.CreateInstance(adapterType, pocoInstance);
                }
                else
                {
                    // try parameterless adapter ctor as fallback
                    adapterInstance = Activator.CreateInstance(adapterType);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to create adapter for {adapterType.FullName}: {ex.Message}");
            }

            if (adapterInstance is IRule ir)
            {
                Console.WriteLine($"Adapter instantiated: {adapterType.FullName}");
                rules.Add(ir);
            }
            else
            {
                Console.WriteLine($"Adapter instance is null or not IRule for {adapterType.FullName}");
            }
        }

        Console.WriteLine($"Instantiated {rules.Count} IRule instances.");

        var facts = new Facts();
        facts.Set("orderTotal", 1500m);
        facts.Set("orderId", "ORD-123");

        var rulesCollection = new Rules(rules);
        var engine = new DefaultRulesEngine();

        engine.Fire(rulesCollection, facts);

        Console.WriteLine("Finished running sample rules.");
    }
}
