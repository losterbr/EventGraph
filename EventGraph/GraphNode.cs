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
    /// Represents the spot quote-producing node type.
    /// </summary>
    public interface ISpotQuoteNode : IGraphNode
    {
        event EventHandler<QuoteTick> SpotTick;

        double Spot { get; }
    }

    /// <summary>
    /// Represents a graph node that provides a volatility quote.
    /// </summary>
    public interface IVolQuoteNode : IGraphNode
    {
        event EventHandler<QuoteTick> VolatilityTick;

        double Volatility { get; }
    }
}