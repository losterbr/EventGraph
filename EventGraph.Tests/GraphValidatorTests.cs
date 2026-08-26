namespace EventGraph.Tests
{
    public class GraphValidatorTests
    {
        [Fact]
        public void EnsureAcyclicAcceptsAnAcyclicGraph()
        {
            var source = new TestNode("Source");
            var left = new TestNode("Left", source);
            var right = new TestNode("Right", source);

            GraphValidator.EnsureAcyclic([left, right]);
        }

        [Fact]
        public void EnsureAcyclicRejectsASelfCycle()
        {
            var node = new TestNode("Self");
            node.SetDependencies(node);

            var exception = Assert.Throws<InvalidOperationException>(() => GraphValidator.EnsureAcyclic([node]));

            Assert.Contains("Self", exception.Message);
        }

        [Fact]
        public void EnsureAcyclicRejectsAnIndirectCycle()
        {
            var first = new TestNode("First");
            var second = new TestNode("Second", first);
            first.SetDependencies(second);

            _ = Assert.Throws<InvalidOperationException>(() => GraphValidator.EnsureAcyclic([first]));
        }

        [Fact]
        public void EnsureAcyclicRejectsNullRoots()
        {
            _ = Assert.Throws<ArgumentNullException>(() => GraphValidator.EnsureAcyclic(null));
        }

        [Fact]
        public void EnsureAcyclicRejectsNullNodes()
        {
            _ = Assert.Throws<ArgumentException>(() => GraphValidator.EnsureAcyclic([null]));
        }

        private sealed class TestNode(string name, params IGraphNode[] dependencies) : IGraphNode
        {
            public string Name { get; } = name;

            public string Type => "TestNode";

            public IReadOnlyList<IGraphNode> Dependencies { get; private set; } = dependencies;

            public void SetDependencies(params IGraphNode[] dependencies)
            {
                Dependencies = dependencies;
            }
        }
    }
}