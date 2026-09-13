using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Provides an equity option definition loaded from JSON.
    /// </summary>
    public sealed class EquityOptionDefinitionProvider(string name, string underlyer, string maturity, double strike, string optionType) : IDefinitionProvider<EquityOptionDefinition>
    {
        public EquityOptionDefinitionProvider(IReadOnlyDictionary<string, JsonElement> definition)
            : this(
                GetString(definition, "name"),
                GetString(definition, "underlyer"),
                GetString(definition, "maturity"),
                GetDouble(definition, "strike"),
                GetString(definition, "optionType"))
        {
        }

        public EquityOptionDefinition Definition { get; } = new(name, underlyer, maturity, strike, optionType);

        internal static IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> Compile(
            IReadOnlyDictionary<string, JsonElement> definition,
            DateTime valuationDate,
            IReadOnlyDictionary<string, double> _)
        {
            var optionDefinition = new EquityOptionDefinitionProvider(definition).Definition;
            var maturityDate = GetMaturityDate(optionDefinition.Maturity, valuationDate);
            if (maturityDate <= valuationDate)
            {
                // Expired option contracts are omitted from the active graph for this valuation date
                return [];
            }

            return
            [
                new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                {
                    ["type"] = JsonSerializer.SerializeToElement(nameof(EquityOptionNode)),
                    ["name"] = JsonSerializer.SerializeToElement(optionDefinition.Name),
                    ["underlyer"] = JsonSerializer.SerializeToElement(optionDefinition.Underlyer),
                    ["maturity"] = JsonSerializer.SerializeToElement(optionDefinition.Maturity),
                    ["strike"] = JsonSerializer.SerializeToElement(optionDefinition.Strike),
                    ["optionType"] = JsonSerializer.SerializeToElement(optionDefinition.OptionType),
                    ["valuationDate"] = JsonSerializer.SerializeToElement(valuationDate.ToString("O"))
                }
            ];
        }

        private static DateTime GetMaturityDate(string maturity, DateTime valuationDate)
        {
            return DateHelpers.TryAddTenor(valuationDate, maturity, out var maturityDate)
                ? maturityDate
                : DateTime.Parse(maturity, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(EquityOptionDefinitionProvider));
        }

        private static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetDouble(definition, propertyName, nameof(EquityOptionDefinitionProvider));
        }
    }
}