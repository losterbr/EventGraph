using System;

namespace EventGraph
{
    public class Listener
    {
        private readonly bool quiet;
        private readonly ConsoleColor basketColor;

        public Listener(bool quiet = false, ConsoleColor basketColor = ConsoleColor.Cyan)
        {
            this.quiet = quiet;
            this.basketColor = basketColor;
        }

        public void Subscribe(SimulatedSpot quote)
        {
            quote.Tick += SpotTicked;
            if (!quiet)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Subscribed to {quote.Name}");
            }
        }

        public void Subscribe(BasketSpot quote)
        {
            quote.Tick += SpotTicked;
            if (!quiet)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Subscribed to Basket({quote.Name})");
            }
        }

        private void SpotTicked(object sender, SpotMessage e)
        {
            if (!quiet)
            {
                var isBasketUpdate = sender is BasketSpot;
                if (isBasketUpdate)
                {
                    Console.ForegroundColor = basketColor;
                }

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Quote {e.Name, -10} updated to {e.Value:0.##}");

                if (isBasketUpdate)
                {
                    Console.ResetColor();
                }
            }
        }
    }
}
