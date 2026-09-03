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
            var first = new EquitySource("A", 100.0, 0.0, 0.0);
            var second = new EquitySource("a", 200.0, 0.0, 0.0);

            _ = Assert.Throws<ArgumentException>(() => new QuoteGraph([first, second]));
        }

        [Fact]
        public void QuoteGraphRejectsDependenciesOutsideTheGraph()
        {
            var source = new EquitySource("A", 100.0, 0.0, 0.0);
            var basket = new BasketSpotNode("BASKET", [new SpotNode(source)]);

            _ = Assert.Throws<KeyNotFoundException>(() => new QuoteGraph([basket]));
        }

        [Fact]
        public void GetIndexRejectsUnknownNodeNames()
        {
            var source = new EquitySource("A", 100.0, 0.0, 0.0);
            var graph = new QuoteGraph([source]);

            _ = Assert.Throws<KeyNotFoundException>(() => graph.GetIndex("Missing"));
        }

        [Fact]
        public void QuoteGraphExposesDependentIndices()
        {
            var sourceA = new EquitySource("A", 100.0, 0.0, 0.0);
            var sourceB = new EquitySource("B", 200.0, 0.0, 0.0);
            var spotA = new SpotNode(sourceA);
            var spotB = new SpotNode(sourceB);
            var parent = new BasketSpotNode("PARENT", [spotA, spotB]);
            var childSpot = new SpotNode(parent);
            var child = new BasketSpotNode("CHILD", [childSpot]);
            var graph = new QuoteGraph([sourceA, sourceB, spotA, spotB, parent, childSpot, child]);

            Assert.Equal([graph.GetIndex("SpotNode::A")], graph.DependentsByNode[graph.GetIndex("EquitySource::A")]);
            Assert.Equal([graph.GetIndex("SpotNode::B")], graph.DependentsByNode[graph.GetIndex("EquitySource::B")]);
            Assert.Equal([graph.GetIndex("BasketSpotNode::PARENT")], graph.DependentsByNode[graph.GetIndex("SpotNode::A")]);
            Assert.Equal([graph.GetIndex("BasketSpotNode::PARENT")], graph.DependentsByNode[graph.GetIndex("SpotNode::B")]);
            Assert.Equal([graph.GetIndex("SpotNode::PARENT")], graph.DependentsByNode[graph.GetIndex("BasketSpotNode::PARENT")]);
            Assert.Equal([graph.GetIndex("CHILD")], graph.DependentsByNode[graph.GetIndex("SpotNode::PARENT")]);
            Assert.Empty(graph.DependentsByNode[graph.GetIndex("CHILD")]);
        }
    }
}