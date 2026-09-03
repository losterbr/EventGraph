using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Pass-through node that exposes a graph node's ticking spot.
    /// </summary>
    public sealed class SpotNode(ISpotSourceNode sourceNode) : ISpotNode
    {
        private readonly ISpotSourceNode source = sourceNode ?? throw new ArgumentNullException(nameof(sourceNode));
        private EventHandler<QuoteTick> tick;

        public event EventHandler<QuoteTick> Tick
        {
            add
            {
                var shouldSubscribe = tick == null;
                tick += value;
                if (shouldSubscribe)
                {
                    source.Tick += SourceTicked;
                }
            }
            remove
            {
                tick -= value;
                if (tick == null)
                {
                    source.Tick -= SourceTicked;
                }
            }
        }

        public string Name => source.Name;

        public string Type => nameof(SpotNode);

        public double Spot => source.Spot;

        public string Currency => source.Currency;

        public IReadOnlyList<IGraphNode> Dependencies => [source];

        private void SourceTicked(object sender, QuoteTick e)
        {
            tick?.Invoke(this, new QuoteTick(Name, e.Value));
        }
    }
}
