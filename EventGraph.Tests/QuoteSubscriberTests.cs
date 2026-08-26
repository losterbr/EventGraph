using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EventGraph;
using Xunit;

namespace EventGraph.Tests;

public class QuoteSubscriberTests
{
    private static string[] GetOutputLines(StringWriter output)
    {
        return output.ToString()
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string GetIdentifier(string line)
    {
        var timestampEnd = line.IndexOf(']');
        Assert.True(timestampEnd >= 0, $"Expected timestamp prefix in line: '{line}'");

        var contentStart = timestampEnd + 2;
        var contentEnd = line.IndexOf(" updated to", contentStart, StringComparison.Ordinal);
        Assert.True(contentEnd > contentStart, $"Expected update marker in line: '{line}'");

        return line.Substring(contentStart, contentEnd - contentStart);
    }

    [Fact]
    public async Task QuoteSubscriberSubscribesAndFormatsIdentifiers()
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
            Assert.Contains("SimulatedSpot::XYZ", rendered);

            var sourceLine = GetOutputLines(output)
                .Single(line => line.Contains("SimulatedSpot::XYZ") && line.Contains("updated to"));
            var sourceIdentifier = GetIdentifier(sourceLine);

            Assert.Equal(40, sourceIdentifier.Length);
            Assert.StartsWith("SimulatedSpot::XYZ", sourceIdentifier);

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

            var basketLine = GetOutputLines(output)
                .Single(line => line.Contains("CalculatedBasket::B TSLA,GOOG,AMZN,MSFT") && line.Contains("updated to"));
            var basketIdentifier = GetIdentifier(basketLine);

            Assert.Equal(40, basketIdentifier.Length);
            Assert.StartsWith("CalculatedBasket::B TSLA,GOOG,AMZN,MSFT", basketIdentifier);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task QuoteSubscriberKeepsConcurrentUpdatesOnSeparateLines()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(output);
            var subscriber = new QuoteSubscriber();
            var sources = new[]
            {
                new SimulatedQuoteSource("A", 100.0, 0.0, 0.0),
                new SimulatedQuoteSource("B", 200.0, 0.0, 0.0),
                new SimulatedQuoteSource("C", 300.0, 0.0, 0.0)
            };

            foreach (var source in sources)
            {
                subscriber.Subscribe(source);
            }

            await Task.WhenAll(sources.Select(source => source.Start(1)));

            var updateLines = GetOutputLines(output)
                .Where(line => line.Contains("SimulatedSpot::A") || line.Contains("SimulatedSpot::B") || line.Contains("SimulatedSpot::C"))
                .ToArray();

            Assert.Equal(3, updateLines.Length);
            Assert.All(updateLines, line => Assert.StartsWith("[", line));
            Assert.All(updateLines, line => Assert.Contains("updated to", line));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task QuoteSubscriberLogsConstituentBeforeBasketUpdate()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(output);
            var subscriber = new QuoteSubscriber();
            var sources = new[]
            {
                new SimulatedQuoteSource("A", 100.0, 0.0, 0.0),
                new SimulatedQuoteSource("B", 200.0, 0.0, 0.0)
            };

            subscriber.Subscribe(sources[0]);
            subscriber.Subscribe(sources[1]);
            var basket = new BasketAggregate(sources);
            subscriber.Subscribe(basket);

            await sources[0].Start(1);
            await sources[1].Start(1);

            var lines = GetOutputLines(output);
            var sourceIndex = Array.FindIndex(lines, line => line.Contains("SimulatedSpot::B") && line.Contains("updated to"));
            var basketIndex = Array.FindIndex(lines, line => line.Contains("CalculatedBasket::B A,B") && line.Contains("updated to"));

            Assert.True(sourceIndex >= 0);
            Assert.True(basketIndex > sourceIndex);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task QuoteSubscriberHandlesQuietAndCustomColorModes()
    {
        var output = new StringWriter();
        var originalOut = Console.Out;
        var originalColor = Console.ForegroundColor;

        try
        {
            Console.SetOut(output);
            var quietSubscriber = new QuoteSubscriber(quiet: true);
            var source = new SimulatedQuoteSource("XYZ", 100.0, 0.0, 0.0);

            quietSubscriber.Subscribe(source);
            await source.Start(1);
            Assert.Empty(output.ToString());

            output.GetStringBuilder().Clear();

            var coloredSubscriber = new QuoteSubscriber(quiet: false, basketColor: ConsoleColor.Red);
            var sources = new[]
            {
                new SimulatedQuoteSource("A", 100.0, 0.0, 0.0),
                new SimulatedQuoteSource("B", 200.0, 0.0, 0.0)
            };
            var basket = new BasketAggregate(sources);

            coloredSubscriber.Subscribe(basket);
            await basket.RunOnceAsync();

            Assert.Contains("Subscribed to B A,B", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.ForegroundColor = originalColor;
        }
    }
}
