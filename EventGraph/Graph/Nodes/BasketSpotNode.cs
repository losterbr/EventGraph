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
    public class BasketSpotNode : ISpotNode, ISpotDefinitionProvider
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
            : this(new BasketDefinitionProvider(definition).Definition, nodesByName)
        {
        }

        private BasketSpotNode(BasketDefinition definition, IReadOnlyDictionary<string, IGraphNode> nodesByName)
            : this(definition.Name, GetConstituents(definition.Constituents, nodesByName), definition.Weights)
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
            Definition = GetDefinition(this.constituents[0]);
            if (this.constituents.Any(constituent => !string.Equals(GetDefinition(constituent).Currency, Definition.Currency, StringComparison.OrdinalIgnoreCase)))
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

        public SpotDefinition Definition { get; }

        public IReadOnlyList<IGraphNode> Dependencies => constituents;

        internal static IGraphNode Create(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
        {
            return new BasketSpotNode(definition, nodesByName);
        }

        internal static string GetNodeName(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return GraphDefinitionEnrichmentContext.GetNodeName(definition);
        }

        internal static bool IsSource()
        {
            return false;
        }

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return [.. new BasketDefinitionProvider(definition).Definition.Constituents.Select(GetDependencyKey)];
        }

        internal static IReadOnlyDictionary<string, JsonElement> EnrichDefinition(
            GraphDefinitionEnrichmentContext context,
            IReadOnlyDictionary<string, JsonElement> definition)
        {
            var basketDefinition = new BasketDefinitionProvider(definition).Definition;
            var normalizedConstituents = basketDefinition.Constituents
                .Select(constituent => context.ContainsDefinition(GraphKey.Of(nameof(BasketSpotNode), constituent))
                    ? GraphKey.Of(nameof(BasketSpotNode), constituent)
                    : GraphKey.Of(nameof(SpotNode), constituent))
                .ToArray();
            var enrichedDefinition = new Dictionary<string, JsonElement>(definition, StringComparer.OrdinalIgnoreCase)
            {
                ["constituents"] = JsonSerializer.SerializeToElement(normalizedConstituents)
            };

            foreach (var dependencyKey in GetDependencyNames(enrichedDefinition))
            {
                if (!dependencyKey.StartsWith($"{nameof(BasketSpotNode)}::", StringComparison.Ordinal))
                {
                    var name = dependencyKey[(dependencyKey.IndexOf("::", StringComparison.Ordinal) + 2)..];
                    context.AddSyntheticIfMissing(nameof(SpotNode), name, new Dictionary<string, JsonElement>
                    {
                        ["source"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(EquitySource), name))
                    });
                }
            }

            return enrichedDefinition;
        }

        private static SpotDefinition GetDefinition(ISpotNode node)
        {
            return node is ISpotDefinitionProvider provider
                ? provider.Definition
                : throw new ArgumentException($"Basket constituent '{node.Name}' does not provide a spot definition.", nameof(node));
        }

        private static List<ISpotNode> GetConstituents(
            IReadOnlyList<string> references,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
        {
            var constituents = new List<ISpotNode>();
            foreach (var reference in references)
            {
                var key = GetDependencyKey(reference);
                if (key == null || !nodesByName.TryGetValue(key, out var node) || node is not ISpotNode constituent)
                {
                    throw new InvalidDataException($"BasketSpotNode references an unknown spot node '{reference}'.");
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
