using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Aggregates multiple simulated quotes into a weighted basket value.
    /// </summary>
    public class BasketAggregate : IQuoteNode
    {
        public event EventHandler<QuoteTick> Tick;

        private const double Epsilon = 1e-9;

        private readonly IReadOnlyList<IQuoteNode> constituents;
        private readonly Dictionary<string, double> spots = new();
        private readonly Dictionary<string, double> weights = new();
        private readonly string name;

        public BasketAggregate(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IQuoteNode> nodesByName)
            : this(
                GetString(definition, "name"),
                GetConstituents(definition, nodesByName),
                GetWeights(definition))
        {
        }

        public BasketAggregate(IReadOnlyList<IQuoteNode> constituents, IReadOnlyList<double> weights = null)
            : this(constituents == null ? null : $"B {string.Join(",", constituents.Select(x => x.Name))}", constituents, weights)
        {
        }

        public BasketAggregate(string name, IReadOnlyList<IQuoteNode> constituents, IReadOnlyList<double> weights = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Basket name cannot be empty.", nameof(name));
            }

            if (constituents == null || constituents.Count == 0)
            {
                throw new ArgumentException("Basket must have at least one constituent.", nameof(constituents));
            }

            this.constituents = constituents;
            this.name = name;

            if (weights != null)
            {
                if (weights.Count != constituents.Count)
                {
                    throw new ArgumentException("The number of weights must match the number of constituents.", nameof(weights));
                }

                var weightSum = weights.Sum();
                if (Math.Abs(weightSum - 1.0) > Epsilon)
                {
                    throw new ArgumentException("The sum of constituent weights must be 1 within epsilon.", nameof(weights));
                }

                for (int i = 0; i < constituents.Count; i++)
                {
                    if (Math.Abs(weights[i]) <= Epsilon)
                    {
                        continue;
                    }

                    this.weights[constituents[i].Name] = weights[i];
                    if (constituents[i] is BasketAggregate basket)
                    {
                        basket.Tick += SpotTicked;
                    }
                    else
                    {
                        constituents[i].Tick += SpotTicked;
                    }
                }
            }
            else
            {
                foreach (var constituent in constituents)
                {
                    this.weights[constituent.Name] = 1.0 / constituents.Count;
                    if (constituent is BasketAggregate basket)
                    {
                        basket.Tick += SpotTicked;
                    }
                    else
                    {
                        constituent.Tick += SpotTicked;
                    }
                }
            }
        }

        public string Name => name;

        public string Type => "CalculatedBasket";

        public double CurrentValue { get; private set; }

        public IReadOnlyList<IQuoteNode> Dependencies => constituents;

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            if (definition == null || !definition.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString()))
            {
                throw new InvalidDataException($"BasketAggregate requires a non-empty '{propertyName}' property.");
            }

            return property.GetString();
        }

        private static IReadOnlyList<IQuoteNode> GetConstituents(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IQuoteNode> nodesByName)
        {
            if (definition == null || !definition.TryGetValue("names", out var names) || names.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("BasketAggregate requires a names array.");
            }

            var constituents = new List<IQuoteNode>();
            foreach (var name in names.EnumerateArray())
            {
                if (name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString()) || !nodesByName.TryGetValue(name.GetString(), out var constituent))
                {
                    throw new InvalidDataException($"BasketAggregate references an unknown source '{name}'.");
                }

                constituents.Add(constituent);
            }

            return constituents;
        }

        private static IReadOnlyList<double> GetWeights(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (definition == null || !definition.TryGetValue("weights", out var weights) || weights.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("BasketAggregate requires a weights array.");
            }

            var values = new List<double>();
            foreach (var weight in weights.EnumerateArray())
            {
                if (weight.ValueKind != JsonValueKind.Number || !weight.TryGetDouble(out var value))
                {
                    throw new InvalidDataException("BasketAggregate weights must be numeric.");
                }

                values.Add(value);
            }

            return values;
        }

        public void Connect()
        {
            foreach (var constituent in constituents)
            {
                if (constituent is BasketAggregate basket)
                {
                    basket.Tick -= SpotTicked;
                    basket.Tick += SpotTicked;
                }
                else
                {
                    constituent.Tick -= SpotTicked;
                    if (weights.ContainsKey(constituent.Name))
                    {
                        constituent.Tick += SpotTicked;
                    }
                }
            }
        }

        public string GetWeights()
        {
            return string.Join(", ", weights.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value:0.###}"));
        }

        private bool AllSpotsAvailable()
        {
            lock (spots)
            {
                return spots.Count == weights.Count;
            }
        }

        private double Spot()
        {
            lock (spots)
            {
                return spots.Sum(x => weights[x.Key] * x.Value);
            }
        }

        private void SpotTicked(object sender, QuoteTick e)
        {
            lock (spots)
            {
                spots[e.Name] = e.Value;
                if (AllSpotsAvailable())
                {
                    var spot = Spot();
                    CurrentValue = spot;
                    var quoteTick = new QuoteTick(name, spot);
                    Tick?.Invoke(this, quoteTick);
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
                        if (weights.ContainsKey(constituent.Name))
                        {
                            spots[constituent.Name] = constituent.CurrentValue;
                        }
                    }

                    if (AllSpotsAvailable())
                    {
                        var spot = Spot();
                        CurrentValue = spot;
                        var quoteTick = new QuoteTick(name, spot);
                        Tick?.Invoke(this, quoteTick);
                    }
                }
            });
        }
    }
}
