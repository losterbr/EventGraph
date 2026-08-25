using System;

namespace EventGraph
{
    /// <summary>
    /// Subscribes to quote updates and prints them to the console.
    /// </summary>
    public class QuoteSubscriber
    {
        private const int NodeIdentifierWidth = 40;
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
                    Console.ForegroundColor = quote.Color;
                    Console.WriteLine($" Subscribed to {quote.Name}");
                    Console.ResetColor();
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
                    Console.ForegroundColor = quote.Color;
                    Console.WriteLine($" Subscribed to {quote.Name}");
                    Console.ResetColor();
                }
            }
        }

        private void SpotTicked(object sender, QuoteTick e)
        {
            if (!quiet)
            {
                lock (ConsoleLock)
                {
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
