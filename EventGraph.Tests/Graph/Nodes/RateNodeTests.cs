namespace EventGraph.Tests
{
    public class RateNodeTests
    {
        [Fact]
        public void RateNodeDelegatesToTheUnderlyingCurrencyRateSource()
        {
            var source = new CurrencyRateSource("USD_3M_Libor", 0.05, "USD");

            var rateNode = new RateNode(source);

            Assert.Equal("USD_3M_Libor", rateNode.Name);
            Assert.Equal(0.05, rateNode.InterestRate);
            Assert.Equal("USD", rateNode.Currency);
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
