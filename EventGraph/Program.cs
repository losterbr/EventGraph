using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventGraph
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var options = AppOptionsParser.Parse(args);

            if (options.ShowHelp)
            {
                PrintUsage();
                return;
            }

            var quotes = options.Symbols
                .Select((symbol, index) => new SimulatedSpot(symbol, 800.0 + index * 100.0, 0.2 + index * 0.05, 3.0 + index))
                .ToList();

            var listener = new Listener(options.Quiet, options.BasketColor);
            quotes.ForEach(listener.Subscribe);

            var basketQuote = new BasketSpot(quotes);
            listener.Subscribe(basketQuote);

            var tasks = quotes.Select(quote => quote.Start(options.TickCount)).ToArray();
            await Task.WhenAll(tasks);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: dotnet run --project EventGraph/EventGraph.csproj -- [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --ticks <n>      Number of ticks each simulated spot emits (default: 5)");
            Console.WriteLine("  --quiet         Suppress subscription and quote output");
            Console.WriteLine("  --symbols A,B,C Comma-separated list of symbols to simulate");
            Console.WriteLine("  --basket-color <color>  Console color for basket updates (default: Cyan)");
            Console.WriteLine("  --help          Show this help message");
        }
    }
}
