using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph.Tests
{
    public class EquityDefinitionTests
    {
        [Fact]
        public void EquityDefinitionInitializesProperties()
        {
            var definition = new EquityDefinition("AAPL", 225.0, 0.28, 4.5, "USD");

            Assert.Equal("AAPL", definition.Name);
            Assert.Equal(225.0, definition.Spot);
            Assert.Equal(0.28, definition.Volatility);
            Assert.Equal(4.5, definition.MeanTickTimeSeconds);
            Assert.Equal("USD", definition.Currency);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void EquityDefinitionRejectsInvalidName(string name)
        {
            _ = Assert.Throws<ArgumentException>(() => new EquityDefinition(name, 100.0, 0.2));
        }

        [Fact]
        public void EquityDefinitionRejectsInvalidSpot()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityDefinition("AAPL", double.NaN, 0.2));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityDefinition("AAPL", double.PositiveInfinity, 0.2));
        }

        [Fact]
        public void EquityDefinitionRejectsInvalidVolatility()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityDefinition("AAPL", 100.0, -0.1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityDefinition("AAPL", 100.0, double.NaN));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityDefinition("AAPL", 100.0, double.PositiveInfinity));
        }

        [Fact]
        public void EquityDefinitionRejectsInvalidMeanTickTime()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityDefinition("AAPL", 100.0, 0.2, -1.0));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityDefinition("AAPL", 100.0, 0.2, double.NaN));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityDefinition("AAPL", 100.0, 0.2, double.PositiveInfinity));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void EquityDefinitionRejectsInvalidCurrency(string currency)
        {
            _ = Assert.Throws<ArgumentException>(() => new EquityDefinition("AAPL", 100.0, 0.2, 1.0, currency));
        }

        [Fact]
        public void EquityDefinitionProviderProvidesEquityDefinition()
        {
            var provider = new EquityDefinitionProvider("AAPL", 225.0, 0.28, 4.5, "USD");

            _ = Assert.IsAssignableFrom<IDefinitionProvider<EquityDefinition>>(provider);
            Assert.Equal("AAPL", provider.Definition.Name);
            Assert.Equal("USD", provider.Definition.Currency);
            Assert.Equal(225.0, provider.Definition.Spot);
            Assert.Equal(0.28, provider.Definition.Volatility);
            Assert.Equal(4.5, provider.Definition.MeanTickTimeSeconds);
        }

        [Fact]
        public void EquityDefinitionProviderReadsJsonDefinitions()
        {
            using var document = JsonDocument.Parse("{\"name\":\"AAPL\",\"spot\":225.0,\"volatility\":0.28,\"meanTickTimeSeconds\":4.5,\"currency\":\"USD\"}");
            var dict = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var provider = new EquityDefinitionProvider(dict);

            Assert.Equal("AAPL", provider.Definition.Name);
            Assert.Equal(225.0, provider.Definition.Spot);
            Assert.Equal(0.28, provider.Definition.Volatility);
            Assert.Equal(4.5, provider.Definition.MeanTickTimeSeconds);
            Assert.Equal("USD", provider.Definition.Currency);
        }

        [Fact]
        public void EquityDefinitionProviderUsesDefaultsForOptionalFields()
        {
            using var document = JsonDocument.Parse("{\"name\":\"AAPL\",\"spot\":225.0,\"volatility\":0.28}");
            var dict = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var provider = new EquityDefinitionProvider(dict);

            Assert.Equal("AAPL", provider.Definition.Name);
            Assert.Equal(1.0, provider.Definition.MeanTickTimeSeconds);
            Assert.Equal("USD", provider.Definition.Currency);
        }
    }
}
