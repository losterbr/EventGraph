using System.Text.Json;

namespace EventGraph.Tests
{
    public class BasketAggregateTests
    {
        [Fact]
        public async Task BasketAggregateRaisesUpdateAfterConstituentsPublish()
        {
            var quotes = new[]
            {
                new SimulatedAssetSource("A", 100.0, 0.0, 0.0),
                new SimulatedAssetSource("B", 200.0, 0.0, 0.0)
            };

            var basket = new BasketAggregate(quotes);
            var updates = new List<QuoteTick>();
            basket.SpotTick += (_, message) => updates.Add(message);

            await basket.RunOnceAsync();

            _ = Assert.Single(updates);
            Assert.Equal(150.0, updates[0].Value, 10);
        }

        [Fact]
        public async Task BasketAggregateUsesProvidedWeights()
        {
            var quotes = new[]
            {
                new SimulatedAssetSource("A", 100.0, 0.0, 0.0),
                new SimulatedAssetSource("B", 200.0, 0.0, 0.0)
            };

            var basket = new BasketAggregate(quotes, [0.25, 0.75]);
            var updates = new List<QuoteTick>();
            basket.SpotTick += (_, message) => updates.Add(message);

            await basket.RunOnceAsync();

            _ = Assert.Single(updates);
            Assert.Equal(175.0, updates[0].Value, 10);
        }

        [Fact]
        public async Task BasketAggregateAllowsNegativeWeightsWhenTheySumToOne()
        {
            var quotes = new[]
            {
                new SimulatedAssetSource("A", 120.0, 0.0, 0.0),
                new SimulatedAssetSource("B", 50.0, 0.0, 0.0)
            };

            var basket = new BasketAggregate(quotes, [2.0, -1.0]);
            var updates = new List<QuoteTick>();
            basket.SpotTick += (_, message) => updates.Add(message);

            await basket.RunOnceAsync();

            _ = Assert.Single(updates);
            Assert.Equal(190.0, updates[0].Value, 10);
            Assert.Equal(190.0, basket.Spot, 10);
        }

        [Fact]
        public async Task BasketAggregateCreatedFromDefinitionAllowsNegativeWeightsWhenTheySumToOne()
        {
            var sourceA = new SimulatedAssetSource("A", 120.0, 0.0, 0.0);
            var sourceB = new SimulatedAssetSource("B", 50.0, 0.0, 0.0);
            using var definition = JsonDocument.Parse("""
        {
          "name": "PAIR_TRADE",
          "constituents": ["A", "B"],
          "weights": [2, -1]
        }
        """);

            var basket = new BasketAggregate(
                ToDictionary(definition),
                new Dictionary<string, ISpotQuoteNode>
                {
                    [sourceA.Name] = sourceA,
                    [sourceB.Name] = sourceB
                });
            var updates = new List<QuoteTick>();
            basket.SpotTick += (_, message) => updates.Add(message);

            await basket.RunOnceAsync();

            _ = Assert.Single(updates);
            Assert.Equal(190.0, updates[0].Value, 10);
        }

        [Fact]
        public async Task BasketAggregateSkipsZeroWeightConstituents()
        {
            var quotes = new[]
            {
                new SimulatedAssetSource("A", 100.0, 0.0, 0.0),
                new SimulatedAssetSource("B", 200.0, 0.0, 0.0)
            };

            var basket = new BasketAggregate(quotes, [0.0, 1.0]);
            var updates = new List<QuoteTick>();
            basket.SpotTick += (_, message) => updates.Add(message);

            await quotes[0].Start(1);

            Assert.Empty(updates);
        }

        [Fact]
        public async Task BasketAggregateDoesNotSubscribeUntilConnected()
        {
            var source = new SimulatedAssetSource("A", 100.0, 0.0, 0.0);
            var basket = new BasketAggregate([source]);
            var updates = new List<QuoteTick>();
            basket.SpotTick += (_, message) => updates.Add(message);

            await source.Start(1);

            Assert.Empty(updates);
        }

        [Fact]
        public async Task BasketAggregatePublishesSourceTicksAfterConnect()
        {
            var source = new SimulatedAssetSource("A", 100.0, 0.0, 0.0);
            var basket = new BasketAggregate([source]);
            var updates = new List<QuoteTick>();
            basket.SpotTick += (_, message) => updates.Add(message);

            basket.Connect();
            await source.Start(1);

            var update = Assert.Single(updates);
            Assert.Equal(100.0, update.Value, 10);
        }

        [Fact]
        public async Task BasketAggregateIgnoresZeroWeightBasketConstituentsAfterConnect()
        {
            var zeroWeightSource = new SimulatedAssetSource("ZERO", 100.0, 0.0, 0.0);
            var activeSource = new SimulatedAssetSource("ACTIVE", 200.0, 0.0, 0.0);
            var zeroWeightBasket = new BasketAggregate("ZERO_BASKET", [zeroWeightSource]);
            var basket = new BasketAggregate("BASKET", [zeroWeightBasket, activeSource], [0.0, 1.0]);
            var updates = new List<QuoteTick>();
            basket.SpotTick += (_, message) => updates.Add(message);

            basket.Connect();
            await zeroWeightBasket.RunOnceAsync();

            Assert.Empty(updates);
        }

