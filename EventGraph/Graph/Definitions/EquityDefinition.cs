using System;

namespace EventGraph
{
    /// <summary>
    /// Defines immutable inputs for an equity asset.
    /// </summary>
    public sealed class EquityDefinition
    {
        public EquityDefinition(string name, double spot, double volatility, double meanTickTimeSeconds = 1.0, string currency = "USD")
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Equity name cannot be empty.", nameof(name));
            }

            if (double.IsNaN(spot) || double.IsInfinity(spot))
            {
                throw new ArgumentOutOfRangeException(nameof(spot), "Spot must be a finite number.");
            }

            if (volatility < 0.0 || double.IsNaN(volatility) || double.IsInfinity(volatility))
            {
                throw new ArgumentOutOfRangeException(nameof(volatility), "Volatility must be a non-negative finite number.");
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
            Volatility = volatility;
            MeanTickTimeSeconds = meanTickTimeSeconds;
            Currency = currency;
        }

        public string Name { get; }

        public double Spot { get; }

        public double Volatility { get; }

        public double MeanTickTimeSeconds { get; }

        public string Currency { get; }
    }
}
