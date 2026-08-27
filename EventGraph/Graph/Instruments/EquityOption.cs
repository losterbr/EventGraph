using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Prices a one-constituent equity option with the Black-Scholes formula.
    /// </summary>
    public sealed class EquityOption : IEquityOptionNode
    {
        private readonly IForwardCurveNode forwardNode;
        private readonly IVolQuoteNode volatilitySource;
        private readonly IDiscountCurveNode discountCurveNode;

        public EquityOption(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
            : this(
                GetString(definition, "name"),
                GetNode<IForwardCurveNode>(definition, "constituent", nodesByName),
                GetNode<IVolQuoteNode>(definition, "volatility", nodesByName),
                GetNode<IDiscountCurveNode>(definition, "discountCurve", nodesByName),
                GetMaturity(definition),
                GetDouble(definition, "strike"),
                GetOptionType(definition))
        {
        }

        public EquityOption(
            string name,
            IForwardCurveNode forwardNode,
            IVolQuoteNode volatilitySource,
            IDiscountCurveNode discountCurveNode,
            DateTime maturity,
            double strike,
            EquityOptionType optionType = EquityOptionType.Call)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Option name cannot be empty.", nameof(name));
            }

            ArgumentNullException.ThrowIfNull(forwardNode);
            ArgumentNullException.ThrowIfNull(volatilitySource);
            ArgumentNullException.ThrowIfNull(discountCurveNode);
            if (!string.Equals(forwardNode.Currency, discountCurveNode.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Option and underlying currencies must match.", nameof(discountCurveNode));
            }

            if (maturity.Date <= DateTime.Today)
            {
                throw new ArgumentOutOfRangeException(nameof(maturity), "Option maturity must be after today.");
            }

            if (strike <= 0.0 || double.IsNaN(strike) || double.IsInfinity(strike))
            {
                throw new ArgumentOutOfRangeException(nameof(strike), "Option strike must be a positive finite number.");
            }

            if (!Enum.IsDefined(optionType))
            {
                throw new ArgumentOutOfRangeException(nameof(optionType), "Option type must be Call or Put.");
            }

            Name = name;
            this.forwardNode = forwardNode;
            this.volatilitySource = volatilitySource;
            this.discountCurveNode = discountCurveNode;
            Maturity = maturity.Date;
            Strike = strike;
            OptionType = optionType;
            Price = CalculatePrice();
            this.forwardNode.Tick += ForwardTicked;
        }

        public event EventHandler<QuoteTick> Tick;

        public string Name { get; }

        public string Type => nameof(EquityOption);

        public IReadOnlyList<IGraphNode> Dependencies => [forwardNode, volatilitySource, discountCurveNode];

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return [GetString(definition, "constituent"), GetString(definition, "volatility"), GetString(definition, "discountCurve")];
        }

        public DateTime Maturity { get; }

        public double Strike { get; }

        public EquityOptionType OptionType { get; }

        public double Price { get; private set; }

        public string Currency => discountCurveNode.Currency;

        private void ForwardTicked(object sender, QuoteTick e)
        {
            Price = CalculatePrice();
            Tick?.Invoke(this, new QuoteTick(Name, Price));
        }

        private double CalculatePrice()
        {
            var timeToMaturity = (Maturity - DateTime.Today).TotalDays / 365.0;
            var volatility = volatilitySource.Volatility;
            var forward = forwardNode.Forward(Maturity);
            var discountFactor = discountCurveNode.DiscountFactor(Maturity);
            var standardDeviation = volatility * Math.Sqrt(timeToMaturity);
            if (standardDeviation <= 0.0)
            {
                return discountFactor * Math.Max(forward - Strike, 0.0);
            }

            var d1 = (Math.Log(forward / Strike) + (0.5 * volatility * volatility * timeToMaturity)) / standardDeviation;
            var d2 = d1 - standardDeviation;
            var normal = new MathNet.Numerics.Distributions.Normal(0.0, 1.0);
            return OptionType == EquityOptionType.Call
                ? discountFactor * ((forward * normal.CumulativeDistribution(d1)) - (Strike * normal.CumulativeDistribution(d2)))
                : discountFactor * ((Strike * normal.CumulativeDistribution(-d2)) - (forward * normal.CumulativeDistribution(-d1)));
        }

        private static DateTime GetMaturity(IReadOnlyDictionary<string, JsonElement> definition)
        {
            var maturity = GetString(definition, "maturity");
            return string.Equals(maturity, "1Y", StringComparison.OrdinalIgnoreCase)
                ? DateTime.Today.AddYears(1)
                : DateTime.Parse(maturity, CultureInfo.InvariantCulture);
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
                : throw new InvalidDataException($"EquityOption references an invalid {propertyName} '{nodeName}'.");
        }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(EquityOption));
        }

        private static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetDouble(definition, propertyName, nameof(EquityOption));
        }

        private static EquityOptionType GetOptionType(IReadOnlyDictionary<string, JsonElement> definition)
        {
            var optionType = GetString(definition, "optionType");
            return Enum.TryParse<EquityOptionType>(optionType, ignoreCase: true, out var parsed)
                ? parsed
                : throw new InvalidDataException("EquityOption optionType must be Call or Put.");
        }
    }
}