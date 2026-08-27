using System;
using System.Collections.Generic;
using System.Linq;

namespace EventGraph
{
    /// <summary>
    /// Stores graph nodes in a stable indexed order with dependencies represented as node indices.
    /// </summary>
    public sealed class QuoteGraph
    {
        private readonly Dictionary<string, int> nodeIndexByName;

        public QuoteGraph(IReadOnlyList<IGraphNode> nodes)
        {
            ArgumentNullException.ThrowIfNull(nodes);

            Nodes = [.. nodes];
            QuoteNodes = [.. Nodes.OfType<ISpotQuoteNode>()];
            var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i] ?? throw new ArgumentException("Quote graph nodes cannot be null.", nameof(nodes));
                var key = GraphKey.Of(node.Type, node.Name);
                if (!indices.TryAdd(key, i))
                {
                    throw new ArgumentException($"Duplicate graph node key '{key}' detected.", nameof(nodes));
                }
            }

            nodeIndexByName = indices;
            DependenciesByNode = [.. Nodes.Select(node => (IReadOnlyList<int>)[.. node.Dependencies.Select(dependency => GetIndex(GraphKey.Of(dependency.Type, dependency.Name)))])];
            var dependentsByNode = Enumerable.Range(0, Nodes.Count)
                .Select(_ => new List<int>())
                .ToArray();
            for (int nodeIndex = 0; nodeIndex < DependenciesByNode.Count; nodeIndex++)
            {
                foreach (var dependencyIndex in DependenciesByNode[nodeIndex])
                {
                    dependentsByNode[dependencyIndex].Add(nodeIndex);
                }
            }

            DependentsByNode = [.. dependentsByNode.Select(dependents => (IReadOnlyList<int>)[.. dependents])];
        }

        public IReadOnlyList<IGraphNode> Nodes { get; }

        public IReadOnlyList<ISpotQuoteNode> QuoteNodes { get; }

        public IReadOnlyDictionary<string, int> NodeIndexByName => nodeIndexByName;

        public IReadOnlyList<IReadOnlyList<int>> DependenciesByNode { get; }

        public IReadOnlyList<IReadOnlyList<int>> DependentsByNode { get; }

        public int GetIndex(string name)
        {
            // Try as a canonical key first, then as a bare name resolved to a single match.
            if (nodeIndexByName.TryGetValue(name, out var index))
            {
                return index;
            }

            var matches = nodeIndexByName
                .Where(pair => pair.Key.EndsWith($"::{name}", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count == 1)
            {
                return matches[0].Value;
            }

            throw new KeyNotFoundException($"Graph node '{name}' was not found in the graph.");
        }
    }
}