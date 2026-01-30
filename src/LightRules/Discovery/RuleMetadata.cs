using System;
using System.Collections.Generic;
using LightRules.Attributes;

namespace LightRules.Discovery
{
    public sealed class RuleMetadata
    {
        public Type RuleType { get; }
        public string Name { get; }
        public string? Description { get; }
        public int Priority { get; }
        public bool Enabled { get; }
        public IReadOnlyList<string> Tags { get; }

        public RuleMetadata(Type ruleType, string name, string? description, int priority, bool enabled, IReadOnlyList<string> tags)
        {
            RuleType = ruleType ?? throw new ArgumentNullException(nameof(ruleType));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Priority = priority;
            Enabled = enabled;
            Tags = tags ?? Array.Empty<string>();
        }
    }
}
