using System.Text.Json;

namespace EventGraph.Tests
{
    public class SimulatedAssetSourceTests
    {
        [Fact]
        public async Task SimulatedAssetSourceCanRunMultipleTicks()
        {
            var updates = new List<QuoteTick>();
            var source = new SimulatedAssetSource("XYZ", 100.0, 0.2, 0.0);
            source.SpotTick += (_, message) => updates.Add(message);

            await source.Start(2);

            Assert.Equal(2, updates.Count);
        }

        [Fact]
        public async Task SimulatedAssetSourceCanRunInContinuousModeUntilCancelled()
        {
            var updates = new List<QuoteTick>();
            var source = new SimulatedAssetSource("CONT", 50.0, 0.1, 0.0);
            using var cts = new CancellationTokenSource();
            source.SpotTick += (_, message) =>
            {
                updates.Add(message);
                cts.Cancel();
            };

            await source.Start(0, cts.Token);

            Assert.NotEmpty(updates);
        }

        [Fact]
        public void SimulatedAssetSourceRejectsInvalidName()
        {
            _ = Assert.Throws<ArgumentException>(() => new SimulatedAssetSource(" ", 100.0, 0.1));
        }

        [Fact]
        public void SimulatedAssetSourceRejectsDefinitionsWithoutNames()
        {
            using var definition = JsonDocument.Parse("""
        {
          "spot": 100,
          "volatility": 0.2,
          "meanTickTimeSeconds": 1
        }
        """);

            var exception = Assert.Throws<InvalidDataException>(() =>
                new SimulatedAssetSource(ToDictionary(definition)));

            Assert.Contains("name", exception.Message);
        }

        [Fact]
        public void SimulatedAssetSourceHasTheExpectedType()
        {
            var source = new SimulatedAssetSource("TYPE", 100.0, 0.1);

            Assert.Equal("SimulatedSpot", source.Type);
        }

        [Fact]
        public void SimulatedAssetSourceExposesVolatilityQuote()
        {
            var source = new SimulatedAssetSource("VOL", 100.0, 0.25);

            var volNode = Assert.IsAssignableFrom<IVolQuoteNode>(source);
            var spotNode = Assert.IsAssignableFrom<ISpotQuoteNode>(source);
            Assert.Equal(0.25, volNode.Volatility, 10);
            Assert.Equal(100.0, spotNode.Spot, 10);
        }

        [Fact]
        public void SimulatedAssetSourceRejectsInvalidStartingSpot()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedAssetSource("A", double.NaN, 0.1));
        }

        [Fact]
        public void SimulatedAssetSourceRejectsNegativeVolatility()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedAssetSource("A", 100.0, -0.1));
        }

        [Fact]
        public void SimulatedAssetSourceRejectsNegativeMeanTickTime()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedAssetSource("A", 100.0, 0.1, -1.0));
        }

        [Fact]
        public async Task SimulatedAssetSourceEmitsInitialValueBeforeMovement()
        {
            var source = new SimulatedAssetSource("INIT", 100.0, 0.0, 0.0);
            QuoteTick? message = null;
            source.SpotTick += (_, update) => message = update;

            await source.Start(1);

            Assert.NotNull(message);
            Assert.Equal("INIT", message.Name);
            Assert.Equal(100.0, message.Value, 10);
        }

        private static Dictionary<string, JsonElement> ToDictionary(JsonDocument document)
        {
            return document.RootElement
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
