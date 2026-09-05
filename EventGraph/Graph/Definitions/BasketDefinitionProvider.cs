using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Provides a basket definition loaded from JSON.
    /// </summary>
    public sealed class BasketDefinitionProvider : IDefinitionProvider<BasketDefinition>
    {
        public BasketDefinitionProvider(IReadOnlyDictionary<string, JsonElement> definition)
            : this(GetString(definition, "name"), GetConstituents(definition), GetWeights(definition))
        {
        }

        public BasketDefinitionProvider(string name, IReadOnlyList<string> constituents, IReadOnlyList<double> weights)
        {
            Definition = new BasketDefinition(name, constituents, weights);
        }

        public BasketDefinition Definition { get; }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(BasketDefinitionProvider));
        }

        private static IReadOnlyList<string> GetConstituents(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (definition == null || !definition.TryGetValue("constituents", out var constituents) || constituents.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("BasketDefinitionProvider requires a constituents array.");
            }

            return [.. constituents.EnumerateArray().Select(constituent =>
                constituent.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(constituent.GetString())
                    ? constituent.GetString()
                    : throw new InvalidDataException("BasketDefinitionProvider constituents must be non-empty strings."))];
        }

        private static IReadOnlyList<double> GetWeights(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (definition == null || !definition.TryGetValue("weights", out var weights) || weights.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("BasketDefinitionProvider requires a weights array.");
            }

            return [.. weights.EnumerateArray().Select(weight =>
                weight.ValueKind == JsonValueKind.Number && weight.TryGetDouble(out var value)
                    ? value
                    : throw new InvalidDataException("BasketDefinitionProvider weights must be numeric."))];
        }
    }
}