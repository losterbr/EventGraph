using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph.Tests
{
    public class CurrencyRateDefinitionTests
    {
        [Fact]
        public void CurrencyRateDefinitionInitializesProperties()
        {
            var definition = new CurrencyRateDefinition("USD_3M_Libor", 0.02, "USD");

            Assert.Equal("USD_3M_Libor", definition.Name);
            Assert.Equal(0.02, definition.InterestRate);
            Assert.Equal("USD", definition.Currency);
        }

        [Fact]
        public void CurrencyRateDefinitionDefaultsCurrencyToNameWhenNull()
        {
            var definition = new CurrencyRateDefinition("USD", 0.02);

            Assert.Equal("USD", definition.Name);
            Assert.Equal("USD", definition.Currency);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void CurrencyRateDefinitionRejectsInvalidName(string name)
        {
            _ = Assert.Throws<ArgumentException>(() => new CurrencyRateDefinition(name, 0.02));
        }

        [Fact]
        public void CurrencyRateDefinitionRejectsInvalidInterestRate()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new CurrencyRateDefinition("USD", double.NaN));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new CurrencyRateDefinition("USD", double.PositiveInfinity));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void CurrencyRateDefinitionRejectsBlankCurrency(string currency)
        {
            _ = Assert.Throws<ArgumentException>(() => new CurrencyRateDefinition("USD", 0.02, currency));
        }

        [Fact]
        public void CurrencyRateDefinitionProviderProvidesCurrencyRateDefinition()
        {
            var provider = new CurrencyRateDefinitionProvider("USD_3M_Libor", 0.02, "USD");

            _ = Assert.IsAssignableFrom<IDefinitionProvider<CurrencyRateDefinition>>(provider);
            Assert.Equal("USD_3M_Libor", provider.Definition.Name);
            Assert.Equal("USD", provider.Definition.Currency);
            Assert.Equal(0.02, provider.Definition.InterestRate);
        }

        [Fact]
        public void CurrencyRateDefinitionProviderReadsJsonDefinitions()
        {
            using var document = JsonDocument.Parse("{\"name\":\"USD_3M_Libor\",\"interestRate\":0.02,\"currency\":\"USD\"}");
            var dict = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var provider = new CurrencyRateDefinitionProvider(dict);

            Assert.Equal("USD_3M_Libor", provider.Definition.Name);
            Assert.Equal(0.02, provider.Definition.InterestRate);
            Assert.Equal("USD", provider.Definition.Currency);
        }

        [Fact]
        public void CurrencyRateDefinitionProviderDefaultsCurrencyToName()
        {
            using var document = JsonDocument.Parse("{\"name\":\"USD\",\"interestRate\":0.02}");
            var dict = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var provider = new CurrencyRateDefinitionProvider(dict);

            Assert.Equal("USD", provider.Definition.Name);
            Assert.Equal("USD", provider.Definition.Currency);
        }
    }
}
