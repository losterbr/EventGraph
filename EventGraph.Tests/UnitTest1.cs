using System;
using System.IO;
using System.Reflection;
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

    [Fact]
    public void ParseArgumentsSupportsCustomBasketColor()
    {
        var options = AppOptionsParser.Parse(new[] { "--basket-color", "Yellow" });

        Assert.Equal(ConsoleColor.Yellow, options.BasketColor);
    }

    [Fact]
    public async Task ListenerSubscribesToSpotAndWritesOutput()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(output);
            var listener = new Listener();
            var spot = new SimulatedSpot("XYZ", 100.0, 0.0, 0.0);

            listener.Subscribe(spot);
            await spot.Start(1);

            var rendered = output.ToString();
            Assert.Contains("Subscribed to XYZ", rendered);
            Assert.Contains("Quote XYZ", rendered);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task SimulatedSpotCanRunMultipleTicksAndContinuousMode()
    {
        var updates = new List<SpotMessage>();
        var spot = new SimulatedSpot("XYZ", 100.0, 0.2, 0.0);
        spot.Tick += (_, message) => updates.Add(message);

        await spot.Start(2);
        Assert.Equal(2, updates.Count);

        var continuousSpot = new SimulatedSpot("CONT", 50.0, 0.1, 0.0);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(10);
        await continuousSpot.Start(0, cts.Token);
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

    [Fact]
    public void SpotMessageRequiresAName()
    {
        Assert.Throws<ArgumentException>(() => new SpotMessage(" ", 42.0));
    }

    [Fact]
    public async Task ProgramMainRunsWithTickOptions()
    {
        var programType = typeof(AppOptions).Assembly.GetType("EventGraph.Program");
        var method = programType!.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);

        var task = (Task)method!.Invoke(null, new object[] { new[] { "--ticks", "1", "--quiet" } })!;
        await task;
    }

    [Fact]
    public async Task ProgramMainPrintsHelpWhenRequested()
    {
        var programType = typeof(AppOptions).Assembly.GetType("EventGraph.Program");
        var method = programType!.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);

        var task = (Task)method!.Invoke(null, new object[] { new[] { "--help" } })!;
        await task;
    }

    [Fact]
    public void ParseArgumentsDefaultsToContinuousRun()
    {
        var options = AppOptionsParser.Parse(Array.Empty<string>());

        Assert.Equal(0, options.TickCount);
    }
}