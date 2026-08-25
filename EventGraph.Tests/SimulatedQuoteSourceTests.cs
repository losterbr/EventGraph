using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class SimulatedQuoteSourceTests
{
    [Fact]
    public async Task SimulatedQuoteSourceCanRunMultipleTicks()
    {
        var updates = new List<QuoteTick>();
        var source = new SimulatedQuoteSource("XYZ", 100.0, 0.2, 0.0);
        source.Tick += (_, message) => updates.Add(message);

        await source.Start(2);

        Assert.Equal(2, updates.Count);
    }

    [Fact]
    public async Task SimulatedQuoteSourceCanRunInContinuousModeUntilCancelled()
    {
        var updates = new List<QuoteTick>();
        var source = new SimulatedQuoteSource("CONT", 50.0, 0.1, 0.0);
        source.Tick += (_, message) => updates.Add(message);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(10);

        await source.Start(0, cts.Token);

        Assert.NotEmpty(updates);
    }

    [Fact]
    public void SimulatedQuoteSourceRejectsInvalidName()
    {
        Assert.Throws<ArgumentException>(() => new SimulatedQuoteSource(" ", 100.0, 0.1));
    }

    [Fact]
    public void SimulatedQuoteSourceRejectsInvalidStartingSpot()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedQuoteSource("A", double.NaN, 0.1));
    }

    [Fact]
    public void SimulatedQuoteSourceRejectsNegativeVolatility()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedQuoteSource("A", 100.0, -0.1));
    }

    [Fact]
    public void SimulatedQuoteSourceRejectsNegativeMeanTickTime()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedQuoteSource("A", 100.0, 0.1, -1.0));
    }

    [Fact]
    public async Task SimulatedQuoteSourceEmitsInitialValueBeforeMovement()
    {
        var source = new SimulatedQuoteSource("INIT", 100.0, 0.0, 0.0);
        QuoteTick? message = null;
        source.Tick += (_, update) => message = update;

        await source.Start(1);

        Assert.NotNull(message);
        Assert.Equal("INIT", message!.Name);
        Assert.Equal(100.0, message.Value, 10);
    }
}
