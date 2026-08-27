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

        /// <summary>
        /// Checks one node and all of its dependencies using depth-first traversal.
        ///
        /// A node is "visiting" while this call is still walking through its dependency
        /// subtree. The visiting set therefore represents the current path through the
        /// graph, not every node encountered so far. Reaching a node already in this set
        /// means that the current path has looped back to an ancestor, so the graph has a
        /// cycle.
        ///
        /// A node is "visited" only after its dependency subtree has been accepted. The
        /// visited set represents nodes whose dependencies have already been completely
        /// checked. Encountering one of these nodes again is safe and lets shared
        /// dependencies be skipped without traversing them twice.
        ///
        /// The steps are:
        /// 1. Reject a null node.
        /// 2. Reject the node if it is already being visited on the current path.
        /// 3. Return if the node has already been fully visited.
        /// 4. Mark the node as visiting.
        /// 5. Visit each dependency recursively.
        /// 6. Remove the node from visiting after all dependencies pass.
        /// 7. Mark the node as visited so later traversals can skip it.
        /// </summary>
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

            if (visited.Contains(node))
            {
                return;
            }

            _ = visiting.Add(node);
            foreach (var dependency in node.Dependencies)
            {
                Visit(dependency, visiting, visited);
            }

            _ = visiting.Remove(node);
            _ = visited.Add(node);
        }
    }
}