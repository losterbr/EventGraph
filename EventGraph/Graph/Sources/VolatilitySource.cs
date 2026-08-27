using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EventGraph
{
    /// <summary>
    /// Sources a volatility quote from another graph node as an explicit dependency.
    /// </summary>
    public sealed class VolatilitySource : IVolQuoteNode
    {
        private readonly IVolQuoteNode source;

        public VolatilitySource(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
            : this(
                GetString(definition, "name"),
                GetNode<IVolQuoteNode>(definition, "constituent", nodesByName))
        {
        }

        public VolatilitySource(string name, IVolQuoteNode source)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Volatility source name cannot be empty.", nameof(name));
            }

            ArgumentNullException.ThrowIfNull(source);

            Name = name;
            this.source = source;
        }

        public string Name { get; }

        public string Type => nameof(VolatilitySource);

        public double Volatility => source.Volatility;

        public IReadOnlyList<IGraphNode> Dependencies => [source];

        internal static IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return [GetString(definition, "constituent")];
        }

        private static TNode GetNode<TNode>(
            IReadOnlyDictionary<string, JsonElement> definition,
            string propertyName,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
            where TNode : class, IGraphNode
        {
            var nodeName = GetString(definition, propertyName);
            return nodesByName != null && nodesByName.TryGetValue(nodeName, out var node) && node is TNode typedNode
                ? typedNode
                : throw new InvalidDataException($"VolatilitySource references an invalid {propertyName} '{nodeName}'.");
        }

        private static string GetString(IReadOnlyDictionary<string, JsonElement> definition, string propertyName)
        {
            return JsonDefinitionReader.GetString(definition, propertyName, nameof(VolatilitySource));
        }
    }
}
