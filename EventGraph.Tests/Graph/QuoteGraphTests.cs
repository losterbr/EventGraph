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
            var first = new SimulatedAssetSource("A", 100.0, 0.0, 0.0);
            var second = new SimulatedAssetSource("a", 200.0, 0.0, 0.0);

            _ = Assert.Throws<ArgumentException>(() => new QuoteGraph([first, second]));
        }

        [Fact]
        public void QuoteGraphRejectsDependenciesOutsideTheGraph()
        {
            var source = new SimulatedAssetSource("A", 100.0, 0.0, 0.0);
            var basket = new BasketAggregate("BASKET", [source]);

            _ = Assert.Throws<KeyNotFoundException>(() => new QuoteGraph([basket]));
        }

        [Fact]
        public void GetIndexRejectsUnknownNodeNames()
        {
            var source = new SimulatedAssetSource("A", 100.0, 0.0, 0.0);
            var graph = new QuoteGraph([source]);

            _ = Assert.Throws<KeyNotFoundException>(() => graph.GetIndex("Missing"));
        }

        [Fact]
        public void QuoteGraphExposesDependentIndices()
        {
            var sourceA = new SimulatedAssetSource("A", 100.0, 0.0, 0.0);
            var sourceB = new SimulatedAssetSource("B", 200.0, 0.0, 0.0);
            var parent = new BasketAggregate("PARENT", [sourceA, sourceB]);
            var child = new BasketAggregate("CHILD", [parent]);
            var graph = new QuoteGraph([sourceA, sourceB, parent, child]);

            Assert.Equal([graph.GetIndex("PARENT")], graph.DependentsByNode[graph.GetIndex("A")]);
            Assert.Equal([graph.GetIndex("PARENT")], graph.DependentsByNode[graph.GetIndex("B")]);
            Assert.Equal([graph.GetIndex("CHILD")], graph.DependentsByNode[graph.GetIndex("PARENT")]);
            Assert.Empty(graph.DependentsByNode[graph.GetIndex("CHILD")]);
        }
    }
}