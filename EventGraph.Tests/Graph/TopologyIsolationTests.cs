using System.Reflection;
using System.Text.Json;

namespace EventGraph.Tests
{
    public class TopologyIsolationTests
    {
        [Fact]
        public void RegisteredTopologyHooksDoNotAcceptGraphNodes()
        {
            var graphAssembly = typeof(NodeRegistry).Assembly;
            foreach (var nodeTypeName in NodeRegistry.SupportedTypes)
            {
                var nodeType = graphAssembly.GetTypes().Single(type => type.Name == nodeTypeName);
                AssertDefinitionOnlyParameters(nodeType, "EnrichDefinition", "EventGraph.GraphDefinitionEnrichmentContext", typeof(IReadOnlyDictionary<string, JsonElement>).FullName!);
                AssertDefinitionOnlyParameters(nodeType, "GetDependencyNames", typeof(IReadOnlyDictionary<string, JsonElement>).FullName!);
            }
        }

        [Fact]
        public void QuoteGraphSnapshotsDependenciesAtConstruction()
        {
            var dependency = new TestNode("DEPENDENCY");
            var node = new TestNode("NODE", dependency);
            var graph = new QuoteGraph([dependency, node]);

            node.SetDependencies();

            Assert.Equal([graph.GetIndex("TestNode::DEPENDENCY")], graph.DependenciesByNode[graph.GetIndex("TestNode::NODE")]);
        }

        private static void AssertDefinitionOnlyParameters(Type nodeType, string methodName, params string[] expectedParameterTypeNames)
        {
            var method = nodeType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.NotNull(method);
            Assert.Equal(expectedParameterTypeNames, method.GetParameters().Select(parameter => parameter.ParameterType.FullName));
        }

        private sealed class TestNode(string name, params IGraphNode[] dependencies) : IGraphNode
        {
            private IReadOnlyList<IGraphNode> dependencies = dependencies;

            public string Name { get; } = name;

            public string Type => nameof(TestNode);

            public IReadOnlyList<IGraphNode> Dependencies => dependencies;

            public void SetDependencies(params IGraphNode[] updatedDependencies)
            {
                dependencies = updatedDependencies;
            }
        }
    }
}