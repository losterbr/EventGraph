using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Pass-through node that exposes an equity's constant volatility.
    /// </summary>
    public sealed class VolatilityNode(IVolSourceNode sourceNode) : IVolNode
    {
        private readonly IVolSourceNode source = sourceNode ?? throw new ArgumentNullException(nameof(sourceNode));

        public string Name => source.Name;

        public string Type => nameof(VolatilityNode);

        public double Volatility => source.Volatility;

        public IReadOnlyList<IGraphNode> Dependencies => [source];

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return [GraphKey.Of(nameof(EquitySource), GraphDefinitionEnrichmentContext.GetNodeName(definition))];
        }

        internal static IGraphNode Create(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
        {
            return new VolatilityNode(GraphNodeResolver.ResolveByName<IVolSourceNode>(definition, nodesByName, nameof(EquitySource)));
        }

        internal static IReadOnlyDictionary<string, JsonElement> EnrichDefinition(
            GraphDefinitionEnrichmentContext _,
            IReadOnlyDictionary<string, JsonElement> definition)
        {
            return definition;
        }

        internal static string GetNodeName(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return GraphDefinitionEnrichmentContext.GetNodeName(definition);
        }

        internal static bool IsSource()
        {
            return false;
        }
    }
}