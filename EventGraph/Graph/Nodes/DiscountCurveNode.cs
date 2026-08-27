using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Internal node that converts a flat rate into a continuously compounded discount factor curve.
    /// </summary>
    public sealed class DiscountCurveNode : IDiscountCurveNode
    {
        private readonly RateNode rateNode;

        public DiscountCurveNode(RateNode rateNode)
        {
            this.rateNode = rateNode ?? throw new ArgumentNullException(nameof(rateNode));
            DiscountFactor = date => Math.Exp(-this.rateNode.InterestRate * (date - DateTime.Today).TotalDays / 365.0);
        }

        public string Name => rateNode.Name;

        public string Type => nameof(DiscountCurveNode);

        public string Currency => rateNode.Name;

        public Func<DateTime, double> DiscountFactor { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [rateNode];
    }
}
