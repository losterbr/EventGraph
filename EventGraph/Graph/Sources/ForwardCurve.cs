using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Derives a forward curve from an equity's spot and a discount factor curve.
    /// </summary>
    public sealed class ForwardCurve : IForwardCurveNode
    {
        private readonly ISpotQuoteNode equity;
        private readonly IDiscountFactorNode discountFactorNode;

        public ForwardCurve(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
            : this(
                GetString(definition, "name"),
                GetNode<ISpotQuoteNode>(definition, "constituent", nodesByName),
                GetNode<IDiscountFactorNode>(definition, "currency", nodesByName))
        {
        }

        public ForwardCurve(string name, ISpotQuoteNode equity, IDiscountFactorNode discountFactorNode)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Forward curve name cannot be empty.", nameof(name));
            }

            ArgumentNullException.ThrowIfNull(equity);
            ArgumentNullException.ThrowIfNull(discountFactorNode);
            if (!string.Equals(equity.Currency, discountFactorNode.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Forward curve and discount curve currencies must match.", nameof(discountFactorNode));
            }

            Name = name;
            this.equity = equity;
            this.discountFactorNode = discountFactorNode;
            Forward = maturity => this.equity.Spot / this.discountFactorNode.DiscountFactor(maturity);
            this.equity.Tick += EquityTicked;
        }

        public event EventHandler<QuoteTick> Tick;

        public string Name { get; }

        public string Type => nameof(ForwardCurve);

        public string Currency => discountFactorNode.Currency;

        public Func<DateTime, double> Forward { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [equity, discountFactorNode];

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return [GetString(definition, "constituent"), GetString(definition, "currency")];
        }

        private void EquityTicked(object sender, QuoteTick e)
        {
            Tick?.Invoke(this, new QuoteTick(Name, equity.Spot));
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
                : throw new InvalidDataException($"ForwardCurve references an invalid {propertyName} '{nodeName}'.");
        }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(ForwardCurve));
        }
    }
}
