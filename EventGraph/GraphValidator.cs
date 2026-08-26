using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Validates the dependency graph before quote processing begins.
    /// </summary>
    public static class GraphValidator
    {
        public static void EnsureAcyclic(IEnumerable<IGraphNode> roots)
        {
            ArgumentNullException.ThrowIfNull(roots);

            var visiting = new HashSet<IGraphNode>(ReferenceEqualityComparer.Instance);
            var visited = new HashSet<IGraphNode>(ReferenceEqualityComparer.Instance);

            foreach (var root in roots)
            {
                Visit(root, visiting, visited);
            }
        }

        private static void Visit(
            IGraphNode node,
            HashSet<IGraphNode> visiting,
            HashSet<IGraphNode> visited)
        {
            if (node == null)
            {
                throw new ArgumentException("Graph nodes cannot be null.", nameof(node));
            }

            if (visiting.Contains(node))
            {
                throw new InvalidOperationException($"Cycle detected at graph node '{node.Name}'.");
            }

            if (!visited.Add(node))
            {
                return;
            }

            _ = visiting.Add(node);
            foreach (var dependency in node.Dependencies)
            {
                Visit(dependency, visiting, visited);
            }

            _ = visiting.Remove(node);
        }
    }
}