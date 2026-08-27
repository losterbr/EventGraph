using System.Text.Json;

namespace EventGraph.Tests
{
    public class VolatilitySourceTests
    {
        [Fact]
        public void VolatilityDelegatesToTheUnderlyingEquitySource()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);

            var volatility = new VolatilitySource(equity);

            Assert.Equal(0.2, volatility.Volatility);
            Assert.Equal(nameof(VolatilitySource), volatility.Type);
            Assert.Equal([equity], volatility.Dependencies);
        }

        [Fact]
        public void VolatilitySourceRejectsNullSource()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new VolatilitySource(null));
        }
    }
}
