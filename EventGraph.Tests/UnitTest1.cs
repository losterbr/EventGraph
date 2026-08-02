using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class EventGraphTests
{
    [Fact]
    public async Task BasketSpotRaisesUpdateAfterConstituentsPublish()
    {
        var quotes = new[]
        {
            new SimulatedSpot("A", 100.0, 0.0, 0.0),
            new SimulatedSpot("B", 200.0, 0.0, 0.0)
        };

        var listener = new Listener();
        var basket = new BasketSpot(quotes.ToList());
        listener.Subscribe(basket);

        var updates = new List<SpotMessage>();
        basket.Tick += (_, message) => updates.Add(message);

        var task = basket.RunOnceAsync();
        await task;

        Assert.Single(updates);
        Assert.Equal(150.0, updates[0].Value, 10);
    }

    [Fact]
    public void ParseArgumentsSupportsCustomTickCount()
    {
        var options = AppOptionsParser.Parse(new[] { "--ticks", "12" });

        Assert.Equal(12, options.TickCount);
    }

    [Fact]
    public void ParseArgumentsSupportsQuietAndSymbols()
    {
        var options = AppOptionsParser.Parse(new[] { "--quiet", "--symbols", "A,B,C" });

        Assert.True(options.Quiet);
        Assert.Equal(new[] { "A", "B", "C" }, options.Symbols);
    }
}