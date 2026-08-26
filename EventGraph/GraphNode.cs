using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Represents a graph node that can publish quote ticks and depend on other graph nodes.
    /// </summary>
    public interface IGraphNode
    {
        event EventHandler<QuoteTick> Tick;

        string Name { get; }

        string Type { get; }

        double CurrentValue { get; }

        IReadOnlyList<IGraphNode> Dependencies { get; }
    }
}