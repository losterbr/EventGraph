using System.Text.Json;

namespace EventGraph.Tests
{
    public class SimulatedQuoteSourceTests
    {
        [Fact]
        public async Task SimulatedQuoteSourceCanRunMultipleTicks()
        {
            var updates = new List<QuoteTick>();
            var source = new SimulatedQuoteSource("XYZ", 100.0, 0.2, 0.0);
            source.Tick += (_, message) => updates.Add(message);

            await source.Start(2);

            Assert.Equal(2, updates.Count);
        }

        [Fact]
        public async Task SimulatedQuoteSourceCanRunInContinuousModeUntilCancelled()
        {
            var updates = new List<QuoteTick>();
            var source = new SimulatedQuoteSource("CONT", 50.0, 0.1, 0.0);
            source.Tick += (_, message) => updates.Add(message);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(10);

            await source.Start(0, cts.Token);

            Assert.NotEmpty(updates);
        }

        [Fact]
        public void SimulatedQuoteSourceRejectsInvalidName()
        {
            _ = Assert.Throws<ArgumentException>(() => new SimulatedQuoteSource(" ", 100.0, 0.1));
        }

        [Fact]
        public void SimulatedQuoteSourceRejectsDefinitionsWithoutNames()
        {
            using var definition = JsonDocument.Parse("""
        {
          "spot": 100,
          "volatility": 0.2,
          "meanTickTimeSeconds": 1
        }
        """);

            var exception = Assert.Throws<InvalidDataException>(() =>
                new SimulatedQuoteSource(ToDictionary(definition)));

            Assert.Contains("name", exception.Message);
        }

        [Fact]
        public void SimulatedQuoteSourceHasTheExpectedType()
        {
            var source = new SimulatedQuoteSource("TYPE", 100.0, 0.1);

            Assert.Equal("SimulatedSpot", source.Type);
        }

        [Fact]
        public void SimulatedQuoteSourceRejectsInvalidStartingSpot()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedQuoteSource("A", double.NaN, 0.1));
        }

        [Fact]
        public void SimulatedQuoteSourceRejectsNegativeVolatility()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedQuoteSource("A", 100.0, -0.1));
        }

        [Fact]
        public void SimulatedQuoteSourceRejectsNegativeMeanTickTime()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedQuoteSource("A", 100.0, 0.1, -1.0));
        }

        [Fact]
        public async Task SimulatedQuoteSourceEmitsInitialValueBeforeMovement()
        {
            var source = new SimulatedQuoteSource("INIT", 100.0, 0.0, 0.0);
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
