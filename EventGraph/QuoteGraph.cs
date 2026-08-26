using System;
using System.Collections.Generic;
using System.Linq;

namespace EventGraph
{
    /// <summary>
    /// Stores quote nodes in a stable indexed order with dependencies represented as node indices.
    /// </summary>
    public sealed class QuoteGraph
    {
        private readonly Dictionary<string, int> nodeIndexByName;

        public QuoteGraph(IReadOnlyList<IQuoteNode> nodes)
        {
            ArgumentNullException.ThrowIfNull(nodes);

            Nodes = [.. nodes];
            var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i] ?? throw new ArgumentException("Quote graph nodes cannot be null.", nameof(nodes));
                if (!indices.TryAdd(node.Name, i))
                {
                    throw new ArgumentException($"Duplicate quote node name '{node.Name}' detected.", nameof(nodes));
                }
            }

            nodeIndexByName = indices;
            DependenciesByNode = [.. Nodes.Select(node => (IReadOnlyList<int>)[.. node.Dependencies.Select(dependency => GetIndex(dependency.Name))])];
        }

        public IReadOnlyList<IQuoteNode> Nodes { get; }

        public IReadOnlyDictionary<string, int> NodeIndexByName => nodeIndexByName;

        public IReadOnlyList<IReadOnlyList<int>> DependenciesByNode { get; }

        public int GetIndex(string name)
        {
            return nodeIndexByName.GetValueOrDefault(name, -1) is var index && index >= 0
                ? index
                : throw new KeyNotFoundException($"Quote node '{name}' was not found in the graph.");
        }
    }
}