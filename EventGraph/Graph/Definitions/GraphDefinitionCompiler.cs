using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    internal static class GraphDefinitionCompiler
    {
        private static readonly Dictionary<string, Func<IReadOnlyDictionary<string, JsonElement>, IReadOnlyList<IReadOnlyDictionary<string, JsonElement>>>> Compilers =
            new Dictionary<string, Func<IReadOnlyDictionary<string, JsonElement>, IReadOnlyList<IReadOnlyDictionary<string, JsonElement>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(BasketDefinition)] = BasketDefinitionProvider.Compile
            };

        public static IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> Compile(IReadOnlyDictionary<string, JsonElement> definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var type = JsonDefinitionReader.GetString(definition, "type", "graph definition");
            return Compilers.TryGetValue(type, out var compiler)
                ? compiler(definition)
                : [definition];
        }
    }
}