using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EventGraph
{
    internal sealed class GraphDefinitionEnrichmentContext(
        Dictionary<string, IReadOnlyDictionary<string, JsonElement>> definitionsByKey,
        List<IReadOnlyDictionary<string, JsonElement>> definitionsToAdd)
    {
        public bool ContainsDefinition(string key)
        {
            return definitionsByKey.ContainsKey(key);
        }

        public IReadOnlyDictionary<string, JsonElement> GetDefinition(string key)
        {
            return definitionsByKey.TryGetValue(key, out var definition)
                ? definition
                : throw new InvalidDataException($"Graph definition '{key}' was not found.");
        }

        public IEnumerable<IReadOnlyDictionary<string, JsonElement>> Definitions => definitionsByKey.Values;

        public void AddSyntheticIfMissing(string type, string name, Dictionary<string, JsonElement> properties = null)
        {
            var key = GraphKey.Of(type, name);
            if (definitionsByKey.ContainsKey(key))
            {
                return;
            }

            var synthetic = properties == null
                ? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, JsonElement>(properties, StringComparer.OrdinalIgnoreCase);
            synthetic["type"] = JsonSerializer.SerializeToElement(type);
            synthetic["name"] = JsonSerializer.SerializeToElement(name);
            definitionsToAdd.Add(synthetic);
        }

        public void AddReferencedDefinitionIfMissing(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            var reference = GetString(definition, propertyName);
            var separator = reference.IndexOf("::", StringComparison.Ordinal);
            if (separator < 0)
            {
                return;
            }

            var type = reference[..separator];
            var name = reference[(separator + 2)..];
            AddSyntheticIfMissing(type, name);
        }

        public static string GetType(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return !definition.TryGetValue("type", out var type) || type.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(type.GetString())
                ? throw new InvalidDataException("Every graph definition must provide a non-empty string type.")
                : type.GetString();
        }

        public static string GetNodeName(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return !definition.TryGetValue("name", out var name) || name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString())
                ? throw new InvalidDataException("Every graph definition must provide a non-empty string name.")
                : name.GetString();
        }

        public static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, "graph definition");
        }

        public static string GetReferencedNodeName(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            var reference = GetString(definition, propertyName);
            var separator = reference.IndexOf("::", StringComparison.Ordinal);
            return separator < 0
                ? reference
                : reference[(separator + 2)..];
        }

        public static string GetOptionalString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return definition.TryGetValue(propertyName, out var property) && property.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.GetString())
                ? property.GetString()
                : null;
        }
    }
}