using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class QuoteTickTests
{
    [Fact]
    public void ValueSetterUpdatesTheStoredValue()
    {
        var message = new QuoteTick("AAPL", 42.5);

        message.Value = 99.0;

        Assert.Equal(99.0, message.Value);
    }

    [Fact]
    public void ConstructorRejectsNullOrWhitespaceNames()
    {
        Assert.Throws<ArgumentException>(() => new QuoteTick(null!, 42.5));
        Assert.Throws<ArgumentException>(() => new QuoteTick("   ", 42.5));
    }

    [Fact]
    public void ValueSetterRejectsNonFiniteValues()
    {
        var message = new QuoteTick("AAPL", 42.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => message.Value = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => message.Value = double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => message.Value = double.NegativeInfinity);
    }
}
