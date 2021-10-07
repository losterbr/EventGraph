using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventGraph
{
    public class SimulatedSpot
    {
        public event TickHandler Tick;
        public delegate void TickHandler(SimulatedSpot q, SpotMessage s);

        private readonly string name;
        private readonly double spot;
        private readonly double vol;
        private readonly double meanTickTimeSeconds;

        private const double MilliSecondsPerYear = 365.25 * 24.0 * 60.0 * 60.0 * 1000.0;

        public string Name
        {
            get { return name; }
        }

        public SimulatedSpot(string name, double spot, double vol, double meanTickTimeSeconds = 1.0)
        { this.name = name;  this.spot = spot; this.vol = vol;  this.meanTickTimeSeconds = meanTickTimeSeconds; }

        private double IncrStdDev(double tMilliSeconds) 
        {
            return vol * Math.Sqrt(tMilliSeconds / MilliSecondsPerYear);
        }

        private static void Sleep(double tMilliSeconds)
        {
            Thread.Sleep((int)tMilliSeconds);
        }

        public async Task Start()
        {
            MathNet.Numerics.Distributions.Poisson poissonDist = new(meanTickTimeSeconds * 1000.0);
            MathNet.Numerics.Distributions.Normal normalDist = new(0.0, 1.0);

            var spotMessage = new SpotMessage(this.name, this.spot);
            await Task.Run(() =>
                {
                    Tick(this, spotMessage);
                    double timeStepMilliSeconds = poissonDist.Sample();
                    double stdDev = 0.0;
                    while (true)
                    {
                        timeStepMilliSeconds = poissonDist.Sample();
                        stdDev = IncrStdDev(timeStepMilliSeconds);
                        spotMessage.Value *= Math.Exp(stdDev * normalDist.Sample() - 0.5 * stdDev*stdDev);
                        Sleep(timeStepMilliSeconds);
                        Tick(this, spotMessage);
                    }
                }
                );
        }
    }
}
