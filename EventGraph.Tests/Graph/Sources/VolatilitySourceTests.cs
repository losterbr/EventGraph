using System.Text.Json;

namespace EventGraph.Tests
{
    public class VolatilitySourceTests
    {
        [Fact]
        public void VolatilityDelegatesToTheUnderlyingSource()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);

            var volatility = new VolatilitySource("AAPL_VOL", equity);

            Assert.Equal(0.2, volatility.Volatility);
            Assert.Equal(nameof(VolatilitySource), volatility.Type);
            Assert.Equal([equity], volatility.Dependencies);
        }

        [Fact]
        public void VolatilitySourceRejectsBlankName()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);

            _ = Assert.Throws<ArgumentException>(() => new VolatilitySource(" ", equity));
        }

        [Fact]
        public void VolatilitySourceRejectsNullSource()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new VolatilitySource("AAPL_VOL", null));
        }

        [Fact]
        public void VolatilitySourceLoadsItsDependencyFromJson()
        {
            var equity = new SimulatedAssetSource("AAPL", 100.0, 0.2, 0.0);
            using var document = JsonDocument.Parse("{\"name\":\"AAPL_VOL\",\"constituent\":\"AAPL\"}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var volatility = new VolatilitySource(definition, new Dictionary<string, IGraphNode>
            {
                [equity.Name] = equity
            });

            Assert.Equal("AAPL_VOL", volatility.Name);
            Assert.Equal([equity], volatility.Dependencies);
        }
    }
}
