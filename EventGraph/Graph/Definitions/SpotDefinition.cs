using System;

namespace EventGraph
{
    /// <summary>
    /// Defines immutable metadata for a spot value.
    /// </summary>
    public sealed record SpotDefinition
    {
        public SpotDefinition(string name, string currency)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Spot name cannot be empty.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Currency cannot be empty.", nameof(currency));
            }

            Name = name;
            Currency = currency;
        }

        public string Name { get; }

        public string Currency { get; }
    }

}