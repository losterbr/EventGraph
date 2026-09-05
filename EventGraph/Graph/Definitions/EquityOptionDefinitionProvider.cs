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

        internal static IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> Compile(IReadOnlyDictionary<string, JsonElement> definition)
        {
            var optionDefinition = new EquityOptionDefinitionProvider(definition).Definition;
            return
            [
                new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                {
                    ["type"] = JsonSerializer.SerializeToElement(nameof(EquityOptionNode)),
                    ["name"] = JsonSerializer.SerializeToElement(optionDefinition.Name),
                    ["underlyer"] = JsonSerializer.SerializeToElement(optionDefinition.Underlyer),
                    ["maturity"] = JsonSerializer.SerializeToElement(optionDefinition.Maturity),
                    ["strike"] = JsonSerializer.SerializeToElement(optionDefinition.Strike),
                    ["optionType"] = JsonSerializer.SerializeToElement(optionDefinition.OptionType)
                }
            ];
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