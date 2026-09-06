using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EventGraph.Tests
{
    public class BasketDefinitionTests
    {
        [Fact]
        public void BasketDefinitionInitializesProperties()
        {
            var definition = new BasketDefinition("BASKET", "USD", ["A", "B"], [0.25, 0.75]);

            Assert.Equal("BASKET", definition.Name);
            Assert.Equal("USD", definition.Currency);
            Assert.Equal(["A", "B"], definition.Constituents);
            Assert.Equal([0.25, 0.75], definition.Weights);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void BasketDefinitionRejectsInvalidName(string name)
        {
            _ = Assert.Throws<ArgumentException>(() => new BasketDefinition(name, "USD", ["A"], [1.0]));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void BasketDefinitionRejectsInvalidCurrency(string currency)
        {
            _ = Assert.Throws<ArgumentException>(() => new BasketDefinition("BASKET", currency, ["A"], [1.0]));
        }

        [Fact]
        public void BasketDefinitionRejectsNullOrEmptyConstituents()
        {
            _ = Assert.Throws<ArgumentException>(() => new BasketDefinition("BASKET", "USD", null!, [1.0]));
            _ = Assert.Throws<ArgumentException>(() => new BasketDefinition("BASKET", "USD", [], []));
        }

        [Fact]
        public void BasketDefinitionRejectsBlankConstituents()
        {
            _ = Assert.Throws<ArgumentException>(() => new BasketDefinition("BASKET", "USD", ["A", " "], [0.5, 0.5]));
        }

        [Fact]
        public void BasketDefinitionRejectsMismatchedWeights()
        {
            _ = Assert.Throws<ArgumentException>(() => new BasketDefinition("BASKET", "USD", ["A", "B"], null!));
            _ = Assert.Throws<ArgumentException>(() => new BasketDefinition("BASKET", "USD", ["A", "B"], [1.0]));
        }

        [Fact]
        public void BasketDefinitionRejectsInvalidWeights()
        {
            _ = Assert.Throws<ArgumentException>(() => new BasketDefinition("BASKET", "USD", ["A"], [double.NaN]));
            _ = Assert.Throws<ArgumentException>(() => new BasketDefinition("BASKET", "USD", ["A"], [double.PositiveInfinity]));
            _ = Assert.Throws<ArgumentException>(() => new BasketDefinition("BASKET", "USD", ["A", "B"], [0.5, 0.6]));
        }

        [Fact]
        public void BasketDefinitionProviderProvidesBasketDefinition()
        {
            var provider = new BasketDefinitionProvider("BASKET", "USD", ["A", "B"], [0.25, 0.75]);

            _ = Assert.IsAssignableFrom<IDefinitionProvider<BasketDefinition>>(provider);
            Assert.Equal("BASKET", provider.Definition.Name);
            Assert.Equal("USD", provider.Definition.Currency);
            Assert.Equal(["A", "B"], provider.Definition.Constituents);
            Assert.Equal([0.25, 0.75], provider.Definition.Weights);
        }

        [Fact]
        public void BasketDefinitionProviderReadsJsonDefinitions()
        {
            using var document = JsonDocument.Parse("{\"name\":\"BASKET\",\"currency\":\"USD\",\"constituents\":[\"A\",\"B\"],\"weights\":[0.25,0.75]}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var provider = new BasketDefinitionProvider(definition);

            Assert.Equal("BASKET", provider.Definition.Name);
            Assert.Equal("USD", provider.Definition.Currency);
            Assert.Equal(["A", "B"], provider.Definition.Constituents);
            Assert.Equal([0.25, 0.75], provider.Definition.Weights);
        }

        [Fact]
        public void BasketDefinitionProviderRequiresCurrency()
        {
            using var document = JsonDocument.Parse("{\"name\":\"BASKET\",\"constituents\":[\"A\"],\"weights\":[1]}");
            var definition = document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            var exception = Assert.Throws<InvalidDataException>(() => new BasketDefinitionProvider(definition));

            Assert.Contains("currency", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BasketDefinitionProviderRejectsInvalidConstituentsJson()
        {
            using var doc1 = JsonDocument.Parse("{\"name\":\"BASKET\",\"currency\":\"USD\",\"weights\":[1]}");
            _ = Assert.Throws<InvalidDataException>(() => new BasketDefinitionProvider(
                doc1.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase)));

            using var doc2 = JsonDocument.Parse("{\"name\":\"BASKET\",\"currency\":\"USD\",\"constituents\":[\"\"],\"weights\":[1]}");
            _ = Assert.Throws<InvalidDataException>(() => new BasketDefinitionProvider(
                doc2.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase)));
        }

        [Fact]
        public void BasketDefinitionProviderRejectsInvalidWeightsJson()
        {
            using var doc1 = JsonDocument.Parse("{\"name\":\"BASKET\",\"currency\":\"USD\",\"constituents\":[\"A\"]}");
            _ = Assert.Throws<InvalidDataException>(() => new BasketDefinitionProvider(
                doc1.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase)));

            using var doc2 = JsonDocument.Parse("{\"name\":\"BASKET\",\"currency\":\"USD\",\"constituents\":[\"A\"],\"weights\":[\"bad\"]}");
            _ = Assert.Throws<InvalidDataException>(() => new BasketDefinitionProvider(
                doc2.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase)));
        }
    }
}