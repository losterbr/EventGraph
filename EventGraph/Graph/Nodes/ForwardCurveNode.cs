using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Derives a forward curve from an equity's spot and a discount factor curve.
    /// </summary>
    public sealed class ForwardCurveNode : IForwardCurveNode
    {
        private readonly ISpotSourceNode spotNode;
        private readonly IDiscountCurveNode discountCurveNode;

        public ForwardCurveNode(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
            : this(
                GetNode<ISpotSourceNode>(definition, "spot", nodesByName),
                GetNode<IDiscountCurveNode>(definition, "discountCurve", nodesByName))
        {
        }

        public ForwardCurveNode(ISpotSourceNode spotNode, IDiscountCurveNode discountCurveNode)
        {
            ArgumentNullException.ThrowIfNull(spotNode);
            ArgumentNullException.ThrowIfNull(discountCurveNode);
            if (!string.Equals(spotNode.Currency, discountCurveNode.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Forward curve and discount curve currencies must match.", nameof(discountCurveNode));
            }

            this.spotNode = spotNode;
            this.discountCurveNode = discountCurveNode;
            Forward = maturity => this.spotNode.Spot / this.discountCurveNode.DiscountFactor(maturity);
            this.spotNode.Tick += SpotTicked;
        }

        public event EventHandler<QuoteTick> Tick;

        public string Name => spotNode.Name;

        public string Type => nameof(ForwardCurveNode);

        public string Currency => discountCurveNode.Currency;

        public Func<DateTime, double> Forward { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [spotNode, discountCurveNode];

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return [GetString(definition, "spot"), GetString(definition, "discountCurve")];
        }

        private void SpotTicked(object sender, QuoteTick e)
        {
            Tick?.Invoke(this, new QuoteTick(Name, spotNode.Spot));
        }

        private static TNode GetNode<TNode>(
            IReadOnlyDictionary<string, JsonElement> definition,
            string propertyName,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
            where TNode : class, IGraphNode
        {
            var nodeName = GetString(definition, propertyName);
            return nodesByName != null && nodesByName.TryGetValue(nodeName, out var node) && node is TNode typedNode
                ? typedNode
                : throw new InvalidDataException($"ForwardCurveNode references an invalid {propertyName} '{nodeName}'.");
        }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(ForwardCurveNode));
        }
    }
}
