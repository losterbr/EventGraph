using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Pass-through node that exposes an equity's constant volatility.
    /// </summary>
    public sealed class VolatilityNode(IVolSourceNode sourceNode) : IVolNode
    {
        private readonly IVolSourceNode source = sourceNode ?? throw new ArgumentNullException(nameof(sourceNode));

        public string Name => source.Name;

        public string Type => nameof(VolatilityNode);

        public double Volatility => source.Volatility;

        public IReadOnlyList<IGraphNode> Dependencies => [source];
    }
}