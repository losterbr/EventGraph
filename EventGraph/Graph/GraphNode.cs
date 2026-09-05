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
    /// Represents a graph node that provides a ticking spot value.
    /// </summary>
    public interface ISpotValueNode : ITickingNode
    {
        double Spot { get; }
    }

    /// <summary>
    /// Represents a graph source that provides a spot value for a SpotNode to expose.
    /// </summary>
    public interface ISpotSourceNode : ISpotValueNode
    {
    }

    /// <summary>
    /// Represents a graph node that exposes a spot quote for downstream consumers.
    /// </summary>
    public interface ISpotNode : ISpotValueNode
    {
    }

    /// <summary>
    /// Represents a graph node that provides a volatility quote.
    /// </summary>
    public interface IVolSourceNode : IGraphNode
    {
        double Volatility { get; }
    }

    /// <summary>
    /// Represents a graph node that exposes a volatility quote for downstream consumers.
    /// </summary>
    public interface IVolNode : IGraphNode
    {
        double Volatility { get; }
    }

    /// <summary>
    /// Represents a graph node that provides a flat interest rate quote.
    /// </summary>
    public interface IRateSourceNode : IGraphNode
    {
        double InterestRate { get; }

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
    /// Represents an internal graph node that provides a discount factor curve derived from a flat rate.
    /// </summary>
    public interface IDiscountCurveNode : IGraphNode
    {
        Func<DateTime, double> DiscountFactor { get; }

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