using System.Text.Json;

namespace EventGraph.Tests
{
    public class NodeRegistryTests
    {
        [Fact]
        public void SupportedTypesIncludeRegisteredNodeTypes()
        {
            Assert.Contains("SimulatedAssetSource", NodeRegistry.SupportedTypes);
            Assert.Contains(nameof(BasketAggregate), NodeRegistry.SupportedTypes);
            Assert.Contains(nameof(ForwardCurve), NodeRegistry.SupportedTypes);
            Assert.Contains(nameof(VolatilitySource), NodeRegistry.SupportedTypes);
        }

        [Fact]
        public void IsSupportedTypeRejectsBlankNamesAndMatchesCaseInsensitively()
        {
            Assert.False(NodeRegistry.IsSupportedType(" "));
            Assert.True(NodeRegistry.IsSupportedType("simulatedassetsource"));
        }

        [Fact]
        public void CreateNodeCreatesSimulatedAssetSource()
        {
            using var definition = JsonDocument.Parse("""
        {
          "type": "SimulatedAssetSource",
          "name": "A",
          "spot": 100,
          "volatility": 0.2,
          "meanTickTimeSeconds": 1
        }
        """);

            var node = NodeRegistry.CreateNode(ToDictionary(definition), new Dictionary<string, IGraphNode>());

            var source = Assert.IsType<SimulatedAssetSource>(node);
            Assert.Equal("A", source.Name);
        }

        [Fact]
        public void CreateNodeCreatesBasketAggregate()
        {
            var source = new SimulatedAssetSource("A", 100.0, 0.0, 0.0);
            using var definition = JsonDocument.Parse("""
        {
          "type": "BasketAggregate",
          "name": "BASKET",
          "constituents": ["A"],
          "weights": [1]
        }
        """);

            var node = NodeRegistry.CreateNode(
                ToDictionary(definition),
                new Dictionary<string, IGraphNode> { [source.Name] = source });

            var basket = Assert.IsType<BasketAggregate>(node);
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