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
    /// Represents a graph node that provides a date-based rate curve.
    /// </summary>
    public interface IDiscountFactorNode : IGraphNode
    {
        Func<DateTime, double> DiscountFactor { get; }

        string Currency { get; }
    }

    /// <summary>
    /// Represents an equity option price derived from an equity and a rate curve.
    /// </summary>
    public interface IEquityOptionNode : IGraphNode
    {
        event EventHandler<QuoteTick> PriceTick;

        double Price { get; }
    }
}