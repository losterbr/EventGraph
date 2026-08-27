using System.Text.Json;

namespace EventGraph.Tests
{
    public class EquityOptionTests
    {
        [Fact]
        public void EquityOptionCalculatesBlackScholesCallPrice()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var discountFactor = new RateCurveSource("USD", 0.05);
            var maturity = DateTime.Today.AddYears(1);

            var option = new EquityOption("AAPL_CALL", equity, discountFactor, maturity, 100.0);

            Assert.Equal(7.5771, option.Price, precision: 3);
            Assert.Equal([equity, discountFactor], option.Dependencies);
        }

        [Fact]
        public void EquityOptionRejectsBlankName()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var discountFactor = new RateCurveSource("USD", 0.05);

            _ = Assert.Throws<ArgumentException>(() => new EquityOption(" ", equity, discountFactor, DateTime.Today.AddYears(1), 100.0));
        }

        [Fact]
        public void EquityOptionRejectsPastMaturity()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var discountFactor = new RateCurveSource("USD", 0.05);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOption("AAPL_CALL", equity, discountFactor, DateTime.Today, 100.0));
        }

        [Fact]
        public void EquityOptionRejectsNonPositiveStrike()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var discountFactor = new RateCurveSource("USD", 0.05);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOption("AAPL_CALL", equity, discountFactor, DateTime.Today.AddYears(1), 0.0));
        }

        [Fact]
        public async Task EquityOptionPublishesARecalculatedPriceWhenEquityTicks()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var discountFactor = new RateCurveSource("USD", 0.05);
            var option = new EquityOption("AAPL_CALL", equity, discountFactor, DateTime.Today.AddYears(1), 100.0);
            QuoteTick? update = null;
            option.PriceTick += (_, message) => update = message;

            await equity.Start(1);

            Assert.NotNull(update);
            Assert.Equal(option.Price, update.Value);
        }

        [Fact]
        public void EquityOptionLoadsItsDependenciesAndOneYearMaturityFromJson()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var discountFactor = new RateCurveSource("USD", 0.05);
            using var document = JsonDocument.Parse("{\"name\":\"AAPL_CALL\",\"constituent\":\"AAPL\",\"discountFactor\":\"USD\",\"maturity\":\"1Y\",\"strike\":100}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var option = new EquityOption(definition, new Dictionary<string, IGraphNode>
            {
                [equity.Name] = equity,
                [discountFactor.Name] = discountFactor
            });

            Assert.Equal(DateTime.Today.AddYears(1), option.Maturity);
            Assert.Equal(100.0, option.Strike);
        }
    }
}