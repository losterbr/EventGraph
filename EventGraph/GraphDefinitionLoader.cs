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

            var types = definitions.Select(GetType).ToList();
            var unsupportedType = types.FirstOrDefault(type => type != "SimulatedQuoteSource" && type != "BasketAggregate");
            if (unsupportedType != null)
            {
                throw new InvalidDataException($"Unsupported graph node type: '{unsupportedType}'.");
            }

            var sources = definitions
                .Where(definition => GetType(definition) == "SimulatedQuoteSource")
                .Select(definition => new SimulatedQuoteSource(definition))
                .ToList();
            var nodesByName = sources.Cast<IQuoteNode>().ToDictionary(source => source.Name, StringComparer.OrdinalIgnoreCase);
            var baskets = definitions
                .Where(definition => GetType(definition) == "BasketAggregate")
                .Select(definition => new BasketAggregate(definition, nodesByName))
                .ToList();

            return sources.Cast<IQuoteNode>().Concat(baskets).ToList();
        }

        private static string GetType(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (!definition.TryGetValue("type", out var type) || type.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(type.GetString()))
            {
                throw new InvalidDataException("Every graph definition must provide a non-empty string type.");
            }

            return type.GetString();
        }

        private static IReadOnlyDictionary<string, JsonElement> LoadDefinition(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var document = JsonDocument.Parse(stream);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException($"Graph definition must be a JSON object: {path}");
                }

                return document.RootElement
                    .EnumerateObject()
                    .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"Graph definition is invalid: {path}", exception);
            }
        }
    }
}
