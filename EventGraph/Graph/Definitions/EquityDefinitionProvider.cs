using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Provides an equity definition loaded from JSON.
    /// </summary>
    public sealed class EquityDefinitionProvider(string name, double spot, double volatility, double meanTickTimeSeconds = 1.0, string currency = "USD") : IDefinitionProvider<EquityDefinition>
    {
        public EquityDefinitionProvider(IReadOnlyDictionary<string, JsonElement> definition)
            : this(
                GetString(definition, "name"),
                GetDouble(definition, "spot"),
                GetDouble(definition, "volatility"),
                GetDoubleOrDefault(definition, "meanTickTimeSeconds", 1.0),
                GetStringOrDefault(definition, "currency", "USD"))
        {
        }

        public EquityDefinition Definition { get; } = new(name, spot, volatility, meanTickTimeSeconds, currency);

        internal static IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> Compile(
            IReadOnlyDictionary<string, JsonElement> definition,
            DateTime valuationDate,
            IReadOnlyDictionary<string, double> initialSpots)
        {
            var equityDefinition = new EquityDefinitionProvider(definition).Definition;
            var spot = initialSpots != null && initialSpots.TryGetValue(equityDefinition.Name, out var preservedSpot)
                ? preservedSpot
                : equityDefinition.Spot;

            return
            [
                new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                {
                    ["type"] = JsonSerializer.SerializeToElement(nameof(EquitySource)),
                    ["name"] = JsonSerializer.SerializeToElement(equityDefinition.Name),
                    ["currency"] = JsonSerializer.SerializeToElement(equityDefinition.Currency),
                    ["spot"] = JsonSerializer.SerializeToElement(spot),
                    ["volatility"] = JsonSerializer.SerializeToElement(equityDefinition.Volatility),
                    ["meanTickTimeSeconds"] = JsonSerializer.SerializeToElement(equityDefinition.MeanTickTimeSeconds),
                    ["valuationDate"] = JsonSerializer.SerializeToElement(valuationDate.ToString("O"))
                }
            ];
        }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(EquityDefinitionProvider));
        }

        private static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetDouble(definition, propertyName, nameof(EquityDefinitionProvider));
        }

        private static double GetDoubleOrDefault(IReadOnlyDictionary<string, JsonElement> definition, string propertyName, double defaultValue)
        {
            return JsonDefinitionReader.GetDoubleOrDefault(definition, propertyName, defaultValue, nameof(EquityDefinitionProvider));
        }

        private static string GetStringOrDefault(
            IReadOnlyDictionary<string, JsonElement> definition,
            string propertyName,
            string defaultValue)
        {
            return JsonDefinitionReader.GetStringOrDefault(definition, propertyName, defaultValue, nameof(EquityDefinitionProvider));
        }
    }
}
