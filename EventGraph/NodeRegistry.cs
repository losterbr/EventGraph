using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Central registry of supported graph-node implementations.
    /// New node types should be added here, not in NodeGraphLoader.
    /// </summary>
    public static class NodeRegistry
    {
        private static readonly IReadOnlyDictionary<string, GraphNodeRegistration> Registrations =
            new[]
            {
                typeof(EquitySource),
                typeof(CurrencyRateSource),
                typeof(SpotNode),
                typeof(VolatilityNode),
                typeof(BasketSpotNode),
                typeof(RateCurveNode),
                typeof(ForwardCurveNode),
                typeof(EquityOptionNode)
            }
            .Select(type => new GraphNodeRegistration(type))
            .ToDictionary(registration => registration.NodeType, StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<string> SupportedTypes => [.. Registrations.Keys];

        public static bool IsSupportedType(string type)
        {
            return !string.IsNullOrWhiteSpace(type) && Registrations.ContainsKey(type);
        }

        public static bool IsSourceType(string type)
        {
            return !string.IsNullOrWhiteSpace(type) && Registrations.TryGetValue(type, out var registration) && registration.IsSource;
        }

        public static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            return GetRegistration(definition).GetDependencyNames(definition);
        }

        internal static IReadOnlyDictionary<string, JsonElement> EnrichDefinition(
            GraphDefinitionEnrichmentContext context,
            IReadOnlyDictionary<string, JsonElement> definition)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(definition);

            return GetRegistration(definition).EnrichDefinition(context, definition);
        }

        internal static string GetNodeKey(IReadOnlyDictionary<string, JsonElement> definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var registration = GetRegistration(definition);
            return GraphKey.Of(registration.NodeType, registration.GetNodeName(definition));
        }

        public static IGraphNode CreateNode(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
        {
            ArgumentNullException.ThrowIfNull(definition);

            return GetRegistration(definition).Create(definition, nodesByName);
        }

        private static string GetType(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return !definition.TryGetValue("type", out var type) || type.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(type.GetString())
                ? throw new InvalidDataException("Every graph definition must provide a non-empty string type.")
                : type.GetString();
        }

        private static GraphNodeRegistration GetRegistration(IReadOnlyDictionary<string, JsonElement> definition)
        {
            var type = GetType(definition);
            return Registrations.TryGetValue(type, out var registration)
                ? registration
                : throw new InvalidDataException($"Unsupported graph node type: '{type}'.");
        }

    }
}
