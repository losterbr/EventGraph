using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventGraph
{
    /// <summary>
    /// Aggregates multiple simulated quotes into a weighted basket value.
    /// </summary>
    public class BasketAggregate : IQuoteNode
    {
        public event EventHandler<QuoteTick> Tick;

        private const double Epsilon = 1e-9;

        private readonly IReadOnlyList<IQuoteNode> constituents;
        private readonly Dictionary<string, double> spots = new();
        private readonly Dictionary<string, double> weights = new();
        private readonly string name;

        public BasketAggregate(IReadOnlyList<IQuoteNode> constituents, IReadOnlyList<double> weights = null, ConsoleColor color = ConsoleColor.Cyan)
        {
            if (constituents == null || constituents.Count == 0)
            {
                throw new ArgumentException("Basket must have at least one constituent.", nameof(constituents));
            }

            this.constituents = constituents;
            name = string.Join(",", constituents.Select(x => x.Name));
            Color = color;

            if (weights != null)
            {
                if (weights.Count != constituents.Count)
                {
                    throw new ArgumentException("The number of weights must match the number of constituents.", nameof(weights));
                }

                var weightSum = weights.Sum();
                if (Math.Abs(weightSum - 1.0) > Epsilon)
                {
                    throw new ArgumentException("The sum of constituent weights must be 1 within epsilon.", nameof(weights));
                }

                for (int i = 0; i < constituents.Count; i++)
                {
                    if (Math.Abs(weights[i]) <= Epsilon)
                    {
                        continue;
                    }

                    this.weights[constituents[i].Name] = weights[i];
                    constituents[i].Tick += SpotTicked;
                }
            }
            else
            {
                foreach (var constituent in constituents)
                {
                    this.weights[constituent.Name] = 1.0 / constituents.Count;
                    constituent.Tick += SpotTicked;
                }
            }
        }

        public string Name => name;

        public ConsoleColor Color { get; }

        public double CurrentValue { get; private set; }

        public IReadOnlyList<IQuoteNode> Dependencies => constituents;

        public string GetWeights()
        {
            return string.Join(", ", weights.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value:0.###}"));
        }

        private bool AllSpotsAvailable()
        {
            lock (spots)
            {
                return spots.Count == weights.Count;
            }
        }

        private double Spot()
        {
            lock (spots)
            {
                return spots.Sum(x => weights[x.Key] * x.Value);
            }
        }

        private void SpotTicked(object sender, QuoteTick e)
        {
            lock (spots)
            {
                spots[e.Name] = e.Value;
                if (AllSpotsAvailable())
                {
                    var spot = Spot();
                    CurrentValue = spot;
                    var quoteTick = new QuoteTick(name, spot);
                    Tick?.Invoke(this, quoteTick);
                }
            }
        }

        public Task RunOnceAsync()
        {
            return Task.Run(() =>
            {
                lock (spots)
                {
                    foreach (var constituent in constituents)
                    {
                        if (weights.ContainsKey(constituent.Name))
                        {
                            spots[constituent.Name] = constituent.CurrentValue;
                        }
                    }

                    if (AllSpotsAvailable())
                    {
                        var spot = Spot();
                        CurrentValue = spot;
                        var quoteTick = new QuoteTick(name, spot);
                        Tick?.Invoke(this, quoteTick);
                    }
                }
            });
        }
    }
}
