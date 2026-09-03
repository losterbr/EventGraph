namespace EventGraph.Tests
{
    public class RateCurveNodeTests
    {
        [Fact]
        public void DiscountFactorEvaluatesFromTodayUsingContinuousCompounding()
        {
            var source = new RateCurveNode(new CurrencyRateSource("USD", 0.02));

            Assert.Equal(Math.Exp(-0.02), source.DiscountFactor(DateTime.Today.AddDays(365)), precision: 12);
            Assert.Equal(1.0, source.DiscountFactor(DateTime.Today), precision: 12);
        }

        [Fact]
        public void RateCurveNodeUsesCurrencyRateSourceProperties()
        {
            var rateSource = new CurrencyRateSource("USD", 0.02);

            var source = new RateCurveNode(rateSource);

            Assert.Equal("USD", source.Name);
            Assert.Equal("USD", source.Currency);
            Assert.Equal(0.02, source.InterestRate);
            Assert.Equal(nameof(RateCurveNode), source.Type);
            Assert.Equal([rateSource], source.Dependencies);
        }
    }
}