using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class BasketAggregateTests
{
    [Fact]
    public async Task BasketAggregateRaisesUpdateAfterConstituentsPublish()
    {
        var quotes = new[]
        {
            new SimulatedQuoteSource("A", 100.0, 0.0, 0.0),
            new SimulatedQuoteSource("B", 200.0, 0.0, 0.0)
        };

        var basket = new BasketAggregate(quotes);
        var updates = new List<QuoteTick>();
        basket.Tick += (_, message) => updates.Add(message);

        await basket.RunOnceAsync();

        Assert.Single(updates);
        Assert.Equal(150.0, updates[0].Value, 10);
    }

    [Fact]
    public async Task BasketAggregateUsesProvidedWeights()
    {
        var quotes = new[]
        {
            new SimulatedQuoteSource("A", 100.0, 0.0, 0.0),
            new SimulatedQuoteSource("B", 200.0, 0.0, 0.0)
        };

        var basket = new BasketAggregate(quotes, new[] { 0.25, 0.75 });
        var updates = new List<QuoteTick>();
        basket.Tick += (_, message) => updates.Add(message);

        await basket.RunOnceAsync();

        Assert.Single(updates);
        Assert.Equal(175.0, updates[0].Value, 10);
    }

    [Fact]
    public async Task BasketAggregateSkipsZeroWeightConstituents()
    {
        var quotes = new[]
        {
            new SimulatedQuoteSource("A", 100.0, 0.0, 0.0),
            new SimulatedQuoteSource("B", 200.0, 0.0, 0.0)
        };

        var basket = new BasketAggregate(quotes, new[] { 0.0, 1.0 });
        var updates = new List<QuoteTick>();
        basket.Tick += (_, message) => updates.Add(message);

        await quotes[0].Start(1);

        Assert.Empty(updates);
    }

    [Fact]
    public void BasketAggregateThrowsWhenWeightsDoNotSumToOne()
    {
        var quotes = new[]
        {
            new SimulatedQuoteSource("A", 100.0, 0.0, 0.0),
            new SimulatedQuoteSource("B", 200.0, 0.0, 0.0)
        };

        Assert.Throws<ArgumentException>(() => new BasketAggregate(quotes, new[] { 0.6, 0.3 }));
    }

    [Fact]
    public void BasketAggregateThrowsWhenWeightCountDoesNotMatchConstituents()
    {
        var quotes = new[]
        {
            new SimulatedQuoteSource("A", 100.0, 0.0, 0.0),
            new SimulatedQuoteSource("B", 200.0, 0.0, 0.0)
        };

        Assert.Throws<ArgumentException>(() => new BasketAggregate(quotes, new[] { 0.5 }));
    }

    [Fact]
    public void BasketAggregateThrowsWhenConstituentsAreNull()
    {
        Assert.Throws<ArgumentException>(() => new BasketAggregate(null!));
    }

    [Fact]
    public void BasketAggregateThrowsWhenNoConstituentsAreProvided()
    {
        Assert.Throws<ArgumentException>(() => new BasketAggregate(Array.Empty<SimulatedQuoteSource>()));
    }

    [Fact]
    public void BasketAggregateRetainsItsColor()
    {
        var quotes = new[]
        {
            new SimulatedQuoteSource("A", 100.0, 0.0, 0.0)
        };

        var basket = new BasketAggregate(quotes, color: ConsoleColor.Red);

        Assert.Equal(ConsoleColor.Red, basket.Color);
    }

    [Fact]
    public void BasketAggregateHasTheExpectedType()
    {
        var quotes = new[]
        {
            new SimulatedQuoteSource("A", 100.0, 0.0, 0.0)
        };

        var basket = new BasketAggregate(quotes);

        Assert.Equal("CalculatedBasket", basket.Type);
    }

    [Fact]
    public async Task BasketAggregatePublishesAggregateOnlyWhenAllConstituentsAreAvailable()
    {
        var quotes = new[]
        {
            new SimulatedQuoteSource("A", 100.0, 0.0, 0.0),
            new SimulatedQuoteSource("B", 200.0, 0.0, 0.0)
        };

        var basket = new BasketAggregate(quotes);
        var updates = new List<QuoteTick>();
        basket.Tick += (_, message) => updates.Add(message);

        await quotes[0].Start(1);

        Assert.Empty(updates);
    }

    [Fact]
    public async Task BasketAggregatePublishesAggregateWhenAllConstituentEventsArrive()
    {
        var quotes = new[]
        {
            new SimulatedQuoteSource("A", 100.0, 0.0, 0.0),
            new SimulatedQuoteSource("B", 200.0, 0.0, 0.0)
        };

        var basket = new BasketAggregate(quotes);
        var updates = new List<QuoteTick>();
        basket.Tick += (_, message) => updates.Add(message);

        await Task.WhenAll(quotes[0].Start(1), quotes[1].Start(1));

        Assert.Single(updates);
        Assert.Equal(150.0, updates[0].Value, 10);
    }

    [Fact]
    public void BasketAggregateGetsDisplayWeightsSortedByName()
    {
        var quotes = new[]
        {
            new SimulatedQuoteSource("B", 200.0, 0.0, 0.0),
            new SimulatedQuoteSource("A", 100.0, 0.0, 0.0)
        };

        var basket = new BasketAggregate(quotes);

        Assert.Contains("A=0.5", basket.GetWeights());
        Assert.Contains("B=0.5", basket.GetWeights());
    }
}
