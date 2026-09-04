using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Prices a one-constituent equity option with the Black-Scholes formula.
    /// </summary>
    public sealed class EquityOptionNode : IEquityOptionNode
    {
        private readonly IForwardCurveNode forwardNode;
        private readonly IVolNode volatilitySource;
        private readonly IDiscountCurveNode discountCurveNode;

        public EquityOptionNode(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
            : this(
                GetString(definition, "name"),
                GetNode<IForwardCurveNode>(definition, "forward", nodesByName),
                GetNode<IVolNode>(definition, "volatility", nodesByName),
                GetNode<IDiscountCurveNode>(definition, "discountCurve", nodesByName),
                GetMaturity(definition),
                GetDouble(definition, "strike"),
                GetOptionType(definition))
        {
        }

        public EquityOptionNode(
            string name,
            IForwardCurveNode forwardNode,
            IVolNode volatilitySource,
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

        public string Type => nameof(EquityOptionNode);

        public IReadOnlyList<IGraphNode> Dependencies => [forwardNode, volatilitySource, discountCurveNode];

        internal static IGraphNode Create(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
        {
            return new EquityOptionNode(definition, nodesByName);
        }

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return [GetString(definition, "forward"), GetString(definition, "volatility"), GetString(definition, "discountCurve")];
        }

        internal static IReadOnlyDictionary<string, JsonElement> EnrichDefinition(
            GraphDefinitionEnrichmentContext context,
            IReadOnlyDictionary<string, JsonElement> definition)
        {
            var underlyer = GraphDefinitionEnrichmentContext.GetString(definition, "underlyer");
            var sourceDefinition = context.GetDefinition(GraphKey.Of(nameof(EquitySource), underlyer));
            var currency = GraphDefinitionEnrichmentContext.GetOptionalString(sourceDefinition, "currency") ?? "USD";
            var rateSourceName = GetRateSourceName(context.Definitions, currency);

            context.AddSyntheticIfMissing(nameof(SpotNode), underlyer);
            context.AddSyntheticIfMissing(nameof(VolatilityNode), underlyer);
            context.AddSyntheticIfMissing(nameof(RateCurveNode), rateSourceName);
            context.AddSyntheticIfMissing(nameof(ForwardCurveNode), underlyer, new Dictionary<string, JsonElement>
            {
                ["spot"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(SpotNode), underlyer)),
                ["discountCurve"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(RateCurveNode), rateSourceName))
            });

            return new Dictionary<string, JsonElement>(definition, StringComparer.OrdinalIgnoreCase)
            {
                ["forward"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(ForwardCurveNode), underlyer)),
                ["volatility"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(VolatilityNode), underlyer)),
                ["discountCurve"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(RateCurveNode), rateSourceName))
            };
        }

        private static string GetRateSourceName(
            IEnumerable<IReadOnlyDictionary<string, JsonElement>> definitions,
            string currency)
        {
            var matches = definitions
                .Where(definition => string.Equals(GraphDefinitionEnrichmentContext.GetType(definition), nameof(CurrencyRateSource), StringComparison.Ordinal))
                .Where(definition => string.Equals(GraphDefinitionEnrichmentContext.GetOptionalString(definition, "currency") ?? GraphDefinitionEnrichmentContext.GetNodeName(definition), currency, StringComparison.OrdinalIgnoreCase))
                .Select(GraphDefinitionEnrichmentContext.GetNodeName)
                .ToList();
            return matches.Count switch
            {
                1 => matches[0],
                0 => throw new InvalidDataException($"No CurrencyRateSource found for currency '{currency}'."),
                _ => throw new InvalidDataException($"Multiple CurrencyRateSource definitions found for currency '{currency}'.")
            };
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
            var key = GetString(definition, propertyName);
            return nodesByName != null && nodesByName.TryGetValue(key, out var node) && node is TNode typedNode
                ? typedNode
                : throw new InvalidDataException($"EquityOptionNode references an invalid {propertyName} '{key}'.");
        }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(EquityOptionNode));
        }

        private static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetDouble(definition, propertyName, nameof(EquityOptionNode));
        }

        private static EquityOptionType GetOptionType(IReadOnlyDictionary<string, JsonElement> definition)
        {
            var optionType = GetString(definition, "optionType");
            return Enum.TryParse<EquityOptionType>(optionType, ignoreCase: true, out var parsed)
                ? parsed
                : throw new InvalidDataException("EquityOptionNode optionType must be Call or Put.");
        }
    }
}