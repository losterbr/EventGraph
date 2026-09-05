namespace EventGraph.Tests
{
    /// <summary>
    /// Enforces the source-layer boundary: concrete source types expose only
    /// <c>I*SourceNode</c> capabilities plus the neutral shared
    /// <see cref="ISpotValueNode"/> base. The graph-root interfaces are omitted
    /// because they are inherited through those capability interfaces.
    /// </summary>
    public class SourceNodeArchitectureTests
    {
        [Fact]
        public void SourceClassesImplementOnlySourceNodeInterfaces()
        {
            // .editorconfig enforces the *Source suffix in Graph/Sources; this
            // test enforces the matching I*SourceNode capability convention.
            var graphAssembly = typeof(EquitySource).Assembly;
            var sourceTypes = graphAssembly
                .GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false }
                    && type.Namespace == typeof(EquitySource).Namespace
                    && type.Name.EndsWith("Source", StringComparison.Ordinal));

            foreach (var sourceType in sourceTypes)
            {
                var sourceInterfaces = sourceType.GetInterfaces()
                    .Where(@interface => @interface != typeof(IGraphNode)
                        && @interface != typeof(ITickingNode)
                        && @interface != typeof(ISpotValueNode)
                        && @interface.Name != "ISpotDefinitionOwner"
                        && (!@interface.IsGenericType || @interface.GetGenericTypeDefinition() != typeof(IDefinitionProvider<>)));

                Assert.NotEmpty(sourceInterfaces);
                Assert.All(sourceInterfaces, @interface =>
                {
                    Assert.StartsWith("I", @interface.Name, StringComparison.Ordinal);
                    Assert.EndsWith("SourceNode", @interface.Name, StringComparison.Ordinal);
                });
            }
        }
    }
}