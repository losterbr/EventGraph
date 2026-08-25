using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Represents a quote-producing node and the nodes it depends on.
    /// </summary>
    public interface IQuoteNode
    {
        event EventHandler<QuoteTick> Tick;

        string Name { get; }

        string Type { get; }

        double CurrentValue { get; }

        IReadOnlyList<IQuoteNode> Dependencies { get; }
    }
}