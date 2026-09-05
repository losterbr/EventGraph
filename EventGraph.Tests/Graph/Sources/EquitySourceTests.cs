using System.Text.Json;

namespace EventGraph.Tests
{
    public class EquitySourceTests
    {
        [Fact]
        public void EquitySourceLoadsItsConfigFromJson()
        {
            using var document = JsonDocument.Parse("{\"name\":\"AAPL\",\"currency\":\"USD\",\"spot\":225.0,\"volatility\":0.28,\"meanTickTimeSeconds\":4.5}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var source = new EquitySource(definition);

            Assert.Equal("AAPL", source.Name);
            Assert.Equal(225.0, source.Spot);
            Assert.Equal(0.28, source.Volatility);
            Assert.Equal(nameof(EquitySource), source.Type);
        }

        [Fact]
        public void EquitySourceRejectsInvalidConfig()
        {
            _ = Assert.Throws<ArgumentException>(() => new EquitySource(" ", 100.0, 0.1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquitySource("A", double.NaN, 0.1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquitySource("A", 100.0, -0.1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquitySource("A", 100.0, 0.1, -1.0));
            _ = Assert.Throws<ArgumentException>(() => new EquitySource("A", 100.0, 0.1, 1.0, " "));
        }
    }
}
