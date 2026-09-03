using System.Text.Json;

namespace EventGraph.Tests
{
    public class SimulatedAssetSourceTests
    {
        [Fact]
        public async Task SimulatedAssetSourceCanRunMultipleTicks()
        {
            var updates = new List<QuoteTick>();
            var source = new EquitySource("XYZ", 100.0, 0.2, 0.0);
            source.Tick += (_, message) => updates.Add(message);

            await source.Start(2);

            Assert.Equal(2, updates.Count);
        }

        [Fact]
        public async Task SimulatedAssetSourceCanRunInContinuousModeUntilCancelled()
        {
            var updates = new List<QuoteTick>();
            var source = new EquitySource("CONT", 50.0, 0.1, 0.0);
            using var cts = new CancellationTokenSource();
            source.Tick += (_, message) =>
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
            _ = Assert.Throws<ArgumentException>(() => new EquitySource(" ", 100.0, 0.1));
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
                new EquitySource(ToDictionary(definition)));

            Assert.Contains("name", exception.Message);
        }

        [Fact]
        public void SimulatedAssetSourceHasTheExpectedType()
        {
            var source = new EquitySource("TYPE", 100.0, 0.1);

            Assert.Equal(nameof(EquitySource), source.Type);
        }

        [Fact]
        public void SimulatedAssetSourceExposesVolatilityQuote()
        {
            var source = new EquitySource("VOL", 100.0, 0.25);

            var volNode = Assert.IsAssignableFrom<IVolSourceNode>(source);
            var spotNode = Assert.IsAssignableFrom<ISpotSourceNode>(source);
            Assert.Equal(0.25, volNode.Volatility, 10);
            Assert.Equal(100.0, spotNode.Spot, 10);
            Assert.Equal("USD", spotNode.Currency);
        }

        [Fact]
        public void SimulatedAssetSourceRejectsInvalidStartingSpot()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquitySource("A", double.NaN, 0.1));
        }

        [Fact]
        public void SimulatedAssetSourceRejectsNegativeVolatility()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquitySource("A", 100.0, -0.1));
        }

        [Fact]
        public void SimulatedAssetSourceRejectsNegativeMeanTickTime()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquitySource("A", 100.0, 0.1, -1.0));
        }

        [Fact]
        public void SimulatedAssetSourceRejectsInfiniteMeanTickTime()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquitySource("A", 100.0, 0.1, double.PositiveInfinity));
        }

        [Fact]
        public void SimulatedAssetSourceRejectsBlankCurrency()
        {
            _ = Assert.Throws<ArgumentException>(() => new EquitySource("A", 100.0, 0.1, 1.0, " "));
        }

        [Fact]
        public void SimulatedAssetSourceRejectsNegativeInfinitySpot()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquitySource("A", double.NegativeInfinity, 0.1));
        }

        [Fact]
        public void SimulatedAssetSourceRejectsNaNVolatility()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquitySource("A", 100.0, double.NaN));
        }

        [Fact]
        public void SimulatedAssetSourceRejectsInfiniteVolatility()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new EquitySource("A", 100.0, double.PositiveInfinity));
        }

        [Fact]
        public void SimulatedAssetSourceDefaultsCurrencyToUsd()
        {
            var source = new EquitySource("A", 100.0, 0.1);

            Assert.Equal("USD", source.Currency);
        }

        [Fact]
        public async Task SimulatedAssetSourceEmitsInitialValueBeforeMovement()
        {
            var source = new EquitySource("INIT", 100.0, 0.0, 0.0);
            QuoteTick? message = null;
            source.Tick += (_, update) => message = update;

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
