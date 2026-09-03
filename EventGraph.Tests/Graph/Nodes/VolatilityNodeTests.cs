namespace EventGraph.Tests
{
    public class VolatilityNodeTests
    {
        [Fact]
        public void VolatilityDelegatesToTheUnderlyingEquitySource()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);

            var volatility = new VolatilityNode(equity);

            Assert.Equal(0.2, volatility.Volatility);
            Assert.Equal(nameof(VolatilityNode), volatility.Type);
            Assert.Equal([equity], volatility.Dependencies);
        }

        [Fact]
        public void VolatilityNodeRejectsNullSource()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new VolatilityNode(null));
        }
    }
}