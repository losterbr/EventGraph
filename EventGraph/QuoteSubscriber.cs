using System;

namespace EventGraph
{
    /// <summary>
    /// Subscribes to quote updates and prints them to the console.
    /// </summary>
    public class QuoteSubscriber
    {
        private const int NodeIdentifierWidth = 20;
        private static readonly object ConsoleLock = new();

        private readonly bool quiet;
        public QuoteSubscriber(bool quiet = false)
        {
            this.quiet = quiet;
        }

        public void Subscribe(SimulatedQuoteSource quote)
        {
            quote.Tick += SpotTicked;
            if (!quiet)
            {
                lock (ConsoleLock)
                {
                    WriteTimestamp();
                    Console.ResetColor();
                    Console.WriteLine($" Subscribed to {quote.Name}");
                }
            }
        }

        public void Subscribe(BasketAggregate quote)
        {
            quote.Tick += SpotTicked;
            if (!quiet)
            {
                lock (ConsoleLock)
                {
                    WriteTimestamp();
                    Console.ResetColor();
                    Console.WriteLine($" Subscribed to Basket({quote.Name})");
                }
            }
        }

        private void SpotTicked(object sender, QuoteTick e)
        {
            if (!quiet)
            {
                lock (ConsoleLock)
                {
                    var isBasketUpdate = sender is BasketAggregate;
                    if (isBasketUpdate)
                    {
                        WriteTimestamp();
                        var basket = (BasketAggregate)sender;
                        Console.ForegroundColor = basket.Color;
                        var weights = basket.GetWeights();
                        Console.WriteLine($" {FormatNodeIdentifier($"B {e.Name}")}={e.Value:0.##} [{weights}]");
                        Console.ResetColor();
                        return;
                    }

                    WriteTimestamp();
                    Console.ForegroundColor = ((IQuoteNode)sender).Color;
                    Console.WriteLine($" {FormatNodeIdentifier($"Quote {e.Name}")} updated to {e.Value:0.##}");
                    Console.ResetColor();
                }
            }
        }

        private static string FormatNodeIdentifier(string identifier)
        {
            if (identifier.Length > NodeIdentifierWidth)
            {
                return identifier[..(NodeIdentifierWidth - 3)] + "...";
            }

            return identifier.PadRight(NodeIdentifierWidth);
        }

        private static void WriteTimestamp()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now:HH:mm:ss.fff}]");
        }
    }
}
