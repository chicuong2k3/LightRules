namespace LightRules.Core
{
    /// <summary>
    /// A simple base implementation of <see cref="IRule"/> that provides
    /// name, description and priority fields and default (no-op) behavior for
    /// evaluation and execution. Intended as a convenient base type for tests
    /// and simple rules.
    /// </summary>
    public class BasicRule : IRule
    {
        /// <summary>
        /// Rule name.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Rule description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Rule priority.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Create a new <see cref="BasicRule"/> with default name, description and priority.
        /// </summary>
        public BasicRule()
            : this(IRule.DefaultName, IRule.DefaultDescription, IRule.DefaultPriority)
        {
        }

        /// <summary>
        /// Create a new <see cref="BasicRule"/> with the given name and description.
        /// </summary>
        /// <param name="name">Rule name.</param>
        /// <param name="description">Rule description.</param>
        public BasicRule(string name, string description)
            : this(name, description, IRule.DefaultPriority)
        {
        }

        /// <summary>
        /// Create a new <see cref="BasicRule"/> with name, description and priority.
        /// </summary>
        /// <param name="name">Rule name.</param>
        /// <param name="description">Rule description.</param>
        /// <param name="priority">Rule priority.</param>
        public BasicRule(string name, string? description = IRule.DefaultDescription, int priority = IRule.DefaultPriority)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? IRule.DefaultDescription;
            Priority = priority;
        }

        /// <summary>
        /// Evaluate the rule against the provided facts. The default implementation
        /// returns <c>false</c> (no-op). Override in subclasses to implement rule logic.
        /// </summary>
        /// <param name="facts">Evaluation facts.</param>
        /// <returns><c>true</c> if the rule should fire; otherwise <c>false</c>.</returns>
        public virtual bool Evaluate(Facts facts)
        {
            return false;
        }

        /// <summary>
        /// Execute the rule actions. Default implementation is a no-op. Override in subclasses.
        /// </summary>
        /// <param name="facts">Execution facts.</param>
        public virtual void Execute(Facts facts)
        {
            // no-op
        }

        /// <summary>
        /// Return the rule name.
        /// </summary>
        public override string ToString() => Name;

        /// <summary>
        /// Rules are considered equal if they have the same type, name, description and priority.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj == null || GetType() != obj.GetType()) return false;

            var other = (BasicRule)obj;

            if (Priority != other.Priority) return false;
            if (!string.Equals(Name, other.Name, StringComparison.Ordinal)) return false;
            return string.Equals(Description, other.Description, StringComparison.Ordinal);
        }

        /// <summary>
        /// Compute a hash code consistent with <see cref="Equals(object)"/>.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(Name ?? string.Empty);
                hash = hash * 31 + (Description != null ? StringComparer.Ordinal.GetHashCode(Description) : 0);
                hash = hash * 31 + Priority;
                return hash;
            }
        }

        /// <summary>
        /// Compare rules by priority then name (ordinal).
        /// </summary>
        public int CompareTo(IRule? other)
        {
            if (other == null) return 1;
            if (Priority < other.Priority) return -1;
            if (Priority > other.Priority) return 1;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
    }
}
