namespace EventGraph.Tests
{
    public class RateNodeTests
    {
        [Fact]
        public void RateNodeDelegatesToTheUnderlyingCurrencyRateSource()
        {
            var source = new CurrencyRateSource("USD", 0.05);

            var rateNode = new RateNode(source);

            Assert.Equal("USD", rateNode.Name);
            Assert.Equal(0.05, rateNode.InterestRate);
            Assert.Equal(nameof(RateNode), rateNode.Type);
            Assert.Equal([source], rateNode.Dependencies);
        }

        [Fact]
        public void RateNodeRejectsNullSource()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new RateNode(null));
        }
    }
}
