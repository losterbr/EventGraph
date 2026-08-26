using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Validates the dependency graph before quote processing begins.
    /// </summary>
    public static class GraphValidator
    {
        public static void EnsureAcyclic(IEnumerable<IQuoteNode> roots)
        {
            ArgumentNullException.ThrowIfNull(roots);

            var visiting = new HashSet<IQuoteNode>(ReferenceEqualityComparer.Instance);
            var visited = new HashSet<IQuoteNode>(ReferenceEqualityComparer.Instance);

            foreach (var root in roots)
            {
                Visit(root, visiting, visited);
            }
        }

        private static void Visit(
            IQuoteNode node,
            HashSet<IQuoteNode> visiting,
            HashSet<IQuoteNode> visited)
        {
            if (node == null)
            {
                throw new ArgumentException("Graph nodes cannot be null.", nameof(node));
            }

            if (visiting.Contains(node))
            {
                throw new InvalidOperationException($"Cycle detected at quote node '{node.Name}'.");
            }

            if (!visited.Add(node))
            {
                return;
            }

            visiting.Add(node);
            foreach (var dependency in node.Dependencies)
            {
                Visit(dependency, visiting, visited);
            }

            visiting.Remove(node);
        }
    }
}