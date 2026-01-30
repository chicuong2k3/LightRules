using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LightRules.Core;
using LightRules.Attributes;

namespace LightRules.Discovery
{
    public static class RuleDiscovery
    {
        /// <summary>
        /// Discover rule metadata using the compile-time generated registries. Scans all loaded
        /// assemblies for generated RuleRegistry classes and aggregates their entries.
        /// </summary>
        public static IEnumerable<RuleMetadata> Discover()
        {
            var results = new List<RuleMetadata>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var registryType = assembly.GetType("LightRules.Generated.RuleRegistry");
                    if (registryType == null) continue;

                    var allField = registryType.GetField("All", BindingFlags.Public | BindingFlags.Static);
                    if (allField == null) continue;

                    var value = allField.GetValue(null);
                    if (value is RuleMetadata[] entries)
                    {
                        results.AddRange(entries);
                    }
                }
                catch
                {
                    // Ignore assemblies that can't be inspected
                }
            }

            return results;
        }
    }
}