        [Fact]
        public void BasketAggregateThrowsWhenWeightsDoNotSumToOne()
        {
            var quotes = new[]
            {
                new SimulatedAssetSource("A", 100.0, 0.0, 0.0),
                new SimulatedAssetSource("B", 200.0, 0.0, 0.0)
            };

            _ = Assert.Throws<ArgumentException>(() => new BasketAggregate(quotes, [0.6, 0.3]));
        }

        [Fact]
        public void BasketAggregateThrowsWhenWeightCountDoesNotMatchConstituents()
        {
            var quotes = new[]
            {
                new SimulatedAssetSource("A", 100.0, 0.0, 0.0),
                new SimulatedAssetSource("B", 200.0, 0.0, 0.0)
            };

            _ = Assert.Throws<ArgumentException>(() => new BasketAggregate(quotes, [0.5]));
        }

        [Fact]
        public void BasketAggregateRejectsMixedCurrencies()
        {
            var quotes = new[]
            {
                new SimulatedAssetSource("USD_ASSET", 100.0, 0.0, 0.0, "USD"),
                new SimulatedAssetSource("EUR_ASSET", 200.0, 0.0, 0.0, "EUR")
            };

            var exception = Assert.Throws<ArgumentException>(() => new BasketAggregate(quotes));

            Assert.Contains("same currency", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BasketAggregateThrowsWhenConstituentsAreNull()
        {
            _ = Assert.Throws<ArgumentException>(() => new BasketAggregate(null));
        }

        [Fact]
        public void BasketAggregateThrowsWhenNoConstituentsAreProvided()
        {
            _ = Assert.Throws<ArgumentException>(() => new BasketAggregate([]));
        }

        [Theory]
        [InlineData("{}", "name")]
        [InlineData(/*lang=json,strict*/ "{\"name\":\"BASKET\"}", "constituents")]
        [InlineData(/*lang=json,strict*/ "{\"name\":\"BASKET\",\"constituents\":[\"Missing\"],\"weights\":[1]}", "unknown source")]
        public void BasketAggregateRejectsInvalidDefinitions(string json, string expectedMessage)
        {
            using var definition = JsonDocument.Parse(json);

            var exception = Assert.ThrowsAny<Exception>(() => new BasketAggregate(
                ToDictionary(definition),
                new Dictionary<string, ISpotQuoteNode>()));

            Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BasketAggregateRejectsNonNumericWeights()
        {
            var source = new SimulatedAssetSource("A", 100.0, 0.0, 0.0);
            using var definition = JsonDocument.Parse("{\"name\":\"BASKET\",\"constituents\":[\"A\"],\"weights\":[\"bad\"]}");

            var exception = Assert.Throws<InvalidDataException>(() => new BasketAggregate(
                ToDictionary(definition),
                new Dictionary<string, ISpotQuoteNode> { [source.Name] = source }));

            Assert.Contains("weights", exception.Message);
        }

        [Fact]
        public void BasketAggregateHasTheExpectedType()
        {
            var quotes = new[]
            {
                new SimulatedAssetSource("A", 100.0, 0.0, 0.0)
            };

            var basket = new BasketAggregate(quotes);

            Assert.Equal("CalculatedBasket", basket.Type);
        }

        [Fact]
        public async Task BasketAggregatePublishesAggregateOnlyWhenAllConstituentsAreAvailable()
        {
            var quotes = new[]
            {
                new SimulatedAssetSource("A", 100.0, 0.0, 0.0),
                new SimulatedAssetSource("B", 200.0, 0.0, 0.0)
            };

            var basket = new BasketAggregate(quotes);
            var updates = new List<QuoteTick>();
            basket.SpotTick += (_, message) => updates.Add(message);

            await quotes[0].Start(1);

            Assert.Empty(updates);
        }

        [Fact]
        public async Task BasketAggregatePublishesAggregateWhenAllConstituentEventsArrive()
        {
            var quotes = new[]
            {
                new SimulatedAssetSource("A", 100.0, 0.0, 0.0),
                new SimulatedAssetSource("B", 200.0, 0.0, 0.0)
            };

            var basket = new BasketAggregate(quotes);
            var updates = new List<QuoteTick>();
            basket.SpotTick += (_, message) => updates.Add(message);

            basket.Connect();
            await Task.WhenAll(quotes[0].Start(1), quotes[1].Start(1));

            _ = Assert.Single(updates);
            Assert.Equal(150.0, updates[0].Value, 10);
        }

        [Fact]
        public void BasketAggregateGetsDisplayWeightsSortedByName()
        {
            var quotes = new[]
            {
                new SimulatedAssetSource("B", 200.0, 0.0, 0.0),
                new SimulatedAssetSource("A", 100.0, 0.0, 0.0)
            };

            var basket = new BasketAggregate(quotes);

            Assert.Contains("A=0.5", basket.GetWeights());
            Assert.Contains("B=0.5", basket.GetWeights());
        }

        private static Dictionary<string, JsonElement> ToDictionary(JsonDocument document)
        {
            return document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
