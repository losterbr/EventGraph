using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventGraph
{
    public class SimulatedSpot
    {
        public event TickHandler Tick;
        public delegate void TickHandler(SimulatedSpot q, SpotMessage s);

        private const double MilliSecondsPerYear = 365.25 * 24.0 * 60.0 * 60.0 * 1000.0;

        private readonly string name;
        private readonly double vol;
        private readonly double meanTickTimeSeconds;
        private double currentValue;

        public SimulatedSpot(string name, double spot, double vol, double meanTickTimeSeconds = 1.0)
        {
            this.name = name;
            this.currentValue = spot;
            this.vol = vol;
            this.meanTickTimeSeconds = meanTickTimeSeconds;
        }

        public string Name => name;

        public double CurrentValue => currentValue;

        private double IncrStdDev(double tMilliSeconds)
        {
            return vol * Math.Sqrt(tMilliSeconds / MilliSecondsPerYear);
        }

        private static void Sleep(double tMilliSeconds)
        {
            Thread.Sleep((int)tMilliSeconds);
        }

        public async Task Start(int tickCount = 1, CancellationToken cancellationToken = default)
        {
            var poissonDist = new MathNet.Numerics.Distributions.Poisson(meanTickTimeSeconds * 1000.0);
            var normalDist = new MathNet.Numerics.Distributions.Normal(0.0, 1.0);
            var spotMessage = new SpotMessage(name, currentValue);

            await Task.Run(() =>
            {
                Tick?.Invoke(this, spotMessage);

                int emittedTicks = 1;
                while (!cancellationToken.IsCancellationRequested && (tickCount <= 0 || emittedTicks < tickCount))
                {
                    double timeStepMilliSeconds = poissonDist.Sample();
                    double stdDev = IncrStdDev(timeStepMilliSeconds);
                    double logDriftAdjustment = -0.5 * stdDev * stdDev;

                    // This is the standard lognormal/Itô adjustment for a GBM-style spot process.
                    // A separate convexity adjustment would be applied at the payoff/forward level,
                    // not as an extra term in the underlying spot simulation itself.
                    spotMessage.Value *= Math.Exp(stdDev * normalDist.Sample() + logDriftAdjustment);
                    currentValue = spotMessage.Value;
                    Sleep(timeStepMilliSeconds);
                    Tick?.Invoke(this, spotMessage);
                    emittedTicks++;
                }
            }, cancellationToken);
        }
    }
}
