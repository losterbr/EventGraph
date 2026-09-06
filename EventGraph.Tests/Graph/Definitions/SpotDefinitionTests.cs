using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph.Tests
{
    public class SpotDefinitionTests
    {
        [Fact]
        public void SpotDefinitionInitializesProperties()
        {
            var definition = new SpotDefinition("AAPL", "USD");

            Assert.Equal("AAPL", definition.Name);
            Assert.Equal("USD", definition.Currency);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void SpotDefinitionRejectsInvalidName(string name)
        {
            _ = Assert.Throws<ArgumentException>(() => new SpotDefinition(name, "USD"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void SpotDefinitionRejectsInvalidCurrency(string currency)
        {
            _ = Assert.Throws<ArgumentException>(() => new SpotDefinition("AAPL", currency));
        }

        [Fact]
        public void SpotDefinitionProviderProvidesSpotDefinition()
        {
            var provider = new SpotDefinitionProvider("AAPL", "USD");

            _ = Assert.IsAssignableFrom<IDefinitionProvider<SpotDefinition>>(provider);
            Assert.Equal("AAPL", provider.Definition.Name);
            Assert.Equal("USD", provider.Definition.Currency);
        }

        [Fact]
        public void SpotDefinitionProviderReadsJsonDefinitions()
        {
            using var document = JsonDocument.Parse("{\"name\":\"AAPL\",\"currency\":\"USD\"}");
            var dict = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var provider = new SpotDefinitionProvider(dict);

            Assert.Equal("AAPL", provider.Definition.Name);
            Assert.Equal("USD", provider.Definition.Currency);
        }

        [Fact]
        public void SpotDefinitionProviderDefaultsCurrencyToUsd()
        {
            using var document = JsonDocument.Parse("{\"name\":\"AAPL\"}");
            var dict = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var provider = new SpotDefinitionProvider(dict);

            Assert.Equal("AAPL", provider.Definition.Name);
            Assert.Equal("USD", provider.Definition.Currency);
        }
    }
}
