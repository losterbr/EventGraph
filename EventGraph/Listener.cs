using System;

namespace EventGraph
{
    public class Listener
    {
        private readonly bool quiet;

        public Listener(bool quiet = false)
        {
            this.quiet = quiet;
        }

        public void Subscribe(SimulatedSpot quote)
        {
            quote.Tick += SpotTicked;
            if (!quiet)
            {
                Console.WriteLine($"Subscribed to {quote.Name}");
            }
        }

        public void Subscribe(BasketSpot quote)
        {
            quote.Tick += SpotTicked;
            if (!quiet)
            {
                Console.WriteLine($"Subscribed to {quote.Name}");
            }
        }

        private void SpotTicked(object sender, SpotMessage e)
        {
            if (!quiet)
            {
                Console.WriteLine($"Quote {e.Name, -10} updated to {e.Value:0.##}");
            }
        }
    }
}
