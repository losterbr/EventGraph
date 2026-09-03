using System.Text.Json;

namespace EventGraph.Tests
{
    public class ForwardCurveTests
    {
        [Fact]
        public void ForwardEvaluatesSpotDividedByDiscountFactor()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = new RateCurveNode(new CurrencyRateSource("USD", 0.05));
            var maturity = DateTime.Today.AddYears(1);

            var forward = new ForwardCurve(spotNode, discountFactor);

            Assert.Equal(equity.Spot / discountFactor.DiscountFactor(maturity), forward.Forward(maturity), precision: 12);
            Assert.Equal(nameof(ForwardCurve), forward.Type);
            Assert.Equal("USD", forward.Currency);
            Assert.Equal([spotNode, discountFactor], forward.Dependencies);
        }

        [Fact]
        public void ForwardCurveRejectsNullSpotNode()
        {
            var discountFactor = new RateCurveNode(new CurrencyRateSource("USD", 0.05));

            _ = Assert.Throws<ArgumentNullException>(() => new ForwardCurve(null, discountFactor));
        }

        [Fact]
        public void ForwardCurveRejectsNullDiscountCurveNode()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);

            _ = Assert.Throws<ArgumentNullException>(() => new ForwardCurve(spotNode, null));
        }

        [Fact]
        public void ForwardCurveRejectsCurrencyMismatch()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0, "USD");
            var spotNode = new SpotNode(equity);
            var eurDiscountFactor = new RateCurveNode(new CurrencyRateSource("EUR", 0.05));

            var exception = Assert.Throws<ArgumentException>(() => new ForwardCurve(spotNode, eurDiscountFactor));

            Assert.Contains("currencies must match", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ForwardCurveTicksWhenTheUnderlyingSpotNodeTicks()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = new RateCurveNode(new CurrencyRateSource("USD", 0.05));
            var forward = new ForwardCurve(spotNode, discountFactor);
            QuoteTick? update = null;
            forward.Tick += (_, message) => update = message;

            await equity.Start(1);

            Assert.NotNull(update);
            Assert.Equal(equity.Spot, update.Value);
        }

        [Fact]
        public void ForwardCurveLoadsItsDependenciesFromJson()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = new RateCurveNode(new CurrencyRateSource("USD", 0.05));
            using var document = JsonDocument.Parse("{\"spot\":\"SpotNode::AAPL\",\"discountCurve\":\"RateCurveNode::USD\"}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var forward = new ForwardCurve(definition, new Dictionary<string, IGraphNode>
            {
                ["SpotNode::AAPL"] = spotNode,
                ["RateCurveNode::USD"] = discountFactor
            });

            Assert.Equal("AAPL", forward.Name);
            Assert.Equal([spotNode, discountFactor], forward.Dependencies);
        }
    }
}
