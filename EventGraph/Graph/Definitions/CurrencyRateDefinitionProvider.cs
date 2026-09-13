using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Provides a currency rate definition loaded from JSON.
    /// </summary>
    public sealed class CurrencyRateDefinitionProvider(string name, double interestRate, string currency = null) : IDefinitionProvider<CurrencyRateDefinition>
    {
        public CurrencyRateDefinitionProvider(IReadOnlyDictionary<string, JsonElement> definition)
            : this(
                GetString(definition, "name"),
                GetDouble(definition, "interestRate"),
                GetStringOrDefault(definition, "currency", GetString(definition, "name")))
        {
        }

        public CurrencyRateDefinition Definition { get; } = new(name, interestRate, currency);

        internal static IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> Compile(
            IReadOnlyDictionary<string, JsonElement> definition,
            DateTime valuationDate,
            IReadOnlyDictionary<string, double> _)
        {
            var rateDefinition = new CurrencyRateDefinitionProvider(definition).Definition;
            return
            [
                new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                {
                    ["type"] = JsonSerializer.SerializeToElement(nameof(CurrencyRateSource)),
                    ["name"] = JsonSerializer.SerializeToElement(rateDefinition.Name),
                    ["currency"] = JsonSerializer.SerializeToElement(rateDefinition.Currency),
                    ["interestRate"] = JsonSerializer.SerializeToElement(rateDefinition.InterestRate),
                    ["valuationDate"] = JsonSerializer.SerializeToElement(valuationDate.ToString("O"))
                }
            ];
        }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(CurrencyRateDefinitionProvider));
        }

        private static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetDouble(definition, propertyName, nameof(CurrencyRateDefinitionProvider));
        }

        private static string GetStringOrDefault(
            IReadOnlyDictionary<string, JsonElement> definition,
            string propertyName,
            string defaultValue)
        {
            return JsonDefinitionReader.GetStringOrDefault(definition, propertyName, defaultValue, nameof(CurrencyRateDefinitionProvider));
        }
    }
}
