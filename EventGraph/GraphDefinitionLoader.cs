using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Loads graph nodes from JSON graph definitions.
    /// </summary>
    public static class GraphDefinitionLoader
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static IReadOnlyList<IQuoteNode> LoadNodes(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("A graph definition directory is required.", nameof(directoryPath));
            }

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Graph definition directory was not found: {directoryPath}");
            }

            var definitions = Directory
                .GetFiles(directoryPath, "*.json")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(LoadDefinition)
                .ToList();

            if (definitions.Count == 0)
            {
                throw new InvalidOperationException($"No JSON graph definitions were found in: {directoryPath}");
            }

            var unsupportedType = definitions.FirstOrDefault(definition =>
                definition.Type != "SimulatedQuoteSource" && definition.Type != "BasketAggregate");
            if (unsupportedType != null)
            {
                throw new InvalidDataException($"Unsupported graph node type: '{unsupportedType.Type}'.");
            }

            var sources = definitions
                .Where(definition => definition.Type == "SimulatedQuoteSource")
                .Select(CreateSource)
                .ToList();
            var nodesByName = sources.ToDictionary(source => source.Name, StringComparer.OrdinalIgnoreCase);
            var baskets = definitions
                .Where(definition => definition.Type == "BasketAggregate")
                .Select(definition => CreateBasket(definition, nodesByName))
                .ToList();

            return sources.Cast<IQuoteNode>().Concat(baskets).ToList();
        }

        private static SimulatedQuoteSource CreateSource(SimulatedQuoteSourceDefinition definition)
        {
            return new SimulatedQuoteSource(
                definition.Name,
                definition.Spot,
                definition.Volatility,
                definition.MeanTickTimeSeconds);
        }

        private static BasketAggregate CreateBasket(
            SimulatedQuoteSourceDefinition definition,
            IReadOnlyDictionary<string, SimulatedQuoteSource> nodesByName)
        {
            if (definition.Names == null || definition.Names.Count == 0)
            {
                throw new InvalidDataException($"Basket '{definition.Name}' must define at least one source name.");
            }

            var constituents = definition.Names.Select(name =>
            {
                if (!nodesByName.TryGetValue(name, out var source))
                {
                    throw new InvalidDataException($"Basket '{definition.Name}' references unknown source '{name}'.");
                }

                return (IQuoteNode)source;
            }).ToList();

            return new BasketAggregate(definition.Name, constituents, definition.Weights);
        }

        private static SimulatedQuoteSourceDefinition LoadDefinition(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                return JsonSerializer.Deserialize<SimulatedQuoteSourceDefinition>(stream, SerializerOptions)
                    ?? throw new InvalidDataException($"Graph definition is empty: {path}");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"Graph definition is invalid: {path}", exception);
            }
        }

        private sealed class SimulatedQuoteSourceDefinition
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public double Spot { get; set; }
            public double Volatility { get; set; }
            public double MeanTickTimeSeconds { get; set; }
            public List<string> Names { get; set; }
            public List<double> Weights { get; set; }
        }
    }
}
