namespace LightRules.Attributes
{
    /// <summary>
    /// Attribute to mark a class as a rule and provide metadata used by discovery/runtime.
    /// </summary>
    /// <remarks>
    /// Apply to classes that implement <c>IRule</c>. The attribute stores optional
    /// metadata such as a human-friendly name, description, priority, enabled flag,
    /// and tags. If <see cref="Name"/> is not provided, discovery code may fall back
    /// to the type name.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class RuleAttribute : Attribute
    {
        /// <summary>
        /// The rule name which must be unique within a rules registry.
        /// If null or empty, discovery may fall back to the type name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The rule description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The rule priority.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Whether the rule is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Optional tags to categorize the rule.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleAttribute"/> class.
        /// </summary>
        public RuleAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleAttribute"/> class with the specified name.
        /// </summary>
        /// <param name="name">The rule name.</param>
        public RuleAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleAttribute"/> class with the specified name and priority.
        /// </summary>
        /// <param name="name">The rule name.</param>
        /// <param name="priority">The rule priority.</param>
        public RuleAttribute(string name, int priority)
        {
            Name = name;
            Priority = priority;
        }
    }
}
