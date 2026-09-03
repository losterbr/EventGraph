using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Provides a currency's flat interest rate as source data.
    /// </summary>
    public sealed class CurrencyRateSource : IRateSourceNode
    {
        public CurrencyRateSource(IReadOnlyDictionary<string, JsonElement> definition)
            : this(
                GetString(definition, "name"),
                GetDouble(definition, "interestRate"),
                GetStringOrDefault(definition, "currency", GetString(definition, "name")))
        {
        }

        public CurrencyRateSource(string name, double interestRate, string currency = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Currency rate source name cannot be empty.", nameof(name));
            }

            if (double.IsNaN(interestRate) || double.IsInfinity(interestRate))
            {
                throw new ArgumentOutOfRangeException(nameof(interestRate), "Interest rate must be a finite number.");
            }

            if (currency != null && string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Currency cannot be empty.", nameof(currency));
            }

            Name = name;
            InterestRate = interestRate;
            Currency = currency ?? name;
        }

        public string Name { get; }

        public string Type => nameof(CurrencyRateSource);

        public double InterestRate { get; }

        public string Currency { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [];

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(CurrencyRateSource));
        }

        private static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetDouble(definition, propertyName, nameof(CurrencyRateSource));
        }

        private static string GetStringOrDefault(
            IReadOnlyDictionary<string, JsonElement> definition,
            string propertyName,
            string defaultValue)
        {
            return JsonDefinitionReader.GetStringOrDefault(definition, propertyName, defaultValue, nameof(CurrencyRateSource));
        }
    }
}
