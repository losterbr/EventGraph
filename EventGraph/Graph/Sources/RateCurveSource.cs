using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Internal node that converts a flat interest rate into a continuously compounded discount factor curve.
    /// </summary>
    public sealed class RateCurveSource : IDiscountCurveNode
    {
        private readonly RateNode rateNode;

        public RateCurveSource(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
            : this(
                GetNode<RateNode>(definition, "rate", nodesByName))
        {
        }

        public RateCurveSource(RateNode rateNode)
        {
            this.rateNode = rateNode ?? throw new ArgumentNullException(nameof(rateNode));
            DiscountFactor = date => Math.Exp(-this.rateNode.InterestRate * (date - DateTime.Today).TotalDays / 365.0);
        }

        public string Name => rateNode.Name;

        public string Type => nameof(RateCurveSource);

        public double InterestRate => rateNode.InterestRate;

        public string Currency => rateNode.Currency;

        public Func<DateTime, double> DiscountFactor { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [rateNode];

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return [GetString(definition, "rate")];
        }

        private static TNode GetNode<TNode>(
            IReadOnlyDictionary<string, JsonElement> definition,
            string propertyName,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
            where TNode : class, IGraphNode
        {
            var key = GetString(definition, propertyName);
            return nodesByName != null && nodesByName.TryGetValue(key, out var node) && node is TNode typedNode
                ? typedNode
                : throw new InvalidDataException($"RateCurveSource references an invalid {propertyName} '{key}'.");
        }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(RateCurveSource));
        }
    }
}