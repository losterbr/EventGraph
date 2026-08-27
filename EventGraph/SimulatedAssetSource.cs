using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Simulates a market spot and volatility quote and raises tick events over time.
    /// </summary>
    public class SimulatedAssetSource : ISpotQuoteNode, IVolQuoteNode
    {
        public event EventHandler<QuoteTick> SpotTick;

        private const double MilliSecondsPerYear = 365.25 * 24.0 * 60.0 * 60.0 * 1000.0;
        private readonly double meanTickTimeSeconds;

        public SimulatedAssetSource(IReadOnlyDictionary<string, JsonElement> definition)
            : this(
                GetString(definition, "name"),
                GetDouble(definition, "spot"),
                GetDouble(definition, "volatility"),
                GetDouble(definition, "meanTickTimeSeconds"),
                GetStringOrDefault(definition, "currency", "USD"))
        {
        }

        public SimulatedAssetSource(
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

        public string Type => "SimulatedSpot";

        public double Spot { get; private set; }

        public string Currency { get; }

        public double Volatility { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [];

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return definition == null || !definition.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString())
                ? throw new InvalidDataException($"SimulatedAssetSource requires a non-empty '{propertyName}' property.")
                : property.GetString();
        }

        private static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return definition == null || !definition.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.Number || !property.TryGetDouble(out var value)
                ? throw new InvalidDataException($"SimulatedAssetSource requires a numeric '{propertyName}' property.")
                : value;
        }

        private static string GetStringOrDefault(
            IReadOnlyDictionary<string, JsonElement> definition,
            string propertyName,
            string defaultValue)
        {
            return definition == null || !definition.ContainsKey(propertyName)
                ? defaultValue
                : GetString(definition, propertyName);
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
                SpotTick?.Invoke(this, quoteTick);

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
                    SpotTick?.Invoke(this, quoteTick);
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
