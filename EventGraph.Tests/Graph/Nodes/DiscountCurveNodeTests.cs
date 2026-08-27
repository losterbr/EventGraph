namespace EventGraph.Tests
{
    public class DiscountCurveNodeTests
    {
        [Fact]
        public void DiscountCurveNodeDerivesDiscountFactorFromRateNode()
        {
            var rateNode = new RateNode(new CurrencyRateSource("USD", 0.02));

            var discountCurve = new DiscountCurveNode(rateNode);

            Assert.Equal("USD", discountCurve.Name);
            Assert.Equal("USD", discountCurve.Currency);
            Assert.Equal(nameof(DiscountCurveNode), discountCurve.Type);
            Assert.Equal([rateNode], discountCurve.Dependencies);
            Assert.Equal(Math.Exp(-0.02), discountCurve.DiscountFactor(DateTime.Today.AddDays(365)), precision: 12);
        }

        [Fact]
        public void DiscountCurveNodeRejectsNullRateNode()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new DiscountCurveNode(null));
        }
    }
}
