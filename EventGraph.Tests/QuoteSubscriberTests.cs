using System;
using System.IO;
using System.Threading.Tasks;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class QuoteSubscriberTests
{
    [Fact]
    public async Task QuoteSubscriberSubscribesToSourceAndWritesOutput()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(output);
            var subscriber = new QuoteSubscriber();
            var source = new SimulatedQuoteSource("XYZ", 100.0, 0.0, 0.0);

            subscriber.Subscribe(source);
            await source.Start(1);

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
    public async Task QuoteSubscriberPadsShortNodeIdentifiersToTwentyCharacters()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(output);
            var subscriber = new QuoteSubscriber();
            var source = new SimulatedQuoteSource("XYZ", 100.0, 0.0, 0.0);

            subscriber.Subscribe(source);
            await source.Start(1);

            var updateLine = output.ToString().Split(Environment.NewLine)[1];
            var identifier = updateLine.Substring(updateLine.IndexOf(']') + 2, 20);

            Assert.Equal(20, identifier.Length);
            Assert.Equal("Quote XYZ           ", identifier);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task QuoteSubscriberTruncatesLongBasketIdentifiersToTwentyCharacters()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(output);
            var subscriber = new QuoteSubscriber();
            var sources = new[]
            {
                new SimulatedQuoteSource("TSLA", 100.0, 0.0, 0.0),
                new SimulatedQuoteSource("GOOG", 200.0, 0.0, 0.0),
                new SimulatedQuoteSource("AMZN", 300.0, 0.0, 0.0),
                new SimulatedQuoteSource("MSFT", 400.0, 0.0, 0.0)
            };
            var basket = new BasketAggregate(sources);

            subscriber.Subscribe(basket);
            await basket.RunOnceAsync();

            var updateLine = output.ToString().Split(Environment.NewLine)[1];
            var identifier = updateLine.Substring(updateLine.IndexOf(']') + 2, 20);

            Assert.Equal(20, identifier.Length);
            Assert.Equal("B TSLA,GOOG,AMZN,...", identifier);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task QuoteSubscriberCanSubscribeToAggregateWithCustomColor()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;
        var originalColor = Console.ForegroundColor;

        try
        {
            Console.SetOut(output);
            var subscriber = new QuoteSubscriber(quiet: false);
            var sources = new[]
            {
                new SimulatedQuoteSource("A", 100.0, 0.0, 0.0),
                new SimulatedQuoteSource("B", 200.0, 0.0, 0.0)
            };
            var basket = new BasketAggregate(sources, color: ConsoleColor.Red);

            subscriber.Subscribe(basket);
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
    public async Task QuoteSubscriberDoesNotWriteWhenQuietModeIsEnabled()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(output);
            var subscriber = new QuoteSubscriber(quiet: true);
            var source = new SimulatedQuoteSource("XYZ", 100.0, 0.0, 0.0);

            subscriber.Subscribe(source);
            source.Tick += (_, _) => { };
            await source.Start(1);

            Assert.Empty(output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
