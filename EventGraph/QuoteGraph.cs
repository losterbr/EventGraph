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

        public QuoteGraph(IReadOnlyList<IQuoteTickNode> nodes)
        {
            ArgumentNullException.ThrowIfNull(nodes);

            QuoteNodes = [.. nodes];
            Nodes = QuoteNodes;
            var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i] ?? throw new ArgumentException("Quote graph nodes cannot be null.", nameof(nodes));
                if (!indices.TryAdd(node.Name, i))
                {
                    throw new ArgumentException($"Duplicate graph node name '{node.Name}' detected.", nameof(nodes));
                }
            }

            nodeIndexByName = indices;
            DependenciesByNode = [.. Nodes.Select(node => (IReadOnlyList<int>)[.. node.Dependencies.Select(dependency => GetIndex(dependency.Name))])];
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

        public IReadOnlyList<IQuoteTickNode> QuoteNodes { get; }

        public IReadOnlyDictionary<string, int> NodeIndexByName => nodeIndexByName;

        public IReadOnlyList<IReadOnlyList<int>> DependenciesByNode { get; }

        public IReadOnlyList<IReadOnlyList<int>> DependentsByNode { get; }

        public int GetIndex(string name)
        {
            return nodeIndexByName.GetValueOrDefault(name, -1) is var index && index >= 0
                ? index
                : throw new KeyNotFoundException($"Graph node '{name}' was not found in the graph.");
        }
    }
}