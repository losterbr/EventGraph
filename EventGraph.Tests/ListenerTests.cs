using System;
using System.IO;
using System.Threading.Tasks;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class ListenerTests
{
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
    public async Task ListenerCanSubscribeToBasketWithCustomColor()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;
        var originalColor = Console.ForegroundColor;

        try
        {
            Console.SetOut(output);
            var listener = new Listener(quiet: false, basketColor: ConsoleColor.Red);
            var quotes = new[]
            {
                new SimulatedSpot("A", 100.0, 0.0, 0.0),
                new SimulatedSpot("B", 200.0, 0.0, 0.0)
            };
            var basket = new BasketSpot(quotes);

            listener.Subscribe(basket);
            await basket.RunOnceAsync();

            Assert.Contains("Subscribed to Basket(A,B)", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.ForegroundColor = originalColor;
        }
    }

    [Fact]
    public async Task ListenerDoesNotWriteWhenQuietModeIsEnabled()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(output);
            var listener = new Listener(quiet: true);
            var spot = new SimulatedSpot("XYZ", 100.0, 0.0, 0.0);

            listener.Subscribe(spot);
            spot.Tick += (_, _) => { };
            await spot.Start(1);

            Assert.Empty(output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
