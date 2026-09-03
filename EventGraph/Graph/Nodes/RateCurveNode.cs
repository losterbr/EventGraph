using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Converts a currency's flat interest rate into a continuously compounded discount factor curve.
    /// </summary>
    public sealed class RateCurveNode : IDiscountCurveNode
    {
        private readonly IRateSourceNode source;

        public RateCurveNode(IRateSourceNode source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            DiscountFactor = date => Math.Exp(-this.source.InterestRate * (date - DateTime.Today).TotalDays / 365.0);
        }

        public string Name => source.Name;

        public string Type => nameof(RateCurveNode);

        public double InterestRate => source.InterestRate;

        public string Currency => source.Currency;

        public Func<DateTime, double> DiscountFactor { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [source];
    }
}