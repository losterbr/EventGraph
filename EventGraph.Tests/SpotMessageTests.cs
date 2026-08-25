using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class SpotMessageTests
{
    [Fact]
    public void ValueSetterUpdatesTheStoredValue()
    {
        var message = new SpotMessage("AAPL", 42.5);

        message.Value = 99.0;

        Assert.Equal(99.0, message.Value);
    }

    [Fact]
    public void ConstructorRejectsNullOrWhitespaceNames()
    {
        Assert.Throws<ArgumentException>(() => new SpotMessage(null!, 42.5));
        Assert.Throws<ArgumentException>(() => new SpotMessage("   ", 42.5));
    }

    [Fact]
    public void ValueSetterRejectsNonFiniteValues()
    {
        var message = new SpotMessage("AAPL", 42.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => message.Value = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => message.Value = double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => message.Value = double.NegativeInfinity);
    }
}
