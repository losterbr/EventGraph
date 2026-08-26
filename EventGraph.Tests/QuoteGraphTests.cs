namespace EventGraph.Tests
{
    public class QuoteGraphTests
    {
        [Fact]
        public void QuoteGraphRejectsNullNodeLists()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new QuoteGraph(null));
        }

        [Fact]
        public void QuoteGraphRejectsNullNodes()
        {
            _ = Assert.Throws<ArgumentException>(() => new QuoteGraph([null]));
        }

        [Fact]
        public void QuoteGraphRejectsDuplicateNamesCaseInsensitively()
        {
            var first = new SimulatedQuoteSource("A", 100.0, 0.0, 0.0);
            var second = new SimulatedQuoteSource("a", 200.0, 0.0, 0.0);

            _ = Assert.Throws<ArgumentException>(() => new QuoteGraph([first, second]));
        }

        [Fact]
        public void QuoteGraphRejectsDependenciesOutsideTheGraph()
        {
            var source = new SimulatedQuoteSource("A", 100.0, 0.0, 0.0);
            var basket = new BasketAggregate("BASKET", [source]);

            _ = Assert.Throws<KeyNotFoundException>(() => new QuoteGraph([basket]));
        }

        [Fact]
        public void GetIndexRejectsUnknownNodeNames()
        {
            var source = new SimulatedQuoteSource("A", 100.0, 0.0, 0.0);
            var graph = new QuoteGraph([source]);

            _ = Assert.Throws<KeyNotFoundException>(() => graph.GetIndex("Missing"));
        }
    }
}