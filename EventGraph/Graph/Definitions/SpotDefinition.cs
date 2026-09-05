using System;

namespace EventGraph
{
    /// <summary>
    /// Defines immutable metadata for a spot value.
    /// </summary>
    public sealed record SpotDefinition
    {
        public SpotDefinition(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Currency cannot be empty.", nameof(currency));
            }

            Currency = currency;
        }

        public string Currency { get; }
    }

    /// <summary>
    /// Provides immutable metadata for a spot value.
    /// </summary>
    public interface ISpotDefinitionProvider : IDefinitionProvider<SpotDefinition>
    {
    }
}