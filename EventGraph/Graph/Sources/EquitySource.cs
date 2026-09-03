using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Provides an equity's spot and volatility source data.
    /// </summary>
    public class EquitySource : ISpotSourceNode, IVolSourceNode
    {
        public event EventHandler<QuoteTick> Tick;

        private const double MilliSecondsPerYear = 365.25 * 24.0 * 60.0 * 60.0 * 1000.0;
        private readonly double meanTickTimeSeconds;

        public EquitySource(IReadOnlyDictionary<string, JsonElement> definition)
            : this(
                GetString(definition, "name"),
                GetDouble(definition, "spot"),
                GetDouble(definition, "volatility"),
                GetDouble(definition, "meanTickTimeSeconds"),
                GetStringOrDefault(definition, "currency", "USD"))
        {
        }

        public EquitySource(
            string name,
            double spot,
            double vol,
            double meanTickTimeSeconds = 1.0,
            string currency = "USD")
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Spot name cannot be empty.", nameof(name));
            }

            if (double.IsNaN(spot) || double.IsInfinity(spot))
            {
                throw new ArgumentOutOfRangeException(nameof(spot), "Spot must be a finite number.");
            }

            if (vol < 0.0 || double.IsNaN(vol) || double.IsInfinity(vol))
            {
                throw new ArgumentOutOfRangeException(nameof(vol), "Volatility must be a non-negative finite number.");
            }

            if (meanTickTimeSeconds < 0.0 || double.IsNaN(meanTickTimeSeconds) || double.IsInfinity(meanTickTimeSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(meanTickTimeSeconds), "Mean tick time must be a non-negative finite number.");
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Currency cannot be empty.", nameof(currency));
            }

            Name = name;
            Spot = spot;
            Volatility = vol;
            Currency = currency;
            this.meanTickTimeSeconds = meanTickTimeSeconds;
        }

        public string Name { get; }

        public virtual string Type => nameof(EquitySource);

        public double Spot { get; private set; }

        public string Currency { get; }

        public double Volatility { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [];

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(EquitySource));
        }

        private static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetDouble(definition, propertyName, nameof(EquitySource));
        }

        private static string GetStringOrDefault(
            IReadOnlyDictionary<string, JsonElement> definition,
            string propertyName,
            string defaultValue)
        {
            return JsonDefinitionReader.GetStringOrDefault(definition, propertyName, defaultValue, nameof(EquitySource));
        }

        private double IncrStdDev(double tMilliSeconds)
        {
            return Volatility * Math.Sqrt(tMilliSeconds / MilliSecondsPerYear);
        }

        private static void Sleep(double tMilliSeconds)
        {
            Thread.Sleep((int)tMilliSeconds);
        }

        public async Task Start(int tickCount = 1, CancellationToken cancellationToken = default)
        {
            var poissonLambda = Math.Max(0.0, meanTickTimeSeconds * 1000.0);
            var poissonDist = poissonLambda <= 0.0
                ? null
                : new MathNet.Numerics.Distributions.Poisson(poissonLambda);
            var normalDist = new MathNet.Numerics.Distributions.Normal(0.0, 1.0);
            var quoteTick = new QuoteTick(Name, Spot);

            await Task.Run(() =>
            {
                Tick?.Invoke(this, quoteTick);

                int emittedTicks = 1;
                while (!cancellationToken.IsCancellationRequested && (tickCount <= 0 || emittedTicks < tickCount))
                {
                    double timeStepMilliSeconds = poissonDist?.Sample() ?? 0.0;
                    double stdDev = IncrStdDev(timeStepMilliSeconds);
                    double logDriftAdjustment = -0.5 * stdDev * stdDev;

                    // This is the standard lognormal/Itô adjustment for a GBM-style spot process.
                    // A separate convexity adjustment would be applied at the payoff/forward level,
                    // not as an extra term in the underlying spot simulation itself.
                    quoteTick.Value *= Math.Exp((stdDev * normalDist.Sample()) + logDriftAdjustment);
                    Spot = quoteTick.Value;
                    Sleep(timeStepMilliSeconds);
                    Tick?.Invoke(this, quoteTick);
                    emittedTicks++;

                    if (tickCount == 0)
                    {
                        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                        static bool IsKeyAvailable()
                        {
                            try
                            {
                                return Console.KeyAvailable;
                            }
                            catch (InvalidOperationException)
                            {
                                // Running outside an interactive console (for example in tests) should not crash.
                                return false;
                            }
                        }

                        if (IsKeyAvailable())
                        {
                            break;
                        }
                    }
                }
            }, cancellationToken);
        }
    }
}
