namespace EventGraph.Tests
{
    public class GraphKeyTests
    {
        [Fact]
        public void GraphKeyFormatsTypeAndName()
        {
            Assert.Equal("EquityOptionNode::AAPL", GraphKey.Of("EquityOptionNode", "AAPL"));
        }

        [Fact]
        public void GraphKeyRejectsBlankType()
        {
            _ = Assert.Throws<ArgumentException>(() => GraphKey.Of(" ", "AAPL"));
        }

        [Fact]
        public void GraphKeyRejectsBlankName()
        {
            _ = Assert.Throws<ArgumentException>(() => GraphKey.Of("EquityOptionNode", " "));
        }
    }
}
