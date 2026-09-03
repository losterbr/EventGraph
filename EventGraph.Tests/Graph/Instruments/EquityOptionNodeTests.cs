using System.Text.Json;

namespace EventGraph.Tests
{
    public class EquityOptionTests
    {
        [Fact]
        public void EquityOptionCalculatesBlackScholesCallPrice()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = CreateDiscountCurve();
            var forward = new ForwardCurveNode(spotNode, discountFactor);
            var volatility = new VolatilityNode(equity);
            var maturity = DateTime.Today.AddYears(1);

            var option = new EquityOptionNode("AAPL_CALL", forward, volatility, discountFactor, maturity, 100.0);

            Assert.Equal(10.4506, option.Price, precision: 3);
            Assert.Equal(EquityOptionType.Call, option.OptionType);
            Assert.Equal([forward, volatility, discountFactor], option.Dependencies);
            Assert.Equal("USD", option.Currency);
        }

        [Fact]
        public void EquityOptionCalculatesBlackScholesPutPrice()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = CreateDiscountCurve();
            var forward = new ForwardCurveNode(spotNode, discountFactor);
            var volatility = new VolatilityNode(equity);

            var option = new EquityOptionNode("AAPL_PUT", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 100.0, EquityOptionType.Put);

            Assert.Equal(5.5736, option.Price, precision: 3);
            Assert.Equal(EquityOptionType.Put, option.OptionType);
        }

        [Fact]
        public void EquityOptionRejectsBlankName()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = CreateDiscountCurve();
            var forward = new ForwardCurveNode(spotNode, discountFactor);
            var volatility = new VolatilityNode(equity);

            _ = Assert.Throws<ArgumentException>(() => new EquityOptionNode(" ", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 100.0));
        }

        [Fact]
        public void EquityOptionRejectsPastMaturity()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = CreateDiscountCurve();
            var forward = new ForwardCurveNode(spotNode, discountFactor);
            var volatility = new VolatilityNode(equity);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOptionNode("AAPL_CALL", forward, volatility, discountFactor, DateTime.Today, 100.0));
        }

        [Fact]
        public void EquityOptionRejectsNonPositiveStrike()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = CreateDiscountCurve();
            var forward = new ForwardCurveNode(spotNode, discountFactor);
            var volatility = new VolatilityNode(equity);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOptionNode("AAPL_CALL", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 0.0));
        }

        [Fact]
        public void EquityOptionRejectsCurrencyMismatch()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0, "USD");
            var spotNode = new SpotNode(equity);
            var usdDiscountFactor = CreateDiscountCurve();
            var forward = new ForwardCurveNode(spotNode, usdDiscountFactor);
            var volatility = new VolatilityNode(equity);
            var eurDiscountFactor = CreateDiscountCurve("EUR");

            var exception = Assert.Throws<ArgumentException>(() => new EquityOptionNode("AAPL_CALL", forward, volatility, eurDiscountFactor, DateTime.Today.AddYears(1), 100.0));

            Assert.Contains("currencies must match", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EquityOptionRejectsUndefinedOptionType()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = CreateDiscountCurve();
            var forward = new ForwardCurveNode(spotNode, discountFactor);
            var volatility = new VolatilityNode(equity);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOptionNode("AAPL_CALL", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 100.0, (EquityOptionType)99));
        }

        [Fact]
        public void EquityOptionHandlesSpotMuchGreaterThanStrike()
        {
            var equity = new EquitySource("AAPL", 1_000_000_000.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = CreateDiscountCurve();
            var forward = new ForwardCurveNode(spotNode, discountFactor);
            var volatility = new VolatilityNode(equity);

            var option = new EquityOptionNode("AAPL_CALL", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 1.0);

            Assert.True(double.IsFinite(option.Price));
            Assert.True(option.Price > 900_000_000.0);
        }

        [Fact]
        public async Task EquityOptionPublishesARecalculatedPriceWhenEquityTicks()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = CreateDiscountCurve();
            var forward = new ForwardCurveNode(spotNode, discountFactor);
            var volatility = new VolatilityNode(equity);
            var option = new EquityOptionNode("AAPL_CALL", forward, volatility, discountFactor, DateTime.Today.AddYears(1), 100.0);
            QuoteTick? update = null;
            option.Tick += (_, message) => update = message;

            await equity.Start(1);

            Assert.NotNull(update);
            Assert.Equal(option.Price, update.Value);
        }

        [Fact]
        public void EquityOptionLoadsItsDependenciesAndOneYearMaturityFromJson()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            var discountFactor = CreateDiscountCurve();
            var forward = new ForwardCurveNode(spotNode, discountFactor);
            var volatility = new VolatilityNode(equity);
            using var document = JsonDocument.Parse("{\"name\":\"AAPL_CALL\",\"forward\":\"ForwardCurveNode::AAPL\",\"volatility\":\"VolatilityNode::AAPL\",\"discountCurve\":\"RateCurveNode::USD\",\"maturity\":\"1Y\",\"strike\":100,\"optionType\":\"Put\"}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var option = new EquityOptionNode(definition, new Dictionary<string, IGraphNode>
            {
                ["ForwardCurveNode::AAPL"] = forward,
                ["VolatilityNode::AAPL"] = volatility,
                ["RateCurveNode::USD"] = discountFactor
            });

            Assert.Equal(DateTime.Today.AddYears(1), option.Maturity);
            Assert.Equal(100.0, option.Strike);
            Assert.Equal(EquityOptionType.Put, option.OptionType);
        }

        private static RateCurveNode CreateDiscountCurve(string currency = "USD")
        {
            return new RateCurveNode(new CurrencyRateSource(currency, 0.05));
        }
    }
}