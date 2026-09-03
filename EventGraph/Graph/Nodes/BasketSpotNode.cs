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
    public class BasketSpotNode : ISpotNode
    {
        public event EventHandler<QuoteTick> Tick;

        private const double Epsilon = 1e-9;
        private readonly Dictionary<ISpotNode, int[]> constituentIndicesByNode;
        private readonly IReadOnlyList<ISpotNode> constituents;
        private readonly bool[] hasLatestValue;
        private readonly double[] latestValues;
        private readonly object stateLock = new();
        private readonly double[] weights;
        private readonly int requiredConstituentCount;
        private int availableConstituentCount;

        public BasketSpotNode(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
            : this(
                GetString(definition, "name"),
                GetConstituents(definition, nodesByName),
                GetWeights(definition))
        {
        }

        public BasketSpotNode(IReadOnlyList<ISpotNode> constituents, IReadOnlyList<double> weights = null)
            : this(constituents == null ? null : $"B {string.Join(",", constituents.Select(x => x.Name))}", constituents, weights)
        {
        }

        public BasketSpotNode(string name, IReadOnlyList<ISpotNode> constituents, IReadOnlyList<double> weights = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Basket name cannot be empty.", nameof(name));
            }

            if (constituents == null || constituents.Count == 0)
            {
                throw new ArgumentException("Basket must have at least one constituent.", nameof(constituents));
            }

            this.constituents = [.. constituents];
            Name = name;
            Currency = this.constituents[0].Currency;
            if (this.constituents.Any(constituent => !string.Equals(constituent.Currency, Currency, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Basket constituents must use the same currency.", nameof(constituents));
            }

            hasLatestValue = new bool[this.constituents.Count];
            latestValues = new double[this.constituents.Count];
            this.weights = new double[this.constituents.Count];
            var activeIndicesByNode = new Dictionary<ISpotNode, List<int>>(ReferenceEqualityComparer.Instance);

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
                    this.weights[i] = weights[i];
                    if (Math.Abs(this.weights[i]) <= Epsilon)
                    {
                        continue;
                    }

                    AddActiveIndex(activeIndicesByNode, this.constituents[i], i);
                }
            }
            else
            {
                for (int i = 0; i < this.constituents.Count; i++)
                {
                    this.weights[i] = 1.0 / this.constituents.Count;
                    AddActiveIndex(activeIndicesByNode, this.constituents[i], i);
                }
            }

            constituentIndicesByNode = new Dictionary<ISpotNode, int[]>(ReferenceEqualityComparer.Instance);
            foreach (var pair in activeIndicesByNode)
            {
                constituentIndicesByNode[pair.Key] = [.. pair.Value];
            }

            requiredConstituentCount = constituentIndicesByNode.Values.Sum(indexes => indexes.Length);
        }

        public string Name { get; }

        public string Type => nameof(BasketSpotNode);

        public double Spot { get; private set; }

        public string Currency { get; }

        public IReadOnlyList<IGraphNode> Dependencies => constituents;

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return !definition.TryGetValue("constituents", out var constituents) || constituents.ValueKind != JsonValueKind.Array
                ? []
                : [.. constituents.EnumerateArray()
                .Where(name => name.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(name.GetString()))
                .Select(name => GetDependencyKey(name.GetString()))];
        }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(BasketSpotNode));
        }

        private static List<ISpotNode> GetConstituents(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
        {
            if (definition == null || !definition.TryGetValue("constituents", out var constituentsDefinition) || constituentsDefinition.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("BasketSpotNode requires a constituents array.");
            }

            var constituents = new List<ISpotNode>();
            foreach (var name in constituentsDefinition.EnumerateArray())
            {
                var key = name.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(name.GetString())
                    ? GetDependencyKey(name.GetString())
                    : null;
                if (key == null || !nodesByName.TryGetValue(key, out var node) || node is not ISpotNode constituent)
                {
                    throw new InvalidDataException($"BasketSpotNode references an unknown spot node '{name}'.");
                }

                constituents.Add(constituent);
            }

            return constituents;
        }

        private static string GetDependencyKey(string reference)
        {
            return reference.Contains("::", StringComparison.Ordinal)
                ? reference
                : GraphKey.Of(nameof(SpotNode), reference);
        }

        private static List<double> GetWeights(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (definition == null || !definition.TryGetValue("weights", out var weights) || weights.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("BasketSpotNode requires a weights array.");
            }

            var values = new List<double>();
            foreach (var weight in weights.EnumerateArray())
            {
                if (weight.ValueKind != JsonValueKind.Number || !weight.TryGetDouble(out var value))
                {
                    throw new InvalidDataException("BasketSpotNode weights must be numeric.");
                }

                values.Add(value);
            }

            return values;
        }

        public void Connect()
        {
            foreach (var constituent in constituentIndicesByNode.Keys)
            {
                constituent.Tick -= ConstituentTicked;
                constituent.Tick += ConstituentTicked;
            }
        }

        public string GetWeights()
        {
            return string.Join(", ", Enumerable.Range(0, weights.Length)
                .Where(index => Math.Abs(weights[index]) > Epsilon)
                .OrderBy(index => Dependencies[index].Name, StringComparer.OrdinalIgnoreCase)
                .Select(index => $"{Dependencies[index].Name}={weights[index]:0.###}"));
        }

        private static void AddActiveIndex(
            Dictionary<ISpotNode, List<int>> activeIndicesByNode,
            ISpotNode constituent,
            int index)
        {
            if (!activeIndicesByNode.TryGetValue(constituent, out var indices))
            {
                indices = [];
                activeIndicesByNode[constituent] = indices;
            }

            indices.Add(index);
        }

        private double CalculateSpot()
        {
            var spot = 0.0;
            for (int i = 0; i < weights.Length; i++)
            {
                spot += weights[i] * latestValues[i];
            }

            return spot;
        }

        private void ConstituentTicked(object sender, QuoteTick e)
        {
            if (sender is not ISpotNode node || !constituentIndicesByNode.TryGetValue(node, out var indices))
            {
                return;
            }

            lock (stateLock)
            {
                foreach (var index in indices)
                {
                    latestValues[index] = e.Value;
                    if (!hasLatestValue[index])
                    {
                        hasLatestValue[index] = true;
                        availableConstituentCount++;
                    }
                }

                if (availableConstituentCount == requiredConstituentCount)
                {
                    var spot = CalculateSpot();
                    Spot = spot;
                    var quoteTick = new QuoteTick(Name, spot);
                    Tick?.Invoke(this, quoteTick);
                }
            }
        }

        public Task RunOnceAsync()
        {
            return Task.Run(() =>
            {
                lock (stateLock)
                {
                    foreach (var indexes in constituentIndicesByNode.Values)
                    {
                        foreach (var index in indexes)
                        {
                            latestValues[index] = constituents[index].Spot;
                            if (!hasLatestValue[index])
                            {
                                hasLatestValue[index] = true;
                                availableConstituentCount++;
                            }
                        }
                    }

                    if (availableConstituentCount == requiredConstituentCount)
                    {
                        var spot = CalculateSpot();
                        Spot = spot;
                        var quoteTick = new QuoteTick(Name, spot);
                        Tick?.Invoke(this, quoteTick);
                    }
                }
            });
        }
    }
}
