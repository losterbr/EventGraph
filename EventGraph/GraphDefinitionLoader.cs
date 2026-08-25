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

            return definitions
                .Select(CreateNode)
                .ToList();
        }

        private static IQuoteNode CreateNode(SimulatedQuoteSourceDefinition definition)
        {
            return definition.Type switch
            {
                "SimulatedQuoteSource" => new SimulatedQuoteSource(
                    definition.Name,
                    definition.Spot,
                    definition.Volatility,
                    definition.MeanTickTimeSeconds),
                _ => throw new InvalidDataException($"Unsupported graph node type: '{definition.Type}'.")
            };
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
        }
    }
}
