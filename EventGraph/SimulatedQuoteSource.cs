using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Simulates a market quote and raises tick events over time.
    /// </summary>
    public class SimulatedQuoteSource : IQuoteNode
    {
        public event EventHandler<QuoteTick> Tick;

        private const double MilliSecondsPerYear = 365.25 * 24.0 * 60.0 * 60.0 * 1000.0;

        private readonly string name;
        private readonly double vol;
        private readonly double meanTickTimeSeconds;
        private double currentValue;

        public SimulatedQuoteSource(IReadOnlyDictionary<string, JsonElement> definition)
            : this(
                GetString(definition, "name"),
                GetDouble(definition, "spot"),
                GetDouble(definition, "volatility"),
                GetDouble(definition, "meanTickTimeSeconds"))
        {
        }

        public SimulatedQuoteSource(string name, double spot, double vol, double meanTickTimeSeconds = 1.0)
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

            this.name = name;
            this.currentValue = spot;
            this.vol = vol;
            this.meanTickTimeSeconds = meanTickTimeSeconds;
        }

        public string Name => name;

        public string Type => "SimulatedSpot";

        public double CurrentValue => currentValue;

        public IReadOnlyList<IQuoteNode> Dependencies => Array.Empty<IQuoteNode>();

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            if (definition == null || !definition.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString()))
            {
                throw new InvalidDataException($"SimulatedQuoteSource requires a non-empty '{propertyName}' property.");
            }

            return property.GetString();
        }

        private static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            if (definition == null || !definition.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.Number || !property.TryGetDouble(out var value))
            {
                throw new InvalidDataException($"SimulatedQuoteSource requires a numeric '{propertyName}' property.");
            }

            return value;
        }

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
            var poissonLambda = Math.Max(0.0, meanTickTimeSeconds * 1000.0);
            var poissonDist = poissonLambda <= 0.0
                ? null
                : new MathNet.Numerics.Distributions.Poisson(poissonLambda);
            var normalDist = new MathNet.Numerics.Distributions.Normal(0.0, 1.0);
            var quoteTick = new QuoteTick(name, currentValue);

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
                    quoteTick.Value *= Math.Exp(stdDev * normalDist.Sample() + logDriftAdjustment);
                    currentValue = quoteTick.Value;
                    Sleep(timeStepMilliSeconds);
                    Tick?.Invoke(this, quoteTick);
                    emittedTicks++;

                    if (tickCount == 0)
                    {
                        try
                        {
                            if (Console.KeyAvailable)
                            {
                                break;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Running outside an interactive console (for example in tests) should not crash.
                        }
                    }
                }
            }, cancellationToken);
        }
    }
}
