using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class BasketSpotTests
{
    [Fact]
    public async Task BasketSpotRaisesUpdateAfterConstituentsPublish()
    {
        var quotes = new[]
        {
            new SimulatedSpot("A", 100.0, 0.0, 0.0),
            new SimulatedSpot("B", 200.0, 0.0, 0.0)
        };

        var basket = new BasketSpot(quotes);
        var updates = new List<SpotMessage>();
        basket.Tick += (_, message) => updates.Add(message);

        await basket.RunOnceAsync();

        Assert.Single(updates);
        Assert.Equal(150.0, updates[0].Value, 10);
    }

    [Fact]
    public async Task BasketSpotUsesProvidedWeights()
    {
        var quotes = new[]
        {
            new SimulatedSpot("A", 100.0, 0.0, 0.0),
            new SimulatedSpot("B", 200.0, 0.0, 0.0)
        };

        var basket = new BasketSpot(quotes, new[] { 0.25, 0.75 });
        var updates = new List<SpotMessage>();
        basket.Tick += (_, message) => updates.Add(message);

        await basket.RunOnceAsync();

        Assert.Single(updates);
        Assert.Equal(175.0, updates[0].Value, 10);
    }

    [Fact]
    public async Task BasketSpotSkipsZeroWeightConstituents()
    {
        var quotes = new[]
        {
            new SimulatedSpot("A", 100.0, 0.0, 0.0),
            new SimulatedSpot("B", 200.0, 0.0, 0.0)
        };

        var basket = new BasketSpot(quotes, new[] { 0.0, 1.0 });
        var updates = new List<SpotMessage>();
        basket.Tick += (_, message) => updates.Add(message);

        await quotes[0].Start(1);

        Assert.Empty(updates);
    }

    [Fact]
    public void BasketSpotThrowsWhenWeightsDoNotSumToOne()
    {
        var quotes = new[]
        {
            new SimulatedSpot("A", 100.0, 0.0, 0.0),
            new SimulatedSpot("B", 200.0, 0.0, 0.0)
        };

        Assert.Throws<ArgumentException>(() => new BasketSpot(quotes, new[] { 0.6, 0.3 }));
    }

    [Fact]
    public void BasketSpotThrowsWhenWeightCountDoesNotMatchConstituents()
    {
        var quotes = new[]
        {
            new SimulatedSpot("A", 100.0, 0.0, 0.0),
            new SimulatedSpot("B", 200.0, 0.0, 0.0)
        };

        Assert.Throws<ArgumentException>(() => new BasketSpot(quotes, new[] { 0.5 }));
    }

    [Fact]
    public void BasketSpotThrowsWhenConstituentsAreNull()
    {
        Assert.Throws<ArgumentException>(() => new BasketSpot(null!));
    }

    [Fact]
    public void BasketSpotThrowsWhenNoConstituentsAreProvided()
    {
        Assert.Throws<ArgumentException>(() => new BasketSpot(Array.Empty<SimulatedSpot>()));
    }

    [Fact]
    public async Task BasketSpotPublishesAggregateOnlyWhenAllConstituentsAreAvailable()
    {
        var quotes = new[]
        {
            new SimulatedSpot("A", 100.0, 0.0, 0.0),
            new SimulatedSpot("B", 200.0, 0.0, 0.0)
        };

        var basket = new BasketSpot(quotes);
        var updates = new List<SpotMessage>();
        basket.Tick += (_, message) => updates.Add(message);

        await quotes[0].Start(1);

        Assert.Empty(updates);
    }

    [Fact]
    public async Task BasketSpotPublishesAggregateWhenAllConstituentEventsArrive()
    {
        var quotes = new[]
        {
            new SimulatedSpot("A", 100.0, 0.0, 0.0),
            new SimulatedSpot("B", 200.0, 0.0, 0.0)
        };

        var basket = new BasketSpot(quotes);
        var updates = new List<SpotMessage>();
        basket.Tick += (_, message) => updates.Add(message);

        await Task.WhenAll(quotes[0].Start(1), quotes[1].Start(1));

        Assert.Single(updates);
        Assert.Equal(150.0, updates[0].Value, 10);
    }

    [Fact]
    public void BasketSpotGetsDisplayWeightsSortedByName()
    {
        var quotes = new[]
        {
            new SimulatedSpot("B", 200.0, 0.0, 0.0),
            new SimulatedSpot("A", 100.0, 0.0, 0.0)
        };

        var basket = new BasketSpot(quotes);

        Assert.Contains("A=0.5", basket.GetWeights());
        Assert.Contains("B=0.5", basket.GetWeights());
    }
}
