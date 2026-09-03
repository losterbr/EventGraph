namespace EventGraph.Tests
{
    /// <summary>
    /// Enforces the graph-node and instrument boundary: concrete types expose no
    /// <c>I*SourceNode</c> interfaces. SourceNodeArchitectureTests and
    /// .editorconfig enforce the complementary source-layer convention.
    /// </summary>
    public class NodeArchitectureTests
    {
        [Fact]
        public void NodeClassesDoNotImplementSourceNodeInterfaces()
        {
            // .editorconfig enforces the *Node suffix in Graph/Nodes and
            // Graph/Instruments; this test prevents them from exposing source capabilities.
            var graphAssembly = typeof(SpotNode).Assembly;
            var nodeTypes = graphAssembly
                .GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false }
                    && (type.Namespace == typeof(SpotNode).Namespace
                        || type.Namespace == typeof(EquityOptionNode).Namespace)
                    && type.Name.EndsWith("Node", StringComparison.Ordinal));

            foreach (var nodeType in nodeTypes)
            {
                Assert.DoesNotContain(nodeType.GetInterfaces(), @interface =>
                    @interface.Name.StartsWith('I')
                    && @interface.Name.EndsWith("SourceNode", StringComparison.Ordinal));
            }
        }
    }
}