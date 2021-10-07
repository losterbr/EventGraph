using System;
namespace EventGraph
{
    public class Listener
    {
        public void Subscribe(SimulatedSpot quote)
        {
            quote.Tick += new SimulatedSpot.TickHandler(SpotTicked);
            Console.WriteLine($"Subscribed to {quote.Name}");
        }

        public void Subscribe(BasketSpot quote)
        {
            quote.Tick += new BasketSpot.TickHandler(SpotTicked);
            Console.WriteLine($"Subscribed to {quote.Name}");
        }

        private void SpotTicked(object sender, SpotMessage e)
        {
            Console.WriteLine($"Quote {e.Name, -10} updated to {e.Value:0.##}");
        }
    }
}
