using System.Text.Json;

namespace EventGraph.Tests
{
    public class ForwardCurveTests
    {
        [Fact]
        public void ForwardEvaluatesSpotDividedByDiscountFactor()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var discountFactor = new RateCurveSource("USD", 0.05);
            var maturity = DateTime.Today.AddYears(1);

            var forward = new ForwardCurve("AAPL_FWD", equity, discountFactor);

            Assert.Equal(equity.Spot / discountFactor.DiscountFactor(maturity), forward.Forward(maturity), precision: 12);
            Assert.Equal(nameof(ForwardCurve), forward.Type);
            Assert.Equal("USD", forward.Currency);
            Assert.Equal([equity, discountFactor], forward.Dependencies);
        }

        [Fact]
        public void ForwardCurveRejectsBlankName()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var discountFactor = new RateCurveSource("USD", 0.05);

            _ = Assert.Throws<ArgumentException>(() => new ForwardCurve(" ", equity, discountFactor));
        }

        [Fact]
        public void ForwardCurveRejectsCurrencyMismatch()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0, "USD");
            var discountFactor = new RateCurveSource("EUR", 0.05, "EUR");

            var exception = Assert.Throws<ArgumentException>(() => new ForwardCurve("AAPL_FWD", equity, discountFactor));

            Assert.Contains("currencies must match", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ForwardCurveTicksWhenTheUnderlyingEquityTicks()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var discountFactor = new RateCurveSource("USD", 0.05);
            var forward = new ForwardCurve("AAPL_FWD", equity, discountFactor);
            QuoteTick? update = null;
            forward.Tick += (_, message) => update = message;

            await equity.Start(1);

            Assert.NotNull(update);
            Assert.Equal(equity.Spot, update.Value);
        }

        [Fact]
        public void ForwardCurveLoadsItsDependenciesFromJson()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var discountFactor = new RateCurveSource("USD", 0.05);
            using var document = JsonDocument.Parse("{\"name\":\"AAPL_FWD\",\"constituent\":\"AAPL\",\"currency\":\"USD\"}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var forward = new ForwardCurve(definition, new Dictionary<string, IGraphNode>
            {
                [equity.Name] = equity,
                [discountFactor.Name] = discountFactor
            });

            Assert.Equal("AAPL_FWD", forward.Name);
            Assert.Equal([equity, discountFactor], forward.Dependencies);
        }
    }
}
