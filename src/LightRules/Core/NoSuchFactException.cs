using System;

namespace LightRules.Core
{
    /// <summary>
    /// Thrown when a requested fact is not present in the Facts collection.
    /// </summary>
    public sealed class NoSuchFactException : Exception
    {
        public string MissingFact { get; }
        public NoSuchFactException(string message, string missingFact) : base(message)
        {
            MissingFact = missingFact;
        }
    }
}
