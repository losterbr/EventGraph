using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Provides static constituent names and weights for a synthetic basket.
    /// </summary>
    public sealed class BasketDefinitionSource : IBasketDefinitionSourceNode
    {
        private const double Epsilon = 1e-9;

        public BasketDefinitionSource(IReadOnlyDictionary<string, JsonElement> definition)
            : this(GetString(definition, "name"), GetConstituents(definition), GetWeights(definition))
        {
        }

        public BasketDefinitionSource(string name, IReadOnlyList<string> constituents, IReadOnlyList<double> weights)
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

        public string Type => nameof(BasketDefinitionSource);

        public IReadOnlyList<string> Constituents { get; }

        public IReadOnlyList<double> Weights { get; }

        public IReadOnlyList<IGraphNode> Dependencies => [];

        internal static IGraphNode Create(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> _)
        {
            return new BasketDefinitionSource(definition);
        }

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> _)
        {
            return [];
        }

        internal static IReadOnlyDictionary<string, JsonElement> EnrichDefinition(
            GraphDefinitionEnrichmentContext context,
            IReadOnlyDictionary<string, JsonElement> definition)
        {
            var source = new BasketDefinitionSource(definition);
            var constituentKeys = source.Constituents
                .Select(name => context.ContainsDefinition(GraphKey.Of(nameof(BasketDefinitionSource), name))
                    ? GraphKey.Of(nameof(BasketSpotNode), name)
                    : GraphKey.Of(nameof(SpotNode), name))
                .ToArray();

            foreach (var constituentKey in constituentKeys.Where(key => key.StartsWith($"{nameof(SpotNode)}::", StringComparison.Ordinal)))
            {
                var name = constituentKey[(constituentKey.IndexOf("::", StringComparison.Ordinal) + 2)..];
                context.AddSyntheticIfMissing(nameof(SpotNode), name, new Dictionary<string, JsonElement>
                {
                    ["source"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(EquitySource), name))
                });
            }

            context.AddSyntheticIfMissing(nameof(BasketSpotNode), source.Name, new Dictionary<string, JsonElement>
            {
                ["source"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(BasketDefinitionSource), source.Name)),
                ["constituents"] = JsonSerializer.SerializeToElement(constituentKeys)
            });
            return definition;
        }

        internal static string GetNodeName(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return GraphDefinitionEnrichmentContext.GetNodeName(definition);
        }

        internal static bool IsSource()
        {
            return true;
        }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(BasketDefinitionSource));
        }

        private static IReadOnlyList<string> GetConstituents(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (definition == null || !definition.TryGetValue("constituents", out var constituents) || constituents.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("BasketDefinitionSource requires a constituents array.");
            }

            return [.. constituents.EnumerateArray().Select(constituent =>
                constituent.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(constituent.GetString())
                    ? constituent.GetString()
                    : throw new InvalidDataException("BasketDefinitionSource constituents must be non-empty strings."))];
        }

        private static IReadOnlyList<double> GetWeights(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (definition == null || !definition.TryGetValue("weights", out var weights) || weights.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("BasketDefinitionSource requires a weights array.");
            }

            return [.. weights.EnumerateArray().Select(weight =>
                weight.ValueKind == JsonValueKind.Number && weight.TryGetDouble(out var value)
                    ? value
                    : throw new InvalidDataException("BasketDefinitionSource weights must be numeric."))];
        }
    }
}