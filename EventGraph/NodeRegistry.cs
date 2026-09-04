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
        private static readonly HashSet<string> SourceTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(EquitySource),
            nameof(CurrencyRateSource)
        };

        private static readonly Dictionary<string, Func<IReadOnlyDictionary<string, JsonElement>, IReadOnlyList<string>>> DependencyResolvers =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(BasketSpotNode)] = BasketSpotNode.GetDependencyNames,
                [nameof(SpotNode)] = SpotNode.GetDependencyNames,
                [nameof(VolatilityNode)] = VolatilityNode.GetDependencyNames,
                [nameof(ForwardCurveNode)] = ForwardCurveNode.GetDependencyNames,
                [nameof(RateCurveNode)] = RateCurveNode.GetDependencyNames,
                [nameof(EquityOptionNode)] = EquityOptionNode.GetDependencyNames
            };

        private static readonly Dictionary<string, IGraphDefinitionEnricher> DefinitionEnrichers =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(EquitySource)] = new DelegateGraphDefinitionEnricher(EquitySource.EnrichDefinition),
                [nameof(ForwardCurveNode)] = new DelegateGraphDefinitionEnricher(ForwardCurveNode.EnrichDefinition),
                [nameof(BasketSpotNode)] = new DelegateGraphDefinitionEnricher(BasketSpotNode.EnrichDefinition),
                [nameof(EquityOptionNode)] = new DelegateGraphDefinitionEnricher(EquityOptionNode.EnrichDefinition)
            };

        private static readonly IReadOnlyDictionary<string, Func<IReadOnlyDictionary<string, JsonElement>, IReadOnlyDictionary<string, IGraphNode>, IGraphNode>> Factories =
            new Dictionary<string, Func<IReadOnlyDictionary<string, JsonElement>, IReadOnlyDictionary<string, IGraphNode>, IGraphNode>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(EquitySource)] = EquitySource.Create,
                [nameof(CurrencyRateSource)] = CurrencyRateSource.Create,
                [nameof(SpotNode)] = SpotNode.Create,
                [nameof(VolatilityNode)] = VolatilityNode.Create,
                [nameof(BasketSpotNode)] = BasketSpotNode.Create,
                [nameof(RateCurveNode)] = RateCurveNode.Create,
                [nameof(ForwardCurveNode)] = ForwardCurveNode.Create,
                [nameof(EquityOptionNode)] = EquityOptionNode.Create
            };

        public static IReadOnlyCollection<string> SupportedTypes => [.. Factories.Keys];

        public static bool IsSupportedType(string type)
        {
            return !string.IsNullOrWhiteSpace(type) && Factories.ContainsKey(type);
        }

        public static bool IsSourceType(string type)
        {
            return !string.IsNullOrWhiteSpace(type) && SourceTypes.Contains(type);
        }

        public static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var type = GetType(definition);
            return DependencyResolvers.TryGetValue(type, out var resolver)
                ? resolver(definition)
                : [];
        }

        internal static IReadOnlyDictionary<string, JsonElement> EnrichDefinition(
            GraphDefinitionEnrichmentContext context,
            IReadOnlyDictionary<string, JsonElement> definition)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(definition);

            var type = GetType(definition);
            return DefinitionEnrichers.TryGetValue(type, out var enricher)
                ? enricher.Enrich(context, definition)
                : definition;
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
