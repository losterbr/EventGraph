using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace EventGraph
{
    internal sealed class GraphNodeRegistration(Type nodeType)
    {
        private delegate IGraphNode NodeFactory(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName);

        private delegate IReadOnlyList<string> DependencyResolver(IReadOnlyDictionary<string, JsonElement> definition);

        private delegate IReadOnlyDictionary<string, JsonElement> DefinitionEnricher(
            GraphDefinitionEnrichmentContext context,
            IReadOnlyDictionary<string, JsonElement> definition);

        private delegate string NameResolver(IReadOnlyDictionary<string, JsonElement> definition);

        private delegate bool SourceClassifier();

        private readonly NodeFactory create = GetDelegate<NodeFactory>(nodeType, "Create");
        private readonly DependencyResolver getDependencyNames = GetDelegate<DependencyResolver>(nodeType, "GetDependencyNames");
        private readonly DefinitionEnricher enrichDefinition = GetDelegate<DefinitionEnricher>(nodeType, "EnrichDefinition");
        private readonly NameResolver getNodeName = GetDelegate<NameResolver>(nodeType, "GetNodeName");
        private readonly SourceClassifier isSource = GetDelegate<SourceClassifier>(nodeType, "IsSource");

        public string NodeType { get; } = nodeType.Name;

        public bool IsSource => isSource();

        public IGraphNode Create(
            IReadOnlyDictionary<string, JsonElement> definition,
            IReadOnlyDictionary<string, IGraphNode> nodesByName)
        {
            return create(definition, nodesByName);
        }

        public IReadOnlyList<string> GetDependencyNames(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return getDependencyNames(definition);
        }

        public IReadOnlyDictionary<string, JsonElement> EnrichDefinition(
            GraphDefinitionEnrichmentContext context,
            IReadOnlyDictionary<string, JsonElement> definition)
        {
            return enrichDefinition(context, definition);
        }

        public string GetNodeName(IReadOnlyDictionary<string, JsonElement> definition)
        {
            return getNodeName(definition);
        }

        private static TDelegate GetDelegate<TDelegate>(Type nodeType, string methodName)
            where TDelegate : Delegate
        {
            var method = nodeType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return method != null
                ? method.CreateDelegate<TDelegate>()
                : throw new InvalidOperationException($"Registered graph node type '{nodeType.Name}' must define {methodName}.");
        }
    }
}
