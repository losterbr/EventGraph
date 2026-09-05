using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Pass-through node that exposes a graph node's ticking spot.
    /// </summary>
    public sealed class SpotNode(ISpotSourceNode sourceNode) : ISpotNode, ISpotDefinitionProvider
    {
        private readonly ISpotSourceNode source = sourceNode ?? throw new ArgumentNullException(nameof(sourceNode));
        private EventHandler<QuoteTick> tick;

        public event EventHandler<QuoteTick> Tick
        {
            add
            {
                var shouldSubscribe = tick == null;
                tick += value;
                if (shouldSubscribe)
                {
                    source.Tick += SourceTicked;
                }
            }
            remove
            {
                tick -= value;
                if (tick == null)
                {
                    source.Tick -= SourceTicked;
                }
            }
        }

        public string Name => source.Name;

        public string Type => nameof(SpotNode);

        public double Spot => source.Spot;

        public SpotDefinition Definition => source is ISpotDefinitionProvider provider
            ? provider.Definition
            : throw new InvalidOperationException($"Spot source '{source.Name}' does not provide a spot definition.");

        public IReadOnlyList<IGraphNode> Dependencies => [source];

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return [GetDependencyName(definition)];
        }

        internal static string GetDependencyName(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return definition.TryGetValue("source", out var source) && source.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(source.GetString())
                ? source.GetString()
                : GraphKey.Of(nameof(EquitySource), GraphDefinitionEnrichmentContext.GetNodeName(definition));
        }

        internal static IGraphNode Create(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
        {
            return new SpotNode(GraphNodeResolver.ResolveByKey<ISpotSourceNode>(GetDependencyName(definition), nodesByName));
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

        private void SourceTicked(object sender, QuoteTick e)
        {
            tick?.Invoke(this, new QuoteTick(Name, e.Value));
        }
    }
}
