using System.Text.Json;

namespace EventGraph.Tests
{
    public class RateCurveSourceTests
    {
        [Fact]
        public void DiscountFactorEvaluatesFromTodayUsingContinuousCompounding()
        {
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.02));

            var source = new RateCurveSource(rateNode);

            Assert.Equal(Math.Exp(-0.02), source.DiscountFactor(DateTime.Today.AddDays(365)), precision: 12);
            Assert.Equal(1.0, source.DiscountFactor(DateTime.Today), precision: 12);
        }

        [Fact]
        public void RateCurveSourceLoadsItsRateNodeFromJson()
        {
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.02));
            using var document = JsonDocument.Parse("{\"rate\":\"RateNode::USD\"}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var source = new RateCurveSource(definition, new Dictionary<string, IGraphNode>
            {
                ["RateNode::USD"] = rateNode
            });

            Assert.Equal("USD", source.Name);
            Assert.Equal("USD", source.Currency);
            Assert.Equal(0.02, source.InterestRate);
            Assert.Equal(nameof(RateCurveSource), source.Type);
            Assert.Equal([rateNode], source.Dependencies);
        }
    }
}