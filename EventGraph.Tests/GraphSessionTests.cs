using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EventGraph.Tests
{
    public class GraphSessionTests
    {
        [Fact]
        public void GraphSessionInitializesGraphForClockDate()
        {
            var directory = CreateDirectory();
            try
            {
                WriteSampleFiles(directory);
                var clock = new MarketClock(new DateTime(2026, 6, 1));
                using var session = new GraphSession(directory, clock);

                Assert.NotNull(session.CurrentGraph);
                Assert.Equal(clock, session.Clock);
                Assert.Contains(session.CurrentGraph.Nodes, node => node is EquitySource && node.Name == "AAPL");
                Assert.Contains(session.CurrentGraph.Nodes, node => node is EquityOptionNode);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void GraphSessionRebuildsGraphWhenClockAdvances()
        {
            var directory = CreateDirectory();
            try
            {
                WriteSampleFiles(directory);
                var clock = new MarketClock(new DateTime(2026, 6, 1));
                using var session = new GraphSession(directory, clock);

                var initialGraph = session.CurrentGraph;
                QuoteGraph? rebuiltGraph = null;
                session.GraphRebuilt += (_, g) => rebuiltGraph = g;

                clock.AdvanceDays(30);

                Assert.NotNull(rebuiltGraph);
                Assert.NotSame(initialGraph, session.CurrentGraph);
                Assert.Same(rebuiltGraph, session.CurrentGraph);

                var option = session.CurrentGraph.Nodes.OfType<EquityOptionNode>().Single();
                Assert.Equal(new DateTime(2026, 7, 1), option.ValuationDate);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void GraphSessionPreservesSpotStateAcrossRebuilds()
        {
            var directory = CreateDirectory();
            try
            {
                WriteSampleFiles(directory);
                var clock = new MarketClock(new DateTime(2026, 6, 1));
                using var session = new GraphSession(directory, clock);

                var quote = session.CurrentGraph.Nodes.OfType<EquitySource>().Single(q => q.Name == "AAPL");
                var spotProperty = typeof(EquitySource).GetProperty(nameof(EquitySource.Spot));
                spotProperty!.SetValue(quote, 237.5);

                clock.AdvanceDays(1);

                var newQuote = session.CurrentGraph.Nodes.OfType<EquitySource>().Single(q => q.Name == "AAPL");
                Assert.Equal(237.5, newQuote.Spot);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void GraphSessionOmitsExpiredOptionsOnRebuild()
        {
            var directory = CreateDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "aapl.json"), /*lang=json,strict*/ """
                {
                  "type": "EquityDefinition",
                  "name": "AAPL",
                  "currency": "USD",
                  "spot": 225.0,
                  "volatility": 0.28
                }
                """);
                File.WriteAllText(Path.Combine(directory, "usd.json"), /*lang=json,strict*/ """
                {
                  "type": "CurrencyRateDefinition",
                  "name": "USD",
                  "interestRate": 0.02
                }
                """);
                File.WriteAllText(Path.Combine(directory, "option.json"), /*lang=json,strict*/ """
                {
                  "type": "EquityOptionDefinition",
                  "name": "AAPL_EXPIRING",
                  "underlyer": "AAPL",
                  "maturity": "2026-06-15",
                  "strike": 225.0,
                  "optionType": "Call"
                }
                """);

                var clock = new MarketClock(new DateTime(2026, 6, 1));
                using var session = new GraphSession(directory, clock);

                Assert.Contains(session.CurrentGraph.Nodes, node => node is EquityOptionNode);

                // Advance clock past option maturity
                clock.SetDate(new DateTime(2026, 6, 16));

                Assert.DoesNotContain(session.CurrentGraph.Nodes, node => node is EquityOptionNode);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void GraphSessionWiresSubscriberAndTearsDownOnRebuild()
        {
            var directory = CreateDirectory();
            try
            {
                WriteSampleFiles(directory);
                var clock = new MarketClock(new DateTime(2026, 6, 1));
                var subscriber = new QuoteSubscriber(quiet: true);
                using var session = new GraphSession(directory, clock, subscriber);

                clock.AdvanceDays(1);

                Assert.NotNull(session.CurrentGraph);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void GraphSessionCanDisableStatePreservation()
        {
            var directory = CreateDirectory();
            try
            {
                WriteSampleFiles(directory);
                var clock = new MarketClock(new DateTime(2026, 6, 1));
                using var session = new GraphSession(directory, clock, preserveStateAcrossDates: false);

                clock.AdvanceDays(1);

                Assert.NotNull(session.CurrentGraph);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void GraphSessionRejectsBlankDirectory()
        {
            _ = Assert.Throws<ArgumentException>(() => new GraphSession(" "));
        }

        private static string CreateDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _ = Directory.CreateDirectory(directory);
            return directory;
        }

        private static void WriteSampleFiles(string directory)
        {
            File.WriteAllText(Path.Combine(directory, "aapl.json"), /*lang=json,strict*/ """
            {
              "type": "EquityDefinition",
              "name": "AAPL",
              "currency": "USD",
              "spot": 225.0,
              "volatility": 0.28,
              "meanTickTimeSeconds": 4.5
            }
            """);
            File.WriteAllText(Path.Combine(directory, "usd.json"), /*lang=json,strict*/ """
            {
              "type": "CurrencyRateDefinition",
              "name": "USD",
              "interestRate": 0.02
            }
            """);
            File.WriteAllText(Path.Combine(directory, "basket.json"), /*lang=json,strict*/ """
            {
              "type": "BasketDefinition",
              "name": "TECH",
              "currency": "USD",
              "constituents": ["AAPL"],
              "weights": [1.0]
            }
            """);
            File.WriteAllText(Path.Combine(directory, "option.json"), /*lang=json,strict*/ """
            {
              "type": "EquityOptionDefinition",
              "name": "AAPL_1Y_CALL",
              "underlyer": "AAPL",
              "maturity": "1Y",
              "strike": 225.0,
              "optionType": "Call"
            }
            """);
        }
    }
}
