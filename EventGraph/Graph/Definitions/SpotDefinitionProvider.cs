using System.Collections.Generic;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Provides a spot definition loaded from JSON.
    /// </summary>
    public sealed class SpotDefinitionProvider(string name, string currency = "USD") : IDefinitionProvider<SpotDefinition>
    {
        public SpotDefinitionProvider(IReadOnlyDictionary<string, JsonElement> definition)
            : this(GetString(definition, "name"), GetStringOrDefault(definition, "currency", "USD"))
        {
        }

        public SpotDefinition Definition { get; } = new(name, currency);

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(SpotDefinitionProvider));
        }

        private static string GetStringOrDefault(
            IReadOnlyDictionary<string, JsonElement> definition,
            string propertyName,
            string defaultValue)
        {
            return JsonDefinitionReader.GetStringOrDefault(definition, propertyName, defaultValue, nameof(SpotDefinitionProvider));
        }
    }
}