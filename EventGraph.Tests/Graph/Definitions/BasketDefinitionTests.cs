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
    }
}