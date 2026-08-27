using System.Text.Json;

namespace EventGraph.Tests
{
    public class EquityOptionTests
    {
        [Fact]
        public void EquityOptionCalculatesBlackScholesCallPrice()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.05));
            var discountFactor = new RateCurveSource(rateNode);
            var forward = new ForwardCurve(spotNode, discountFactor);
            var volatility = new VolatilitySource(equity);
            var maturity = DateTime.Today.AddYears(1);

            var option = new EquityOption("AAPL_CALL", forward, volatility, discountFactor, maturity, 100.0);

            Assert.Equal(10.4506, option.Price, precision: 3);
            Assert.Equal(EquityOptionType.Call, option.OptionType);
            Assert.Equal([forward, volatility, discountFactor], option.Dependencies);
            Assert.Equal("USD", option.Currency);
        }

        [Fact]
        public void EquityOptionCalculatesBlackScholesPutPrice()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.05));
            var discountFactor = new RateCurveSource(rateNode);
            var forward = new ForwardCurve(spotNode, discountFactor);
            var volatility = new VolatilitySource(equity);

            var option = new EquityOption("AAPL_PUT", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 100.0, EquityOptionType.Put);

            Assert.Equal(5.5736, option.Price, precision: 3);
            Assert.Equal(EquityOptionType.Put, option.OptionType);
        }

        [Fact]
        public void EquityOptionRejectsBlankName()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.05));
            var discountFactor = new RateCurveSource(rateNode);
            var forward = new ForwardCurve(spotNode, discountFactor);
            var volatility = new VolatilitySource(equity);

            _ = Assert.Throws<ArgumentException>(() => new EquityOption(" ", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 100.0));
        }

        [Fact]
        public void EquityOptionRejectsPastMaturity()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.05));
            var discountFactor = new RateCurveSource(rateNode);
            var forward = new ForwardCurve(spotNode, discountFactor);
            var volatility = new VolatilitySource(equity);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOption("AAPL_CALL", forward, volatility, discountFactor, DateTime.Today, 100.0));
        }

        [Fact]
        public void EquityOptionRejectsNonPositiveStrike()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.05));
            var discountFactor = new RateCurveSource(rateNode);
            var forward = new ForwardCurve(spotNode, discountFactor);
            var volatility = new VolatilitySource(equity);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOption("AAPL_CALL", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 0.0));
        }

        [Fact]
        public void EquityOptionRejectsCurrencyMismatch()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0, "USD");
            var spotNode = new SpotNode(equity);
            var usdRateNode = new RateNode(new CurrencyRateSource("USD", 0.05));
            var usdDiscountFactor = new RateCurveSource(usdRateNode);
            var forward = new ForwardCurve(spotNode, usdDiscountFactor);
            var volatility = new VolatilitySource(equity);
            var eurDiscountFactor = new RateCurveSource(new RateNode(new CurrencyRateSource("EUR", 0.05)));

            var exception = Assert.Throws<ArgumentException>(() => new EquityOption("AAPL_CALL", forward, volatility, eurDiscountFactor, DateTime.Today.AddYears(1), 100.0));

            Assert.Contains("currencies must match", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EquityOptionRejectsUndefinedOptionType()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.05));
            var discountFactor = new RateCurveSource(rateNode);
            var forward = new ForwardCurve(spotNode, discountFactor);
            var volatility = new VolatilitySource(equity);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOption("AAPL_CALL", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 100.0, (EquityOptionType)99));
        }

        [Fact]
        public void EquityOptionHandlesSpotMuchGreaterThanStrike()
        {
            var equity = new SimulatedAssetSource("AAPL", 1_000_000_000.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.05));
            var discountFactor = new RateCurveSource(rateNode);
            var forward = new ForwardCurve(spotNode, discountFactor);
            var volatility = new VolatilitySource(equity);

            var option = new EquityOption("AAPL_CALL", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 1.0);

            Assert.True(double.IsFinite(option.Price));
            Assert.True(option.Price > 900_000_000.0);
        }

        [Fact]
        public async Task EquityOptionPublishesARecalculatedPriceWhenEquityTicks()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.05));
            var discountFactor = new RateCurveSource(rateNode);
            var forward = new ForwardCurve(spotNode, discountFactor);
            var volatility = new VolatilitySource(equity);
            var option = new EquityOption("AAPL_CALL", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 100.0);
            QuoteTick? update = null;
            option.Tick += (_, message) => update = message;

            await equity.Start(1);

            Assert.NotNull(update);
            Assert.Equal(option.Price, update.Value);
        }

        [Fact]
        public void EquityOptionLoadsItsDependenciesAndOneYearMaturityFromJson()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.05));
            var discountFactor = new RateCurveSource(rateNode);
            var forward = new ForwardCurve(spotNode, discountFactor);
            var volatility = new VolatilitySource(equity);
            using var document = JsonDocument.Parse("{\"name\":\"AAPL_CALL\",\"constituent\":\"ForwardCurve::AAPL\",\"volatility\":\"VolatilitySource::AAPL\",\"discountCurve\":\"RateCurveSource::USD\",\"maturity\":\"1Y\",\"strike\":100,\"optionType\":\"Put\"}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var option = new EquityOption(definition, new Dictionary<string, IGraphNode>
            {
                ["EquitySource::AAPL"] = equity,
                ["ForwardCurve::AAPL"] = forward,
                ["VolatilitySource::AAPL"] = volatility,
                ["RateCurveSource::USD"] = discountFactor,
                ["RateNode::USD"] = rateNode
            });

            Assert.Equal(DateTime.Today.AddYears(1), option.Maturity);
            Assert.Equal(100.0, option.Strike);
            Assert.Equal(EquityOptionType.Put, option.OptionType);
        }
    }
}