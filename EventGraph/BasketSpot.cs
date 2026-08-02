using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventGraph
{
    public class BasketSpot
    {
        public event TickHandler Tick;
        public delegate void TickHandler(BasketSpot q, SpotMessage s);

        private readonly IReadOnlyList<SimulatedSpot> constituents;
        private readonly Dictionary<string, double> spots = new();
        private readonly int numConstituents;
        private readonly string name;

        public BasketSpot(IReadOnlyList<SimulatedSpot> constituents)
        {
            this.constituents = constituents;
            numConstituents = constituents.Count;
            name = string.Join(",", constituents.Select(x => x.Name));

            foreach (var constituent in constituents)
            {
                constituent.Tick += SpotTicked;
            }
        }

        public string Name => name;

        private bool AllSpotsAvailable()
        {
            lock (spots)
            {
                return spots.Count == numConstituents;
            }
        }

        private double Spot()
        {
            double weight = 1.0 / numConstituents;
            lock (spots)
            {
                return spots.Values.Sum(x => weight * x);
            }
        }

        private void SpotTicked(object sender, SpotMessage e)
        {
            lock (spots)
            {
                spots[e.Name] = e.Value;
                if (AllSpotsAvailable())
                {
                    var spot = Spot();
                    var spotMessage = new SpotMessage(name, spot);
                    Tick?.Invoke(this, spotMessage);
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
                        spots[constituent.Name] = constituent.CurrentValue;
                    }

                    if (AllSpotsAvailable())
                    {
                        var spot = Spot();
                        var spotMessage = new SpotMessage(name, spot);
                        Tick?.Invoke(this, spotMessage);
                    }
                }
            });
        }
    }
}
