using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Provides a currency's flat interest rate as source data.
    /// </summary>
    public sealed class CurrencyRateSource : IGraphNode
    {
        public CurrencyRateSource(IReadOnlyDictionary<string, JsonElement> definition)
            : this(
                GetString(definition, "name"),
                GetDouble(definition, "interestRate"))
        {
        }

        public CurrencyRateSource(string name, double interestRate)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Currency rate source name cannot be empty.", nameof(name));
            }

            if (double.IsNaN(interestRate) || double.IsInfinity(interestRate))
            {
                throw new ArgumentOutOfRangeException(nameof(interestRate), "Interest rate must be a finite number.");
            }

            Name = name;
            InterestRate = interestRate;
        }

        public string Name { get; }

        public string Type => nameof(CurrencyRateSource);

        public double InterestRate { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [];

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(CurrencyRateSource));
        }

        private static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetDouble(definition, propertyName, nameof(CurrencyRateSource));
        }
    }
}
