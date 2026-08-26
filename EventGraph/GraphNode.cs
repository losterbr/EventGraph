using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Represents a node in the dependency graph.
    /// </summary>
    public interface IGraphNode
    {
        string Name { get; }

        string Type { get; }

        IReadOnlyList<IGraphNode> Dependencies { get; }
    }

    /// <summary>
    /// Represents a graph node that produces a typed value.
    /// </summary>
    public interface IGraphNode<out TResult> : IGraphNode
    {
        TResult CurrentValue { get; }
    }

    /// <summary>
    /// Represents the current quote-tick-producing node type.
    /// </summary>
    public interface IQuoteTickNode : IGraphNode<double>
    {
        event EventHandler<QuoteTick> Tick;
    }
}