using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Pass-through node that exposes an equity's ticking spot.
    /// </summary>
    public sealed class SpotNode : ISpotQuoteNode
    {
        private readonly EquitySource source;

        public SpotNode(EquitySource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.source.Tick += SourceTicked;
        }

        public event EventHandler<QuoteTick> Tick;

        public string Name => source.Name;

        public string Type => nameof(SpotNode);

        public double Spot => source.Spot;

        public string Currency => source.Currency;

        public IReadOnlyList<IGraphNode> Dependencies => [source];

        private void SourceTicked(object sender, QuoteTick e)
        {
            Tick?.Invoke(this, new QuoteTick(Name, e.Value));
        }
    }
}
