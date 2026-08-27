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
    /// Represents a graph node that raises quote-tick events.
    /// </summary>
    public interface ITickingNode : IGraphNode
    {
        event EventHandler<QuoteTick> Tick;
    }

    /// <summary>
    /// Represents the spot quote-producing node type.
    /// </summary>
    public interface ISpotQuoteNode : ITickingNode
    {
        double Spot { get; }

        string Currency { get; }
    }

    /// <summary>
    /// Represents a graph node that provides a volatility quote.
    /// </summary>
    public interface IVolQuoteNode : IGraphNode
    {
        double Volatility { get; }
    }

    /// <summary>
    /// Represents a graph node that provides a discount factor curve.
    /// </summary>
    public interface IDiscountFactorNode : IGraphNode
    {
        Func<DateTime, double> DiscountFactor { get; }

        string Currency { get; }
    }

    /// <summary>
    /// Represents a graph node that provides a forward curve derived from a spot and a discount factor curve.
    /// </summary>
    public interface IForwardCurveNode : ITickingNode
    {
        Func<DateTime, double> Forward { get; }

        string Currency { get; }
    }

    /// <summary>
    /// Represents an equity option price derived from an equity, its volatility, and a discount factor curve.
    /// </summary>
    public interface IEquityOptionNode : ITickingNode
    {
        double Price { get; }

        string Currency { get; }
    }

}