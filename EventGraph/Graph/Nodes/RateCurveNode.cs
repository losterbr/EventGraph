using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Converts a currency's flat interest rate into a continuously compounded discount factor curve.
    /// </summary>
    public sealed class RateCurveNode : IDiscountCurveNode
    {
        private readonly IRateSourceNode source;

        public RateCurveNode(IRateSourceNode source, DateTime? valuationDate = null)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            ValuationDate = (valuationDate ?? DateTime.Today).Date;
            DiscountFactor = date => Math.Exp(-this.source.InterestRate * DateHelpers.YearFraction(ValuationDate, date));
        }

        public string Name => source.Name;

        public string Type => nameof(RateCurveNode);

        public double InterestRate => source.InterestRate;

        public string Currency => source.Currency;

        public DateTime ValuationDate { get; }

        public Func<DateTime, double> DiscountFactor { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [source];

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return [GraphKey.Of(nameof(CurrencyRateSource), GraphDefinitionEnrichmentContext.GetNodeName(definition))];
        }

        internal static IGraphNode Create(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
        {
            var valuationDate = GetValuationDate(definition);
            return new RateCurveNode(
                GraphNodeResolver.ResolveByName<IRateSourceNode>(definition, nodesByName, nameof(CurrencyRateSource)),
                valuationDate);
        }

        internal static IReadOnlyDictionary<string, JsonElement> EnrichDefinition(
            GraphDefinitionEnrichmentContext _,
            IReadOnlyDictionary<string, JsonElement> definition)
        {
            return definition;
        }

        internal static string GetNodeName(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return GraphDefinitionEnrichmentContext.GetNodeName(definition);
        }

        internal static bool IsSource()
        {
            return false;
        }

        private static DateTime? GetValuationDate(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return definition != null && definition.TryGetValue("valuationDate", out var prop) && prop.ValueKind == JsonValueKind.String && DateTime.TryParse(prop.GetString(), CultureInfo.InvariantCulture, out var date)
                ? date
                : null;
        }
    }
}