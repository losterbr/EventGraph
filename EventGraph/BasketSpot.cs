using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;


namespace EventGraph
{
    public class BasketSpot
    {
        
        public event TickHandler Tick;
        public delegate void TickHandler(BasketSpot q, SpotMessage s);

        public BasketSpot(List<SimulatedSpot> constituents)
        {
            spots = new();
            numConstituents = constituents.Count;
            name = constituents.Select(x => x.Name).Aggregate((a, b) => a + "_" + b);
            foreach (SimulatedSpot constituent in constituents)
            {
                constituent.Tick += new SimulatedSpot.TickHandler(SpotTicked);
            }
        }

        private Dictionary<string, double> spots;
        private readonly int numConstituents;
        private readonly string name;

        public string Name
        {
            get { return name; }
        }

        private bool AllSpotsAvailable()
        {
            lock (this.spots) { 
                return spots.Count == numConstituents;
            }
        }

        private double Spot()
        {
            double weight = 1.0/((double) numConstituents);
            lock ( this.spots) { 
                return spots.Select(x => weight * x.Value).Sum();
            }
        }

        private void SpotTicked(object sender, SpotMessage e)
        {
            lock (this.spots) {                
                spots[e.Name] = e.Value;
                if (AllSpotsAvailable())
                {
                    double spot = Spot();
                    SpotMessage spotMessage = new(name,spot);
                    Tick(this, spotMessage);
                }
            }
        }
    }
}
