namespace EventGraph.Tests
{
    public class QuoteTickTests
    {
        [Fact]
        public void ValueSetterUpdatesTheStoredValue()
        {
            var message = new QuoteTick("AAPL", 42.5)
            {
                Value = 99.0
            };

            Assert.Equal(99.0, message.Value);
        }

        [Fact]
        public void ConstructorRejectsNullOrWhitespaceNames()
        {
            _ = Assert.Throws<ArgumentException>(() => new QuoteTick(null, 42.5));
            _ = Assert.Throws<ArgumentException>(() => new QuoteTick("   ", 42.5));
        }

        [Fact]
        public void ValueSetterRejectsNonFiniteValues()
        {
            var message = new QuoteTick("AAPL", 42.5);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => message.Value = double.NaN);
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => message.Value = double.PositiveInfinity);
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => message.Value = double.NegativeInfinity);
        }
    }
}
