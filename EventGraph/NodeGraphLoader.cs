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
            return LoadGraph(directoryPath).Nodes;
        }

        public static QuoteGraph LoadGraph(string directoryPath)
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

            // Kahn's algorithm: track unresolved dependency counts and walk ready nodes in stable name order.
            var inDegreeByName = definitionsByName.Keys.ToDictionary(name => name, _ => 0, StringComparer.OrdinalIgnoreCase);
            var dependentsByName = definitionsByName.Keys.ToDictionary(name => name, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitionsByName.Values)
            {
                var name = GetName(definition);
                foreach (var dependencyName in GetDependencies(definition))
                {
                    if (!definitionsByName.ContainsKey(dependencyName))
                    {
                        throw new InvalidDataException($"Node '{name}' references an unknown dependency '{dependencyName}'.");
                    }

                    inDegreeByName[name]++;
                    dependentsByName[dependencyName].Add(name);
                }
            }

            var nodesByName = new Dictionary<string, IQuoteNode>(StringComparer.OrdinalIgnoreCase);
            var resolvedOrder = new List<IQuoteNode>();
            var readyNames = new SortedSet<string>(
                inDegreeByName.Where(pair => pair.Value == 0).Select(pair => pair.Key),
                StringComparer.OrdinalIgnoreCase);

            while (readyNames.Count > 0)
            {
                var name = readyNames.Min;
                _ = readyNames.Remove(name);

                var definition = definitionsByName[name];
                var node = NodeRegistry.CreateNode(definition, nodesByName);
                nodesByName[node.Name] = node;
                resolvedOrder.Add(node);

                foreach (var dependentName in dependentsByName[name])
                {
                    inDegreeByName[dependentName]--;
                    if (inDegreeByName[dependentName] == 0)
                    {
                        _ = readyNames.Add(dependentName);
                    }

                }
            }

            if (resolvedOrder.Count != definitionsByName.Count)
            {
                var unresolvedNames = string.Join(", ", definitionsByName.Keys.Where(name => !nodesByName.ContainsKey(name)));
                throw new InvalidOperationException($"Unable to satisfy node dependencies for: {unresolvedNames}. Check for missing or cyclic references.");
            }

            return new QuoteGraph(resolvedOrder);
        }

        private static string GetType(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return !definition.TryGetValue("type", out var type) || type.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(type.GetString())
                ? throw new InvalidDataException("Every graph definition must provide a non-empty string type.")
                : type.GetString();
        }

        private static string GetName(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return !definition.TryGetValue("name", out var name) || name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString())
                ? throw new InvalidDataException("Every graph definition must provide a non-empty string name.")
                : name.GetString();
        }

        private static List<string> GetDependencies(IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (!definition.TryGetValue("names", out var names) || names.ValueKind != JsonValueKind.Array)
            {
                return [];
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
                return document.RootElement.ValueKind != JsonValueKind.Object
                    ? throw new InvalidDataException($"Graph definition must be a JSON object: {path}")
                    : (IReadOnlyDictionary<string, JsonElement>)document.RootElement
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
