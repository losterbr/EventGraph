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

            // Auto-materialize internal wrapper nodes referenced by other definitions.
            var toAdd = new List<IReadOnlyDictionary<string, JsonElement>>();
            foreach (var definition in definitionsByKey.Values)
            {
                var type = GetType(definition);
                if (type == nameof(EquitySource))
                {
                    AddSyntheticIfMissing(toAdd, definitionsByKey, nameof(SpotNode), GetNodeName(definition), new Dictionary<string, JsonElement>
                    {
                        ["name"] = JsonSerializer.SerializeToElement(GetNodeName(definition)),
                        ["source"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(EquitySource), GetNodeName(definition)))
                    });
                }
                else if (type == nameof(ForwardCurveNode))
                {
                    AddIfMissing(toAdd, definitionsByKey, definition, "discountCurve");
                    AddIfMissing(toAdd, definitionsByKey, definition, "spot");
                }
                else if (type == nameof(BasketSpotNode))
                {
                    var basketKey = GetNodeKey(definition);
                    var normalizedDefinition = EnrichBasketDefinition(definitionsByKey, definition);
                    definitionsByKey[basketKey] = normalizedDefinition;
                    AddBasketSpotNodesIfMissing(toAdd, definitionsByKey, normalizedDefinition);
                }
                else if (type == nameof(EquityOption))
                {
                    var optionKey = GetNodeKey(definition);
                    definitionsByKey[optionKey] = EnrichEquityOptionDefinition(toAdd, definitionsByKey, definition);
                }
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

        private static string GetNodeName(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return !definition.TryGetValue("name", out var name) || name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString())
                ? throw new InvalidDataException("Every graph definition must provide a non-empty string name.")
                : name.GetString();
        }

        private static string GetStringProperty(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return !definition.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString())
                ? throw new InvalidDataException($"Every graph definition must provide a non-empty string '{propertyName}' property.")
                : property.GetString();
        }

        private static void AddIfMissing(
            List<IReadOnlyDictionary<string, JsonElement>> toAdd,
            Dictionary<string, IReadOnlyDictionary<string, JsonElement>> definitionsByKey,
            IReadOnlyDictionary<string, JsonElement> definition,
            string propertyName)
        {
            var reference = GetStringProperty(definition, propertyName);
            var separator = reference.IndexOf("::", StringComparison.Ordinal);
            if (separator < 0)
            {
                return;
            }

            var refType = reference[..separator];
            var refName = reference[(separator + 2)..];
            var key = GraphKey.Of(refType, refName);
            if (definitionsByKey.ContainsKey(key))
            {
                return;
            }

            // Auto-create internal wrapper definitions from referenced source keys.
            var synthetic = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
            {
                ["type"] = JsonSerializer.SerializeToElement(refType)
            };
            if (refType is nameof(RateCurveNode) or nameof(SpotNode) or nameof(VolatilityNode))
            {
                synthetic["name"] = JsonSerializer.SerializeToElement(refName);
            }

            toAdd.Add(synthetic);
        }

        private static void AddBasketSpotNodesIfMissing(
            List<IReadOnlyDictionary<string, JsonElement>> toAdd,
            Dictionary<string, IReadOnlyDictionary<string, JsonElement>> definitionsByKey,
            IReadOnlyDictionary<string, JsonElement> definition)
        {
            foreach (var dependencyKey in BasketSpotNode.GetDependencyNames(definition))
            {
                if (dependencyKey.StartsWith($"{nameof(BasketSpotNode)}::", StringComparison.Ordinal))
                {
                    continue;
                }

                var name = dependencyKey[(dependencyKey.IndexOf("::", StringComparison.Ordinal) + 2)..];
                AddSyntheticIfMissing(toAdd, definitionsByKey, nameof(SpotNode), name, new Dictionary<string, JsonElement>
                {
                    ["name"] = JsonSerializer.SerializeToElement(name),
                    ["source"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(EquitySource), name))
                });
            }
        }

        private static Dictionary<string, JsonElement> EnrichBasketDefinition(
            Dictionary<string, IReadOnlyDictionary<string, JsonElement>> definitionsByKey,
            IReadOnlyDictionary<string, JsonElement> definition)
        {
            if (!definition.TryGetValue("constituents", out var constituents) || constituents.ValueKind != JsonValueKind.Array)
            {
                return new Dictionary<string, JsonElement>(definition, StringComparer.OrdinalIgnoreCase);
            }

            var normalizedConstituents = constituents.EnumerateArray()
                .Select(constituent => constituent.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(constituent.GetString())
                    ? definitionsByKey.ContainsKey(GraphKey.Of(nameof(BasketSpotNode), constituent.GetString()))
                        ? GraphKey.Of(nameof(BasketSpotNode), constituent.GetString())
                        : GraphKey.Of(nameof(SpotNode), constituent.GetString())
                    : constituent.GetRawText())
                .ToArray();
            return new Dictionary<string, JsonElement>(definition, StringComparer.OrdinalIgnoreCase)
            {
                ["constituents"] = JsonSerializer.SerializeToElement(normalizedConstituents)
            };
        }

        private static Dictionary<string, JsonElement> EnrichEquityOptionDefinition(
            List<IReadOnlyDictionary<string, JsonElement>> toAdd,
            Dictionary<string, IReadOnlyDictionary<string, JsonElement>> definitionsByKey,
            IReadOnlyDictionary<string, JsonElement> definition)
        {
            var underlyer = GetStringProperty(definition, "underlyer");
            var currency = GetUnderlyerCurrency(definitionsByKey, underlyer);
            var rateSourceName = GetRateSourceName(definitionsByKey, currency);

            AddSyntheticIfMissing(toAdd, definitionsByKey, nameof(SpotNode), underlyer, new Dictionary<string, JsonElement>
            {
                ["name"] = JsonSerializer.SerializeToElement(underlyer)
            });
            AddSyntheticIfMissing(toAdd, definitionsByKey, nameof(VolatilityNode), underlyer, new Dictionary<string, JsonElement>
            {
                ["name"] = JsonSerializer.SerializeToElement(underlyer)
            });
            AddSyntheticIfMissing(toAdd, definitionsByKey, nameof(RateCurveNode), rateSourceName, new Dictionary<string, JsonElement>
            {
                ["name"] = JsonSerializer.SerializeToElement(rateSourceName)
            });
            AddSyntheticIfMissing(toAdd, definitionsByKey, nameof(ForwardCurveNode), underlyer, new Dictionary<string, JsonElement>
            {
                ["spot"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(SpotNode), underlyer)),
                ["discountCurve"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(RateCurveNode), rateSourceName))
            });

            return new Dictionary<string, JsonElement>(definition, StringComparer.OrdinalIgnoreCase)
            {
                ["forward"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(ForwardCurveNode), underlyer)),
                ["volatility"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(VolatilityNode), underlyer)),
                ["discountCurve"] = JsonSerializer.SerializeToElement(GraphKey.Of(nameof(RateCurveNode), rateSourceName))
            };
        }

        private static string GetUnderlyerCurrency(
            Dictionary<string, IReadOnlyDictionary<string, JsonElement>> definitionsByKey,
            string underlyer)
        {
            var sourceKey = GraphKey.Of(nameof(EquitySource), underlyer);
            var sourceDefinition = definitionsByKey.TryGetValue(sourceKey, out var definition)
                ? definition
                : throw new InvalidDataException($"EquityOption references an unknown underlyer '{underlyer}'.");

            return GetOptionalString(sourceDefinition, "currency") ?? "USD";
        }

        private static string GetRateSourceName(
            Dictionary<string, IReadOnlyDictionary<string, JsonElement>> definitionsByKey,
            string currency)
        {
            var matches = definitionsByKey.Values
                .Where(definition => GetType(definition) == nameof(CurrencyRateSource))
                .Where(definition => string.Equals(GetOptionalString(definition, "currency") ?? GetNodeName(definition), currency, StringComparison.OrdinalIgnoreCase))
                .Select(GetNodeName)
                .ToList();
            return matches.Count switch
            {
                1 => matches[0],
                0 => throw new InvalidDataException($"No CurrencyRateSource found for currency '{currency}'."),
                _ => throw new InvalidDataException($"Multiple CurrencyRateSource definitions found for currency '{currency}'.")
            };
        }

        private static string GetOptionalString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return definition.TryGetValue(propertyName, out var property) && property.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.GetString())
                ? property.GetString()
                : null;
        }

        private static void AddSyntheticIfMissing(
            List<IReadOnlyDictionary<string, JsonElement>> toAdd,
            Dictionary<string, IReadOnlyDictionary<string, JsonElement>> definitionsByKey,
            string type,
            string name,
            Dictionary<string, JsonElement> extraProperties)
        {
            var key = GraphKey.Of(type, name);
            if (definitionsByKey.ContainsKey(key))
            {
                return;
            }

            var synthetic = new Dictionary<string, JsonElement>(extraProperties, StringComparer.OrdinalIgnoreCase)
            {
                ["type"] = JsonSerializer.SerializeToElement(type)
            };
            toAdd.Add(synthetic);
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
