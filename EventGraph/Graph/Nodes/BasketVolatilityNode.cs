using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Provides the initial fixed volatility assumption for a basket.
    /// </summary>
    public sealed class BasketVolatilityNode(BasketSpotNode basketNode) : IVolNode
    {
        private readonly BasketSpotNode basket = basketNode ?? throw new ArgumentNullException(nameof(basketNode));

        public string Name => basket.Name;

        public string Type => nameof(BasketVolatilityNode);

        public double Volatility => 0.30;

        public IReadOnlyList<IGraphNode> Dependencies => [basket];

        internal static IGraphNode Create(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
        {
            return new BasketVolatilityNode(GraphNodeResolver.ResolveByName<BasketSpotNode>(definition, nodesByName, nameof(BasketSpotNode)));
        }

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return [GraphKey.Of(nameof(BasketSpotNode), GraphDefinitionEnrichmentContext.GetNodeName(definition))];
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