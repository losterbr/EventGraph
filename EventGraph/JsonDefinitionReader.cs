using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Shared property-extraction helpers for parsing node JSON definitions.
    /// </summary>
    internal static class JsonDefinitionReader
    {
        public static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName, string nodeTypeName)
        {
            return definition == null || !definition.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString())
                ? throw new InvalidDataException($"{nodeTypeName} requires a non-empty '{propertyName}' property.")
                : property.GetString();
        }

        public static string GetStringOrDefault(
            IReadOnlyDictionary<string, JsonElement> definition,
            string propertyName,
            string defaultValue,
            string nodeTypeName)
        {
            return definition == null || !definition.ContainsKey(propertyName)
                ? defaultValue
                : GetString(definition, propertyName, nodeTypeName);
        }

        public static double GetDouble(IReadOnlyDictionary<string, JsonElement> definition, string propertyName, string nodeTypeName)
        {
            return definition == null || !definition.TryGetValue(propertyName, out var property) || property.ValueKind != JsonValueKind.Number || !property.TryGetDouble(out var value)
                ? throw new InvalidDataException($"{nodeTypeName} requires a numeric '{propertyName}' property.")
                : value;
        }
    }
}
