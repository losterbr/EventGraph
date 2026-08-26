using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Provides backward-compatible access to graph loading by its previous name.
    /// </summary>
    [Obsolete("Use NodeGraphLoader instead.")]
    public static class GraphDefinitionLoader
    {
        public static IReadOnlyList<IQuoteTickNode> LoadNodes(string directoryPath)
        {
            return NodeGraphLoader.LoadNodes(directoryPath);
        }
    }
}
