using System;

namespace LightRules.Core
{
    /// <summary>
    /// Represents a named fact stored in the rules engine.
    /// Equality and hash code are based on the fact name only.
    /// This non-generic base type is used by the <see cref="Facts"/> collection to store facts
    /// irrespective of their value type.
    /// </summary>
    public class Fact
    {
        /// <summary>
        /// The fact name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The fact value as an object.
        /// </summary>
        public object? Value { get; protected set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Fact"/>.
        /// </summary>
        /// <param name="name">The fact name.</param>
        /// <param name="value">The fact value.</param>
        public Fact(string name, object? value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        /// <summary>
        /// Returns a string representation of the fact in the form "Name=Value".
        /// </summary>
        public override string ToString() => $"{Name}={Value}";

        /// <summary>
        /// Facts are considered equal when their names are equal (ordinal comparison).
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is not Fact other) return false;
            return string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Hash code is based on the fact name using ordinal string comparer.
        /// </summary>
        public override int GetHashCode() => string.IsNullOrWhiteSpace(Name) ? 0 : StringComparer.Ordinal.GetHashCode(Name);
    }

    /// <summary>
    /// Type-safe fact wrapper. Provides a strongly-typed view over the underlying fact value.
    /// </summary>
    /// <typeparam name="T">Type of the fact value.</typeparam>
    public sealed class Fact<T> : Fact
    {
        /// <summary>
        /// Gets or sets the typed value of the fact.
        /// </summary>
        public new T Value
        {
            get => (T)base.Value!;
            set => base.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Fact{T}"/>.
        /// </summary>
        /// <param name="name">The fact name. Must not be null.</param>
        /// <param name="value">The fact value. Must not be null.</param>
        public Fact(string name, T value) : base(name, value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
        }
    }
}
