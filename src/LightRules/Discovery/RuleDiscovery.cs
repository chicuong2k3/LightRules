using System;
using System.Collections.Generic;
using System.Linq;
using LightRules.Core;
using LightRules.Attributes;
using LightRules.Generated;

namespace LightRules.Discovery
{
    public static class RuleDiscovery
    {
        /// <summary>
        /// Discover rule metadata using the compile-time generated registry. This avoids any runtime
        /// reflection or assembly scanning and is AOT-friendly. The generator produces
        /// <see cref="LightRules.Generated.RuleRegistry.All"/> containing all generated adapters.
        /// </summary>
        public static IEnumerable<RuleMetadata> Discover()
        {
            // Return the registry produced by the source generator.
            return RuleRegistry.All;
        }
    }
}
