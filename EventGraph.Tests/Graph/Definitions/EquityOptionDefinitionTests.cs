using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph.Tests
{
    public class EquityOptionDefinitionTests
    {
        [Fact]
        public void EquityOptionDefinitionInitializesProperties()
        {
            var definition = new EquityOptionDefinition("AAPL_1Y_CALL", "AAPL", "1Y", 225.0, "Call");

            Assert.Equal("AAPL_1Y_CALL", definition.Name);
            Assert.Equal("AAPL", definition.Underlyer);
            Assert.Equal("1Y", definition.Maturity);
            Assert.Equal(225.0, definition.Strike);
            Assert.Equal("Call", definition.OptionType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void EquityOptionDefinitionRejectsInvalidName(string name)
        {
            _ = Assert.Throws<ArgumentException>(() => new EquityOptionDefinition(name, "AAPL", "1Y", 100.0, "Call"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void EquityOptionDefinitionRejectsInvalidUnderlyer(string underlyer)
        {
            _ = Assert.Throws<ArgumentException>(() => new EquityOptionDefinition("AAPL_CALL", underlyer, "1Y", 100.0, "Call"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void EquityOptionDefinitionRejectsInvalidMaturity(string maturity)
        {
            _ = Assert.Throws<ArgumentException>(() => new EquityOptionDefinition("AAPL_CALL", "AAPL", maturity, 100.0, "Call"));
        }

        [Fact]
        public void EquityOptionDefinitionRejectsInvalidStrike()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOptionDefinition("AAPL_CALL", "AAPL", "1Y", 0.0, "Call"));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOptionDefinition("AAPL_CALL", "AAPL", "1Y", -10.0, "Call"));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOptionDefinition("AAPL_CALL", "AAPL", "1Y", double.NaN, "Call"));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquityOptionDefinition("AAPL_CALL", "AAPL", "1Y", double.PositiveInfinity, "Call"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void EquityOptionDefinitionRejectsInvalidOptionType(string optionType)
        {
            _ = Assert.Throws<ArgumentException>(() => new EquityOptionDefinition("AAPL_CALL", "AAPL", "1Y", 100.0, optionType));
        }

        [Fact]
        public void EquityOptionDefinitionProviderProvidesEquityOptionDefinition()
        {
            var provider = new EquityOptionDefinitionProvider("AAPL_1Y_CALL", "AAPL", "1Y", 225.0, "Call");

            _ = Assert.IsAssignableFrom<IDefinitionProvider<EquityOptionDefinition>>(provider);
            Assert.Equal("AAPL_1Y_CALL", provider.Definition.Name);
            Assert.Equal("AAPL", provider.Definition.Underlyer);
            Assert.Equal("1Y", provider.Definition.Maturity);
            Assert.Equal(225.0, provider.Definition.Strike);
            Assert.Equal("Call", provider.Definition.OptionType);
        }

        [Fact]
        public void EquityOptionDefinitionProviderReadsJsonDefinitions()
        {
            using var document = JsonDocument.Parse("{\"name\":\"AAPL_1Y_CALL\",\"underlyer\":\"AAPL\",\"maturity\":\"1Y\",\"strike\":225.0,\"optionType\":\"Call\"}");
            var dict = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var provider = new EquityOptionDefinitionProvider(dict);

            Assert.Equal("AAPL_1Y_CALL", provider.Definition.Name);
            Assert.Equal("AAPL", provider.Definition.Underlyer);
            Assert.Equal("1Y", provider.Definition.Maturity);
            Assert.Equal(225.0, provider.Definition.Strike);
            Assert.Equal("Call", provider.Definition.OptionType);
        }
    }
}
