using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Central registry of supported graph-node implementations.
    /// New node types should be added here, not in NodeGraphLoader.
    /// </summary>
    public static class NodeRegistry
    {
        private static readonly Dictionary<string, Func<IReadOnlyDictionary<string, JsonElement>, IReadOnlyList<string>>> DependencyResolvers =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(BasketAggregate)] = BasketAggregate.GetDependencyNames,
                [nameof(EquityOption)] = EquityOption.GetDependencyNames
            };

        private static readonly IReadOnlyDictionary<string, Func<IReadOnlyDictionary<string, JsonElement>, IReadOnlyDictionary<string, IGraphNode>, IGraphNode>> Factories =
            new Dictionary<string, Func<IReadOnlyDictionary<string, JsonElement>, IReadOnlyDictionary<string, IGraphNode>, IGraphNode>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(SimulatedAssetSource)] = (definition, _) => new SimulatedAssetSource(definition),
                [nameof(BasketAggregate)] = (definition, nodesByName) => new BasketAggregate(definition, nodesByName),
                [nameof(RateCurveSource)] = (definition, _) => new RateCurveSource(definition),
                [nameof(EquityOption)] = (definition, nodesByName) => new EquityOption(definition, nodesByName)
            };

        public static IReadOnlyCollection<string> SupportedTypes => [.. Factories.Keys];

        public static bool IsSupportedType(string type)
        {
            return !string.IsNullOrWhiteSpace(type) && Factories.ContainsKey(type);
        }

        public static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var type = GetType(definition);
            return DependencyResolvers.TryGetValue(type, out var resolver)
                ? resolver(definition)
                : [];
        }

        public static IGraphNode CreateNode(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var type = GetType(definition);
            return !Factories.TryGetValue(type, out var factory)
                ? throw new InvalidDataException($"Unsupported graph node type: '{type}'.")
                : factory(definition, nodesByName);
        }

        private static string GetType(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return !definition.TryGetValue("type", out var type) || type.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(type.GetString())
                ? throw new InvalidDataException("Every graph definition must provide a non-empty string type.")
                : type.GetString();
        }
    }
}
