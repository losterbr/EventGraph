using System.Text.Json;

namespace EventGraph.Tests
{
    public class BasketDefinitionTests
    {
        [Fact]
        public void SpotDefinitionProviderProvidesSpotDefinition()
        {
            var provider = new SpotDefinitionProvider("AAPL", "USD");

            _ = Assert.IsAssignableFrom<IDefinitionProvider<SpotDefinition>>(provider);
            Assert.Equal("AAPL", provider.Definition.Name);
            Assert.Equal("USD", provider.Definition.Currency);
        }

        [Fact]
        public void BasketDefinitionProviderProvidesBasketDefinition()
        {
            var provider = new BasketDefinitionProvider("BASKET", ["A", "B"], [0.25, 0.75]);

            _ = Assert.IsAssignableFrom<IDefinitionProvider<BasketDefinition>>(provider);
            Assert.Equal("BASKET", provider.Definition.Name);
            Assert.Equal(["A", "B"], provider.Definition.Constituents);
            Assert.Equal([0.25, 0.75], provider.Definition.Weights);
        }

        [Fact]
        public void BasketDefinitionProviderReadsJsonDefinitions()
        {
            using var document = JsonDocument.Parse("{\"name\":\"BASKET\",\"constituents\":[\"A\",\"B\"],\"weights\":[0.25,0.75]}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var provider = new BasketDefinitionProvider(definition);

            Assert.Equal("BASKET", provider.Definition.Name);
            Assert.Equal(["A", "B"], provider.Definition.Constituents);
            Assert.Equal([0.25, 0.75], provider.Definition.Weights);
        }
    }
}