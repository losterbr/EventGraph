using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace EventGraph
{
    class Program
    {
        static void Main()
        {
            var quotes = new List<SimulatedSpot>
            {
                new SimulatedSpot("TSLA", 800.0, 0.3, 5.0),
                new SimulatedSpot("GOOG", 2800.0, 0.2, 10.0),
                new SimulatedSpot("AMZN", 3320.0, 0.2, 3.0)
            };
            
            Listener listener = new();

            quotes.ForEach(quote => listener.Subscribe(quote));

            BasketSpot basketQuote = new(quotes);
            listener.Subscribe(basketQuote);

            var tasks = quotes.Select(quote => quote.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
