using System;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class AppOptionsTests
{
    [Fact]
    public void ParseArgumentsSupportsCustomTickCount()
    {
        var options = AppOptionsParser.Parse(new[] { "--ticks", "12" });

        Assert.Equal(12, options.TickCount);
    }

    [Fact]
    public void ParseArgumentsSupportsHelpFlag()
    {
        var options = AppOptionsParser.Parse(new[] { "--help" });

        Assert.True(options.ShowHelp);
    }

    [Fact]
    public void ParseArgumentsSupportsQuietAndSymbols()
    {
        var options = AppOptionsParser.Parse(new[] { "--quiet", "--symbols", "A,B,C" });

        Assert.True(options.Quiet);
        Assert.Equal(new[] { "A", "B", "C" }, options.Symbols);
    }

    [Fact]
    public void ParseArgumentsSupportsCustomBasketColor()
    {
        var options = AppOptionsParser.Parse(new[] { "--basket-color", "Yellow" });

        Assert.Equal(ConsoleColor.Yellow, options.BasketColor);
    }

    [Fact]
    public void ParseArgumentsReturnsDefaultValuesWhenNoArgumentsAreProvided()
    {
        var options = AppOptionsParser.Parse(Array.Empty<string>());

        Assert.Equal(0, options.TickCount);
        Assert.False(options.Quiet);
        Assert.False(options.ShowHelp);
        Assert.Equal(new[] { "TSLA", "GOOG", "AMZN" }, options.Symbols);
        Assert.Equal(ConsoleColor.Cyan, options.BasketColor);
    }

    [Fact]
    public void ParseArgumentsThrowsForMissingTickValue()
    {
        Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(new[] { "--ticks" }));
    }

    [Fact]
    public void ParseArgumentsThrowsForInvalidTickValue()
    {
        Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(new[] { "--ticks", "0" }));
    }

    [Fact]
    public void ParseArgumentsThrowsForMissingBasketColorValue()
    {
        Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(new[] { "--basket-color" }));
    }

    [Fact]
    public void ParseArgumentsThrowsForInvalidBasketColor()
    {
        Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(new[] { "--basket-color", "not-a-color" }));
    }

    [Fact]
    public void ParseArgumentsThrowsForMissingSymbolsValue()
    {
        Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(new[] { "--symbols" }));
    }

    [Fact]
    public void ParseArgumentsThrowsForEmptySymbols()
    {
        Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(new[] { "--symbols", "" }));
    }

    [Fact]
    public void ParseArgumentsThrowsForUnknownArgument()
    {
        Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(new[] { "--unexpected" }));
    }
}
