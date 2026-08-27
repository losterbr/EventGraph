using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Pass-through node that exposes a currency's flat interest rate.
    /// </summary>
    public sealed class RateNode : IGraphNode
    {
        private readonly CurrencyRateSource source;

        public RateNode(CurrencyRateSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public string Name => source.Name;

        public string Type => nameof(RateNode);

        public double InterestRate => source.InterestRate;

        public IReadOnlyList<IGraphNode> Dependencies => [source];
    }
}
