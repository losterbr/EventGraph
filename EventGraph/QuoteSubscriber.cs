using System;
using System.Collections.Generic;

namespace EventGraph
{
    /// <summary>
    /// Subscribes to quote updates and prints them to the console.
    /// </summary>
    public class QuoteSubscriber(bool quiet = false, ConsoleColor? basketColor = null)
    {
        private const int NodeIdentifierWidth = 40;
        private static readonly object ConsoleLock = new();
        private static readonly ConsoleColor[] SourceColors =
        [
            ConsoleColor.DarkBlue,
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkRed,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkYellow,
            ConsoleColor.Blue,
            ConsoleColor.Green,
            ConsoleColor.Cyan,
            ConsoleColor.Red,
            ConsoleColor.Magenta,
            ConsoleColor.Yellow,
            ConsoleColor.Gray,
            ConsoleColor.DarkGray
        ];

        private readonly bool quiet = quiet;
        private readonly ConsoleColor? basketColorOverride = basketColor;
        private readonly Dictionary<IGraphNode, ConsoleColor> nodeColors = new(ReferenceEqualityComparer.Instance);
        private int nextSourceColor;
        private int nextBasketColor;

        public void Subscribe(SimulatedQuoteSource quote)
        {
            quote.Tick += SpotTicked;
            nodeColors[quote] = SourceColors[nextSourceColor++ % SourceColors.Length];
            if (!quiet)
            {
                lock (ConsoleLock)
                {
                    WriteTimestamp();
                    Console.ForegroundColor = nodeColors[quote];
                    Console.WriteLine($" Subscribed to {quote.Name}");
                    Console.ResetColor();
                }
            }
        }

        public void Subscribe(BasketAggregate quote)
        {
            quote.Tick += SpotTicked;
            nodeColors[quote] = basketColorOverride ?? SourceColors[nextBasketColor++ % SourceColors.Length];
            if (!quiet)
            {
                lock (ConsoleLock)
                {
                    WriteTimestamp();
                    Console.ForegroundColor = nodeColors[quote];
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
                    var node = (IGraphNode)sender;
                    Console.ForegroundColor = nodeColors[node];
                    Console.WriteLine($" {FormatNodeIdentifier($"{node.Type}::{node.Name}")} updated to {e.Value:0.##}");
                    Console.ResetColor();
                }
            }
        }

        private static string FormatNodeIdentifier(string identifier)
        {
            return identifier.Length > NodeIdentifierWidth
                ? identifier[..(NodeIdentifierWidth - 3)] + "..."
                : identifier.PadRight(NodeIdentifierWidth);
        }

        private static void WriteTimestamp()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now:HH:mm:ss.fff}]");
        }
    }
}
