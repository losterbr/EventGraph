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
        public static IReadOnlyList<ISpotNode> LoadNodes(string directoryPath)
        {
            return LoadGraph(directoryPath).QuoteNodes;
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

            var definitionsByKey = new Dictionary<string, IReadOnlyDictionary<string, JsonElement>>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitions)
            {
                var key = GetNodeKey(definition);
                if (definitionsByKey.ContainsKey(key))
                {
                    throw new InvalidDataException($"Duplicate graph node key '{key}' detected in '{directoryPath}'.");
                }

                definitionsByKey[key] = definition;
            }

            var unsupportedType = definitions
                .Select(GetType)
                .FirstOrDefault(type => !NodeRegistry.IsSupportedType(type));
            if (unsupportedType != null)
            {
                throw new InvalidDataException($"Unsupported graph node type: '{unsupportedType}'.");
            }

            var toAdd = new List<IReadOnlyDictionary<string, JsonElement>>();
            var enrichmentContext = new GraphDefinitionEnrichmentContext(definitionsByKey, toAdd);
            foreach (var definition in definitionsByKey.Values.ToList())
            {
                definitionsByKey[GetNodeKey(definition)] = NodeRegistry.EnrichDefinition(enrichmentContext, definition);
            }

            foreach (var definition in toAdd)
            {
                var type = GetType(definition);
                if (NodeRegistry.IsSourceType(type))
                {
                    throw new InvalidDataException($"Source node type '{type}' must be defined by a JSON file and cannot be synthesized.");
                }

                var key = GetNodeKey(definition);
                if (!definitionsByKey.ContainsKey(key))
                {
                    definitionsByKey[key] = definition;
                }
            }

            // Kahn's algorithm: track unresolved dependency counts and walk ready nodes in stable key order.
            var inDegreeByKey = definitionsByKey.Keys.ToDictionary(key => key, _ => 0, StringComparer.OrdinalIgnoreCase);
            var dependentsByKey = definitionsByKey.Keys.ToDictionary(key => key, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitionsByKey.Values)
            {
                var key = GetNodeKey(definition);
                foreach (var dependencyKey in NodeRegistry.GetDependencyNames(definition))
                {
                    if (ResolveDependencyKey(dependencyKey, definitionsByKey) is not { } resolvedKey)
                    {
                        throw new InvalidDataException($"Node '{key}' references an unknown dependency '{dependencyKey}'.");
                    }

                    inDegreeByKey[key]++;
                    dependentsByKey[resolvedKey].Add(key);
                }
            }

            var nodesByKey = new Dictionary<string, IGraphNode>(StringComparer.OrdinalIgnoreCase);
            var nodesByName = new Dictionary<string, IGraphNode>(StringComparer.OrdinalIgnoreCase);
            var resolvedOrder = new List<IGraphNode>();
            var readyKeys = new SortedSet<string>(
                inDegreeByKey.Where(pair => pair.Value == 0).Select(pair => pair.Key),
                StringComparer.OrdinalIgnoreCase);

            while (readyKeys.Count > 0)
            {
                var key = readyKeys.Min;
                _ = readyKeys.Remove(key);

                var definition = definitionsByKey[key];
                var node = NodeRegistry.CreateNode(definition, new CompositeNodeLookup(nodesByKey, nodesByName));
                if (NodeRegistry.IsSourceType(node.Type) && node.Dependencies.Count != 0)
                {
                    throw new InvalidDataException($"Source node '{GraphKey.Of(node.Type, node.Name)}' cannot depend on other nodes.");
                }

                nodesByKey[GraphKey.Of(node.Type, node.Name)] = node;
                _ = nodesByName.TryAdd(node.Name, node);
                resolvedOrder.Add(node);

                foreach (var dependentKey in dependentsByKey[key])
                {
                    inDegreeByKey[dependentKey]--;
                    if (inDegreeByKey[dependentKey] == 0)
                    {
                        _ = readyKeys.Add(dependentKey);
                    }

                }
            }

            if (resolvedOrder.Count != definitionsByKey.Count)
            {
                var unresolvedKeys = string.Join(", ", definitionsByKey.Keys.Where(key => !nodesByKey.ContainsKey(key)));
                throw new InvalidOperationException($"Unable to satisfy node dependencies for: {unresolvedKeys}. Check for missing or cyclic references.");
            }

            return new QuoteGraph(resolvedOrder);
        }

        private static string GetType(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return !definition.TryGetValue("type", out var type) || type.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(type.GetString())
                ? throw new InvalidDataException("Every graph definition must provide a non-empty string type.")
                : type.GetString();
        }

        private static string ResolveDependencyKey(
            string dependencyKey,
            Dictionary<string, IReadOnlyDictionary<string, JsonElement>> definitionsByKey)
        {
            if (definitionsByKey.ContainsKey(dependencyKey))
            {
                return dependencyKey;
            }

            // Bare name: match exactly one key whose name suffix equals it.
            var matches = definitionsByKey.Keys
                .Where(key => key.EndsWith($"::{dependencyKey}", StringComparison.OrdinalIgnoreCase))
                .ToList();
            return matches.Count == 1 ? matches[0] : null;
        }

        private static string GetNodeKey(IReadOnlyDictionary<string, JsonElement> definition)
        {
            var type = GetType(definition);
            return !definition.TryGetValue("name", out var name) || name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString())
                ? GraphKey.Of(type, InferInternalNodeName(definition, type))
                : GraphKey.Of(type, name.GetString());
        }

        private static string InferInternalNodeName(IReadOnlyDictionary<string, JsonElement> definition, string type)
        {
            return type switch
            {
                nameof(ForwardCurveNode) => GetReferencedNodeName(definition, "spot"),
                _ => throw new InvalidDataException($"Graph definitions of type '{type}' must provide a non-empty string name.")
            };
        }

        private static string GetReferencedNodeName(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            var reference = GetStringProperty(definition, propertyName);
            var separator = reference.IndexOf("::", StringComparison.Ordinal);
            return separator < 0
                ? reference
                : reference[(separator + 2)..];
        }

        private static string GetStringProperty(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return !definition.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString())
                ? throw new InvalidDataException($"Every graph definition must provide a non-empty string '{propertyName}' property.")
                : property.GetString();
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

        /// <summary>
        /// Resolves dependency references against canonical keys first, then plain names as a fallback.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private sealed class CompositeNodeLookup(
            IReadOnlyDictionary<string, IGraphNode> nodesByKey,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
            : IReadOnlyDictionary<string, IGraphNode>
        {
            public IGraphNode this[string key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException(key);

            public IEnumerable<string> Keys => nodesByKey.Keys.Concat(nodesByName.Keys).Distinct(StringComparer.OrdinalIgnoreCase);

            public IEnumerable<IGraphNode> Values => nodesByKey.Values.Concat(nodesByName.Values).Distinct();

            public int Count => nodesByKey.Count + nodesByName.Count;

            public bool ContainsKey(string key)
            {
                return nodesByKey.ContainsKey(key) || nodesByName.ContainsKey(key);
            }

            public bool TryGetValue(string key, out IGraphNode value)
            {
                return nodesByKey.TryGetValue(key, out value) || nodesByName.TryGetValue(key, out value);
            }

            public IEnumerator<KeyValuePair<string, IGraphNode>> GetEnumerator()
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in nodesByKey)
                {
                    if (seen.Add(pair.Key))
                    {
                        yield return pair;
                    }
                }

                foreach (var pair in nodesByName)
                {
                    if (seen.Add(pair.Key))
                    {
                        yield return pair;
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
