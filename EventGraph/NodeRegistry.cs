using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Central registry of supported quote-node implementations.
    /// New node types should be added here, not in NodeGraphLoader.
    /// </summary>
    public static class NodeRegistry
    {
        private static readonly IReadOnlyDictionary<string, Func<IReadOnlyDictionary<string, JsonElement>, IReadOnlyDictionary<string, IQuoteNode>, IQuoteNode>> Factories =
            new Dictionary<string, Func<IReadOnlyDictionary<string, JsonElement>, IReadOnlyDictionary<string, IQuoteNode>, IQuoteNode>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(SimulatedQuoteSource)] = (definition, _) => new SimulatedQuoteSource(definition),
                [nameof(BasketAggregate)] = (definition, nodesByName) => new BasketAggregate(definition, nodesByName)
            };

        public static IReadOnlyCollection<string> SupportedTypes => Factories.Keys.ToArray();

        public static bool IsSupportedType(string type)
        {
            return !string.IsNullOrWhiteSpace(type) && Factories.ContainsKey(type);
        }

        public static IQuoteNode CreateNode(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IQuoteNode> nodesByName)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var type = GetType(definition);
            if (!Factories.TryGetValue(type, out var factory))
            {
                throw new InvalidDataException($"Unsupported graph node type: '{type}'.");
            }

            return factory(definition, nodesByName);
        }

        private static string GetType(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (!definition.TryGetValue("type", out var type) || type.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(type.GetString()))
            {
                throw new InvalidDataException("Every graph definition must provide a non-empty string type.");
            }

            return type.GetString();
        }
    }
}
