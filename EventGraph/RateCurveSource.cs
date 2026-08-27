using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Provides a continuously compounded flat interest-rate curve.
    /// </summary>
    public sealed class RateCurveSource : IRateCurveNode
    {
        public RateCurveSource(IReadOnlyDictionary<string, JsonElement> definition)
            : this(
                GetString(definition, "name"),
                GetDouble(definition, "interestRate"),
                GetStringOrDefault(definition, "currency", "USD"))
        {
        }

        public RateCurveSource(string name, double interestRate, string currency = "USD")
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Rate curve name cannot be empty.", nameof(name));
            }

            if (double.IsNaN(interestRate) || double.IsInfinity(interestRate))
            {
                throw new ArgumentOutOfRangeException(nameof(interestRate), "Interest rate must be a finite number.");
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Currency cannot be empty.", nameof(currency));
            }

            Name = name;
            InterestRate = interestRate;
            Currency = currency;
            RateCurve = date => Math.Exp(InterestRate * (date - DateTime.Today).TotalDays / 365.0);
        }

        public string Name { get; }

        public string Type => nameof(RateCurveSource);

        public double InterestRate { get; }

        public string Currency { get; }

        public Func<DateTime, double> RateCurve { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [];

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return definition == null || !definition.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString())
                ? throw new InvalidDataException($"RateCurveSource requires a non-empty '{propertyName}' property.")
                : property.GetString();
        }

        private static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return definition == null || !definition.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.Number || !property.TryGetDouble(out var value)
                ? throw new InvalidDataException($"RateCurveSource requires a numeric '{propertyName}' property.")
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
    }
}