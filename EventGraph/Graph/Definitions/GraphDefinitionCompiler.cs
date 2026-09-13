using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    internal static class GraphDefinitionCompiler
    {
        private static readonly Dictionary<string, Func<IReadOnlyDictionary<string, JsonElement>, DateTime, IReadOnlyDictionary<string, double>, IReadOnlyList<IReadOnlyDictionary<string, JsonElement>>>> Compilers =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(EquityDefinition)] = EquityDefinitionProvider.Compile,
                [nameof(CurrencyRateDefinition)] = CurrencyRateDefinitionProvider.Compile,
                [nameof(BasketDefinition)] = BasketDefinitionProvider.Compile,
                [nameof(EquityOptionDefinition)] = EquityOptionDefinitionProvider.Compile
            };

        public static IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> Compile(
            IReadOnlyDictionary<string, JsonElement> definition,
            DateTime? valuationDate = null,
            IReadOnlyDictionary<string, double> initialSpots = null)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var effectiveDate = (valuationDate ?? DateTime.Today).Date;
            var type = JsonDefinitionReader.GetString(definition, "type", "graph definition");
            return Compilers.TryGetValue(type, out var compiler)
                ? compiler(definition, effectiveDate, initialSpots)
                : [definition];
        }
    }
}