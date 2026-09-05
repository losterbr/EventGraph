using System;
using System.Collections.Generic;
using System.Linq;

namespace EventGraph
{
    /// <summary>
    /// Defines immutable constituent names and weights for a basket.
    /// </summary>
    public sealed class BasketDefinition
    {
        private const double Epsilon = 1e-9;

        public BasketDefinition(string name, IReadOnlyList<string> constituents, IReadOnlyList<double> weights)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Basket name cannot be empty.", nameof(name));
            }

            if (constituents == null || constituents.Count == 0)
            {
                throw new ArgumentException("Basket must have at least one constituent.", nameof(constituents));
            }

            if (constituents.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Basket constituent names cannot be empty.", nameof(constituents));
            }

            if (weights == null || weights.Count != constituents.Count)
            {
                throw new ArgumentException("The number of weights must match the number of constituents.", nameof(weights));
            }

            if (weights.Any(weight => double.IsNaN(weight) || double.IsInfinity(weight)) || Math.Abs(weights.Sum() - 1.0) > Epsilon)
            {
                throw new ArgumentException("The sum of constituent weights must be 1 within epsilon.", nameof(weights));
            }

            Name = name;
            Constituents = [.. constituents];
            Weights = [.. weights];
        }

        public string Name { get; }

        public IReadOnlyList<string> Constituents { get; }

        public IReadOnlyList<double> Weights { get; }
    }
}