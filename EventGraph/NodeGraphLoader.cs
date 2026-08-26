using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Loads graph nodes from JSON graph definitions and resolves them in dependency order.
    /// </summary>
    public static class NodeGraphLoader
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

            var definitionsByName = new Dictionary<string, IReadOnlyDictionary<string, JsonElement>>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitions)
            {
                var name = GetName(definition);
                if (definitionsByName.ContainsKey(name))
                {
                    throw new InvalidDataException($"Duplicate graph node name '{name}' detected in '{directoryPath}'.");
                }

                definitionsByName[name] = definition;
            }

            var unsupportedType = definitions
                .Select(GetType)
                .FirstOrDefault(type => !NodeRegistry.IsSupportedType(type));
            if (unsupportedType != null)
            {
                throw new InvalidDataException($"Unsupported graph node type: '{unsupportedType}'.");
            }

            var nodesByName = new Dictionary<string, IQuoteNode>(StringComparer.OrdinalIgnoreCase);
            var remainingDefinitions = new Dictionary<string, IReadOnlyDictionary<string, JsonElement>>(definitionsByName, StringComparer.OrdinalIgnoreCase);
            var resolvedOrder = new List<IQuoteNode>();

            while (remainingDefinitions.Count > 0)
            {
                var ready = remainingDefinitions
                    .Where(pair => DependenciesAreSatisfied(pair.Value, nodesByName, definitionsByName))
                    .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(pair => pair.Value)
                    .ToList();

                if (ready.Count == 0)
                {
                    var unresolvedNames = string.Join(", ", remainingDefinitions.Keys);
                    throw new InvalidOperationException($"Unable to satisfy node dependencies for: {unresolvedNames}. Check for missing or cyclic references.");
                }

                foreach (var definition in ready)
                {
                    var node = NodeRegistry.CreateNode(definition, nodesByName);
                    nodesByName[node.Name] = node;
                    resolvedOrder.Add(node);
                    remainingDefinitions.Remove(GetName(definition));
                }
            }

            return resolvedOrder;
        }

        private static bool DependenciesAreSatisfied(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IQuoteNode> nodesByName,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, JsonElement>> definitionsByName)
        {
            var dependencies = GetDependencies(definition);
            foreach (var dependencyName in dependencies)
            {
                if (!definitionsByName.ContainsKey(dependencyName))
                {
                    throw new InvalidDataException($"Node '{GetName(definition)}' references an unknown dependency '{dependencyName}'.");
                }

                if (!nodesByName.ContainsKey(dependencyName))
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetType(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (!definition.TryGetValue("type", out var type) || type.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(type.GetString()))
            {
                throw new InvalidDataException("Every graph definition must provide a non-empty string type.");
            }

            return type.GetString();
        }

        private static string GetName(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (!definition.TryGetValue("name", out var name) || name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString()))
            {
                throw new InvalidDataException("Every graph definition must provide a non-empty string name.");
            }

            return name.GetString();
        }

        private static IReadOnlyList<string> GetDependencies(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (!definition.TryGetValue("names", out var names) || names.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            var dependencies = new List<string>();
            foreach (var name in names.EnumerateArray())
            {
                if (name.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(name.GetString()))
                {
                    dependencies.Add(name.GetString());
                }
            }

            return dependencies;
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
