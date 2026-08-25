using System;

namespace EventGraph
{
    /// <summary>
    /// Subscribes to quote updates and prints them to the console.
    /// </summary>
    public class QuoteSubscriber
    {
        private readonly bool quiet;
        private readonly ConsoleColor basketColor;

        public QuoteSubscriber(bool quiet = false, ConsoleColor basketColor = ConsoleColor.Cyan)
        {
            this.quiet = quiet;
            this.basketColor = basketColor;
        }

        public void Subscribe(SimulatedQuoteSource quote)
        {
            quote.Tick += SpotTicked;
            if (!quiet)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Subscribed to {quote.Name}");
            }
        }

        public void Subscribe(BasketAggregate quote)
        {
            quote.Tick += SpotTicked;
            if (!quiet)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Subscribed to Basket({quote.Name})");
            }
        }

        private void SpotTicked(object sender, QuoteTick e)
        {
            if (!quiet)
            {
                var isBasketUpdate = sender is BasketAggregate;
                if (isBasketUpdate)
                {
                    Console.ForegroundColor = basketColor;
                    var basket = (BasketAggregate)sender;
                    var weights = basket.GetWeights();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] B {e.Name}={e.Value:0.##} [{weights}]");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Quote {e.Name, -10} updated to {e.Value:0.##}");
            }
        }
    }
}
