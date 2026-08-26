using System;
using System.IO;
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

            var definitionDirectory = Path.Combine(AppContext.BaseDirectory, "graph definition");
            var nodes = GraphDefinitionLoader.LoadNodes(definitionDirectory);
            var quotes = nodes.OfType<SimulatedQuoteSource>().ToList();
            var baskets = nodes.OfType<BasketAggregate>().ToList();

            var listener = new QuoteSubscriber(options.Quiet, options.BasketColor);
            foreach (var quote in quotes)
            {
                listener.Subscribe(quote);
            }

            foreach (var basket in baskets)
            {
                basket.Connect();
            }

            GraphValidator.EnsureAcyclic(nodes);
            foreach (var basket in baskets)
            {
                listener.Subscribe(basket);
            }

            var tasks = quotes.Select(quote => quote.Start(options.TickCount)).ToArray();
            await Task.WhenAll(tasks);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: dotnet run --project EventGraph/EventGraph.csproj -- [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --ticks <n>      Number of ticks each simulated spot emits (default: continuous until interrupted)");
            Console.WriteLine("  --quiet         Suppress subscription and quote output");
            Console.WriteLine("  --basket-color <color>  Console color for basket updates (default: Cyan)");
            Console.WriteLine("  --help          Show this help message");
        }
    }
}
