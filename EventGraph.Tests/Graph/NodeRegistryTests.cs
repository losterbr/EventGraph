using System.Text.Json;

namespace EventGraph.Tests
{
    public class NodeRegistryTests
    {
        [Fact]
        public void SupportedTypesIncludeRegisteredNodeTypes()
        {
            Assert.Contains("EquitySource", NodeRegistry.SupportedTypes);
            Assert.Contains(nameof(EquitySource), NodeRegistry.SupportedTypes);
            Assert.Contains(nameof(CurrencyRateSource), NodeRegistry.SupportedTypes);
            Assert.Contains(nameof(RateCurveNode), NodeRegistry.SupportedTypes);
            Assert.Contains(nameof(VolatilityNode), NodeRegistry.SupportedTypes);
            Assert.Contains(nameof(BasketSpotNode), NodeRegistry.SupportedTypes);
            Assert.Contains(nameof(ForwardCurveNode), NodeRegistry.SupportedTypes);
            Assert.Contains(nameof(EquityOption), NodeRegistry.SupportedTypes);
        }

        [Fact]
        public void IsSupportedTypeRejectsBlankNamesAndMatchesCaseInsensitively()
        {
            Assert.False(NodeRegistry.IsSupportedType(" "));
            Assert.True(NodeRegistry.IsSupportedType("equitysource"));
        }

        [Fact]
        public void SourceTypesAreJsonBackedAndHaveNoDependencies()
        {
            Assert.True(NodeRegistry.IsSourceType(nameof(EquitySource)));
            Assert.True(NodeRegistry.IsSourceType(nameof(CurrencyRateSource)));
            Assert.False(NodeRegistry.IsSourceType(nameof(SpotNode)));
            Assert.False(NodeRegistry.IsSourceType(nameof(VolatilityNode)));
            Assert.False(NodeRegistry.IsSourceType(nameof(RateCurveNode)));

            foreach (var type in NodeRegistry.SupportedTypes.Where(NodeRegistry.IsSourceType))
            {
                using var document = JsonDocument.Parse($"{{\"type\":\"{type}\"}}");
                Assert.Empty(NodeRegistry.GetDependencyNames(ToDictionary(document)));
            }
        }

        [Fact]
        public void CreateNodeCreatesSimulatedAssetSource()
        {
            using var definition = JsonDocument.Parse("""
        {
          "type": "EquitySource",
          "name": "A",
          "spot": 100,
          "volatility": 0.2,
          "meanTickTimeSeconds": 1
        }
        """);

            var node = NodeRegistry.CreateNode(ToDictionary(definition), new Dictionary<string, IGraphNode>());

            var source = Assert.IsType<EquitySource>(node);
            Assert.Equal("A", source.Name);
        }

        [Fact]
        public void CreateNodeCreatesBasketAggregate()
        {
            var source = new EquitySource("A", 100.0, 0.0, 0.0);
            var spot = new SpotNode(source);
            using var definition = JsonDocument.Parse("""
        {
          "type": "BasketSpotNode",
          "name": "BASKET",
          "constituents": ["A"],
          "weights": [1]
        }
        """);

            var node = NodeRegistry.CreateNode(
                ToDictionary(definition),
                new Dictionary<string, IGraphNode> { [GraphKey.Of(nameof(SpotNode), spot.Name)] = spot });

            var basket = Assert.IsType<BasketSpotNode>(node);
            Assert.Equal("BASKET", basket.Name);
        }

        [Fact]
        public void CreateNodeRejectsNullDefinitions()
        {
            _ = Assert.Throws<ArgumentNullException>(() =>
                NodeRegistry.CreateNode(null, new Dictionary<string, IGraphNode>()));
        }

        [Fact]
        public void CreateNodeRejectsMissingTypes()
        {
            using var definition = JsonDocument.Parse("{} ");

            _ = Assert.Throws<InvalidDataException>(() =>
                NodeRegistry.CreateNode(ToDictionary(definition), new Dictionary<string, IGraphNode>()));
        }

        [Fact]
        public void CreateNodeRejectsUnsupportedTypes()
        {
            using var definition = JsonDocument.Parse("{\"type\":\"UnknownNode\"}");

            var exception = Assert.Throws<InvalidDataException>(() =>
                NodeRegistry.CreateNode(ToDictionary(definition), new Dictionary<string, IGraphNode>()));

            Assert.Contains("Unsupported graph node type", exception.Message);
        }

        private static Dictionary<string, JsonElement> ToDictionary(JsonDocument document)
        {
            return document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }
    }
}