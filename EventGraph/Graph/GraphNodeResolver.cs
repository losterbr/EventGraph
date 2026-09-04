using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EventGraph
{
    internal static class GraphNodeResolver
    {
        public static TNode ResolveByKey<TNode>(string key, IReadOnlyDictionary<string, IGraphNode> nodesByName)
            where TNode : class, IGraphNode
        {
            return nodesByName != null && nodesByName.TryGetValue(key, out var node) && node is TNode typedNode
                ? typedNode
                : throw new InvalidDataException($"Could not resolve '{key}' as {typeof(TNode).Name}.");
        }

        public static TNode ResolveByName<TNode>(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName,
            string sourceType)
            where TNode : class, IGraphNode
        {
            var name = GraphDefinitionEnrichmentContext.GetNodeName(definition);
            return ResolveByKey<TNode>(GraphKey.Of(sourceType, name), nodesByName);
        }
    }
}