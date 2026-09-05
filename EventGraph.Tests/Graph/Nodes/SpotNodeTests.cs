namespace EventGraph.Tests
{
    public class SpotNodeTests
    {
        [Fact]
        public void SpotNodeDelegatesToTheUnderlyingEquitySource()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);

            var spotNode = new SpotNode(equity);

            Assert.Equal("AAPL", spotNode.Name);
            Assert.Equal(100.0, spotNode.Spot);
            Assert.Equal("USD", spotNode.Definition.Currency);
            _ = Assert.IsAssignableFrom<IDefinitionProvider<SpotDefinition>>(spotNode);
            Assert.Equal(nameof(SpotNode), spotNode.Type);
            Assert.Equal([equity], spotNode.Dependencies);
        }

        [Fact]
        public void SpotNodeRejectsNullSource()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new SpotNode(null));
        }

        [Fact]
        public async Task SpotNodeTicksWhenTheUnderlyingEquitySourceTicks()
        {
            var equity = new EquitySource("AAPL", 100.0, 0.2, 0.0);
            var spotNode = new SpotNode(equity);
            QuoteTick? update = null;
            spotNode.Tick += (_, message) => update = message;

            await equity.Start(1);

            Assert.NotNull(update);
            Assert.Equal("AAPL", update.Name);
        }
    }
}
