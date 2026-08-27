namespace EventGraph.Tests
{
    public class GraphKeyTests
    {
        [Fact]
        public void GraphKeyFormatsTypeAndName()
        {
            Assert.Equal("EquityOption::AAPL", GraphKey.Of("EquityOption", "AAPL"));
        }

        [Fact]
        public void GraphKeyRejectsBlankType()
        {
            _ = Assert.Throws<ArgumentException>(() => GraphKey.Of(" ", "AAPL"));
        }

        [Fact]
        public void GraphKeyRejectsBlankName()
        {
            _ = Assert.Throws<ArgumentException>(() => GraphKey.Of("EquityOption", " "));
        }
    }
}
