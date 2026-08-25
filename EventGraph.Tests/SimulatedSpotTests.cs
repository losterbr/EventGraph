using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class SimulatedSpotTests
{
    [Fact]
    public async Task SimulatedSpotCanRunMultipleTicks()
    {
        var updates = new List<SpotMessage>();
        var spot = new SimulatedSpot("XYZ", 100.0, 0.2, 0.0);
        spot.Tick += (_, message) => updates.Add(message);

        await spot.Start(2);

        Assert.Equal(2, updates.Count);
    }

    [Fact]
    public async Task SimulatedSpotCanRunInContinuousModeUntilCancelled()
    {
        var updates = new List<SpotMessage>();
        var spot = new SimulatedSpot("CONT", 50.0, 0.1, 0.0);
        spot.Tick += (_, message) => updates.Add(message);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(10);

        await spot.Start(0, cts.Token);

        Assert.NotEmpty(updates);
    }

    [Fact]
    public void SimulatedSpotRejectsInvalidName()
    {
        Assert.Throws<ArgumentException>(() => new SimulatedSpot(" ", 100.0, 0.1));
    }

    [Fact]
    public void SimulatedSpotRejectsInvalidStartingSpot()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedSpot("A", double.NaN, 0.1));
    }

    [Fact]
    public void SimulatedSpotRejectsNegativeVolatility()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedSpot("A", 100.0, -0.1));
    }

    [Fact]
    public void SimulatedSpotRejectsNegativeMeanTickTime()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulatedSpot("A", 100.0, 0.1, -1.0));
    }

    [Fact]
    public async Task SimulatedSpotEmitsInitialValueBeforeMovement()
    {
        var spot = new SimulatedSpot("INIT", 100.0, 0.0, 0.0);
        SpotMessage? message = null;
        spot.Tick += (_, update) => message = update;

        await spot.Start(1);

        Assert.NotNull(message);
        Assert.Equal("INIT", message!.Name);
        Assert.Equal(100.0, message.Value, 10);
    }
}
