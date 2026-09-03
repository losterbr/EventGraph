using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Pass-through node that exposes an equity's constant volatility.
    /// </summary>
    public sealed class VolatilityNode : IVolSourceNode
    {
        private readonly EquitySource source;

        public VolatilityNode(EquitySource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public string Name => source.Name;

        public string Type => nameof(VolatilityNode);

        public double Volatility => source.Volatility;

        public IReadOnlyList<IGraphNode> Dependencies => [source];
    }
}